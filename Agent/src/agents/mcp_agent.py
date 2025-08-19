import os
import asyncio
from typing import TypedDict, List, Annotated, Sequence
from operator import add
import json
import re

from dotenv import load_dotenv
from langchain_google_vertexai import ChatVertexAI
from langchain.agents import create_react_agent
from langchain_core.messages import HumanMessage, AIMessage, BaseMessage, ToolMessage, SystemMessage
from langgraph.graph import StateGraph, END
from langchain import hub 
from langchain_core.prompts import ChatPromptTemplate, MessagesPlaceholder 
from langchain_core.agents import AgentFinish, AgentAction
from langchain_core.exceptions import OutputParserException 

# MCP imports
from langchain_mcp_adapters.client import MultiServerMCPClient

# --- 1. Load environment variables (from .env file) ---
load_dotenv()

# --- 2. Set the GOOGLE_APPLICATION_CREDENTIALS environment variable in code ---
try:
    # It's generally better to let dotenv handle this if the path is in .env,
    # but if it must be hardcoded for deployment or specific setups, this is fine.
    # Ensure this path is correct for your environment.
    service_account_key_path = r"C:\Users\Admin\Downloads\geminimcp-464809-eee97d96077e.json"
    if not os.path.exists(service_account_key_path):
        raise FileNotFoundError(f"Service account key file not found at: {service_account_key_path}")
    os.environ["GOOGLE_APPLICATION_CREDENTIALS"] = service_account_key_path
    print(f"[Agent Setup] GOOGLE_APPLICATION_CREDENTIALS set to: {service_account_key_path}")
except FileNotFoundError as e:
    print(f"[Fatal Error] {e}")
    print("Please ensure the service account JSON key file exists at the specified path.")
    raise

# Set your GCP Project ID and Location from .env or default
GCP_PROJECT_ID = os.getenv("GCP_PROJECT_ID", "geminimcp-464809")
GOOGLE_LOCATION = os.getenv("GOOGLE_LOCATION", "us-central1")

# IMPORTANT: This MCP_CORE_PATH now points to your *separate* MCP Server (defaulting to 8001)
# Ensure your CRM MCP server is running on this port.
MCP_CORE_PATH = os.getenv("MCP_CORE_PATH", "http://127.0.0.1:8001/mcp")

# --- Global variables (declared here for module-level access) ---
llm = None
mcp_tools = None
mcp_client = None
langgraph_app = None
agent_prompt = None

# --- Agent State Definition ---
class AgentState(TypedDict):
    input: str
    chat_history: List[BaseMessage]
    agent_outcome: Annotated[Sequence[BaseMessage], add] # This is what LangGraph updates

# --- Initialize Agent Components (async function) ---
async def _initialize_agent_components():
    global llm, mcp_tools, mcp_client, agent_prompt

    print(f"[Agent Setup] Initializing Gemini LLM with project: {GCP_PROJECT_ID}, location: {GOOGLE_LOCATION}")
    llm = ChatVertexAI(
        model_name="gemini-2.5-pro", # Or "gemini-1.5-pro" or "gemini-1.0-pro" based on availability/preference
        temperature=0,
        project=GCP_PROJECT_ID,
        location=GOOGLE_LOCATION
    )
    try:
        test_response = await llm.ainvoke([HumanMessage(content="Hello Gemini! Are you awake?")])
        print(f"[Agent Setup] Gemini test response: {test_response.content[:50]}...")
    except Exception as e:
        print(f"[Agent Setup Error] Failed to connect to Gemini LLM: {e}")
        print("Please check your model name, location, project ID, and Google Cloud credentials.")
        raise

    print(f"[Agent] Using Vertex AI Gemini model: {llm.model_name}")

    print(f"[Agent] Connecting to MCP Core at: {MCP_CORE_PATH}")
    mcp_servers_config = {
        "crm": { # The key "crm" here corresponds to the 'context' you set in your FastMCP server
            "transport": "streamable_http",
            "url": MCP_CORE_PATH
        }
    }
    mcp_client = MultiServerMCPClient(mcp_servers_config)

    try:
        mcp_tools = await mcp_client.get_tools()
    except Exception as e:
        raise ValueError(
            f"Failed to load tools from MCP Core. Ensure your MCP server is running correctly at {MCP_CORE_PATH}. Error: {e}"
        )

    if not mcp_tools:
        raise ValueError(
            "No tools loaded from MCP Core. "
            f"Ensure your MCP server is running correctly at {MCP_CORE_PATH}. "
            "Also check if your FastMCP server has defined tools (using @fastmcp.tool())."
        )
    print(f"[Agent] Successfully loaded tools: {[tool.name for tool in mcp_tools]}")

    agent_prompt = hub.pull("hwchase17/react-chat")
    print("[Agent Setup] Using base 'hwchase17/react-chat' prompt from LangChain Hub.")


# --- Graph Nodes ---
async def call_agent(state: AgentState) -> dict:
    global llm, mcp_tools, agent_prompt 
    if llm is None or mcp_tools is None or agent_prompt is None:
        raise RuntimeError("LLM, tools, and agent_prompt must be initialized before calling agent.")

    agent_runnable = create_react_agent(llm, mcp_tools, agent_prompt) 

    intermediate_steps_for_react = []
    current_action_for_pair = None
    for msg in state.get("agent_outcome", []): 
        if isinstance(msg, AgentAction):
            current_action_for_pair = msg
        elif isinstance(msg, ToolMessage) and current_action_for_pair:
            # LangChain's create_react_agent expects (AgentAction, observation_string)
            intermediate_steps_for_react.append((current_action_for_pair, msg.content))
            current_action_for_pair = None
        elif isinstance(msg, (AIMessage, HumanMessage, SystemMessage, AgentFinish)):
            pass 
        else:
            print(f"Warning: Unexpected message type in agent_outcome for intermediate_steps: {type(msg)}")

    try:
        agent_output = await agent_runnable.ainvoke({
            "input": state["input"],
            "chat_history": state["chat_history"],
            "intermediate_steps": intermediate_steps_for_react,
        })
        print(f"[Agent] LLM output (raw): {agent_output}") # Log raw LLM output for debugging
    except OutputParserException as e:
        print(f"[Agent] OutputParserException caught in call_agent: {e}")
        raw_output_match = re.search(r"LLM output: `(.*)`", str(e), re.DOTALL)
        raw_output = raw_output_match.group(1) if raw_output_match else f"Could not parse LLM output: {e}"
        return {"agent_outcome": [AIMessage(content=raw_output)]}
    except Exception as e:
        print(f"[Agent] General error during agent_runnable.ainvoke: {e}")
        return {"agent_outcome": [AIMessage(content=f"An error occurred while the AI was thinking: {e}")]}

    # Ensure the output is always a list of BaseMessage for Annotated[Sequence[BaseMessage], add]
    if isinstance(agent_output, AgentFinish):
        return {"agent_outcome": [agent_output]}
    elif isinstance(agent_output, AgentAction):
        return {"agent_outcome": [agent_output]}
    elif isinstance(agent_output, list) and all(isinstance(item, BaseMessage) for item in agent_output):
        return {"agent_outcome": agent_output}
    elif isinstance(agent_output, BaseMessage): # If it's a single message, wrap it in a list
        return {"agent_outcome": [agent_output]}
    else:
        print(f"Warning: Unexpected agent_output type from agent_runnable: {type(agent_output)}. Converting to AIMessage.")
        return {"agent_outcome": [AIMessage(content=str(agent_output))]}

async def execute_tools(state: AgentState) -> dict:
    global mcp_tools
    if mcp_tools is None:
        raise RuntimeError("Tools must be initialized before executing them.")

    actions_to_execute = []
    # Find the most recent AgentAction to execute
    for msg in reversed(state.get("agent_outcome", [])): 
        if isinstance(msg, AgentAction):
            actions_to_execute.append(msg)
            break 
    
    if not actions_to_execute:
        print(f"!!! ALERT: No AgentAction found in the latest agent_outcome for tool execution. State: {state}")
        # If no action, perhaps the LLM directly replied (e.g., from OutputParserException fallback)
        # or it's a transient state. Let the graph decide.
        return {"agent_outcome": []} # Return empty list, and decide_next_step will handle.

    tool_messages = []
    for action in actions_to_execute: # In ReAct, typically one action at a time.
        if not isinstance(action, AgentAction): 
            print(f"[Agent] Skipping non-AgentAction item in actions_to_execute: {type(action)}")
            continue

        tool_name = action.tool
        tool_input_from_llm = action.tool_input 

        print(f"\n[Agent] Calling tool: {tool_name} with raw input from LLM: {tool_input_from_llm}")
        found_tool = next((tool for tool in mcp_tools if tool.name == tool_name), None)

        if found_tool:
            try:
                final_tool_argument_for_mcp = {} 
                
                # --- Specific input parsing for each tool ---
                if tool_name == "get_lead_info":
                    # Expects an integer 'id'
                    param_value_extracted = None
                    if isinstance(tool_input_from_llm, dict):
                        if 'id' in tool_input_from_llm:
                            param_value_extracted = tool_input_from_llm['id']
                        elif 'input' in tool_input_from_llm and isinstance(tool_input_from_llm['input'], dict) and 'id' in tool_input_from_llm['input']:
                            param_value_extracted = tool_input_from_llm['input']['id']
                    elif isinstance(tool_input_from_llm, str):
                        try:
                            json_parsed = json.loads(tool_input_from_llm)
                            if 'id' in json_parsed:
                                param_value_extracted = json_parsed['id']
                            elif 'input' in json_parsed and isinstance(json_parsed['input'], dict) and 'id' in json_parsed['input']:
                                param_value_extracted = json_parsed['input']['id']
                        except json.JSONDecodeError:
                            try:
                                param_value_extracted = int(tool_input_from_llm)
                            except ValueError:
                                pass 

                    if param_value_extracted is None:
                        raise ValueError(f"Could not extract a valid integer 'id' for '{tool_name}' from input: {tool_input_from_llm}")
                    
                    try:
                        final_tool_argument_for_mcp = {"input": {"id": int(param_value_extracted)}}
                    except ValueError:
                        raise ValueError(f"Extracted ID '{param_value_extracted}' could not be converted to an integer for '{tool_name}'.")
                elif tool_name == "get_sales_opportunity_by_id":
                    param_value_extracted = None
                    if isinstance(tool_input_from_llm, dict):
                        if 'opportunityId' in tool_input_from_llm:
                            param_value_extracted = tool_input_from_llm['opportunityId']
                        elif 'input' in tool_input_from_llm and isinstance(tool_input_from_llm['input'], dict) and 'opportunityId' in tool_input_from_llm['input']:
                            param_value_extracted = tool_input_from_llm['input']['opportunityId']
                    elif isinstance(tool_input_from_llm, str):
                        try:
                            json_parsed = json.loads(tool_input_from_llm)
                            if 'opportunityId' in json_parsed:
                                param_value_extracted = json_parsed['opportunityId']
                            elif 'input' in json_parsed and isinstance(json_parsed['input'], dict) and 'opportunityId' in json_parsed['input']:
                                param_value_extracted = json_parsed['input']['opportunityId']
                        except json.JSONDecodeError:
                # In this case, we can't assume a direct string, so we'll just pass
                            pass
                    if param_value_extracted is None:
                        raise ValueError(f"Could not extract a valid string 'opportunityId' for '{tool_name}' from input: {tool_input_from_llm}")
                    final_tool_argument_for_mcp = {"input": {"opportunityId": str(param_value_extracted)}}
               
                elif tool_name == "get_sales_lead_quotations_with_items":
                    # Expects a string 'leadId'
                    param_value_extracted = None
                    if isinstance(tool_input_from_llm, dict):
                        if 'leadId' in tool_input_from_llm:
                            param_value_extracted = tool_input_from_llm['leadId']
                        elif 'input' in tool_input_from_llm and isinstance(tool_input_from_llm['input'], dict) and 'leadId' in tool_input_from_llm['input']:
                            param_value_extracted = tool_input_from_llm['input']['leadId']
                    elif isinstance(tool_input_from_llm, str):
                        try:
                            json_parsed = json.loads(tool_input_from_llm)
                            if 'leadId' in json_parsed:
                                param_value_extracted = json_parsed['leadId']
                            elif 'input' in json_parsed and isinstance(json_parsed['input'], dict) and 'leadId' in json_parsed['input']:
                                param_value_extracted = json_parsed['input']['leadId']
                        except json.JSONDecodeError:
                            param_value_extracted = tool_input_from_llm # Assume raw string is the ID

                    if param_value_extracted is None:
                        raise ValueError(f"Could not extract a valid string 'leadId' for '{tool_name}' from input: {tool_input_from_llm}")
                    
                    final_tool_argument_for_mcp = {"input": {"leadId": str(param_value_extracted)}}

                elif tool_name == "get_sales_opportunity_card_counts":
                    # This tool takes no parameters. The LLM might output an empty dict or `None`.
                    final_tool_argument_for_mcp = {"input": {}} # Ensure it's an empty dict for the input Pydantic model

                elif tool_name == "get_active_opportunities_with_items":
                    # This tool takes no parameters.
                    final_tool_argument_for_mcp = {"input": {}} # Ensure it's an empty dict

                elif tool_name == "get_opportunity_by_id_with_items":
                    # Expects a string 'id_or_opportunity_id'
                    param_value_extracted = None
                    if isinstance(tool_input_from_llm, dict):
                        if 'id_or_opportunity_id' in tool_input_from_llm:
                            param_value_extracted = tool_input_from_llm['id_or_opportunity_id']
                        elif 'input' in tool_input_from_llm and isinstance(tool_input_from_llm['input'], dict) and 'id_or_opportunity_id' in tool_input_from_llm['input']:
                            param_value_extracted = tool_input_from_llm['input']['id_or_opportunity_id']
                    elif isinstance(tool_input_from_llm, str):
                        try:
                            json_parsed = json.loads(tool_input_from_llm)
                            if 'id_or_opportunity_id' in json_parsed:
                                param_value_extracted = json_parsed['id_or_opportunity_id']
                            elif 'input' in json_parsed and isinstance(json_parsed['input'], dict) and 'id_or_opportunity_id' in json_parsed['input']:
                                param_value_extracted = json_parsed['input']['id_or_opportunity_id']
                        except json.JSONDecodeError:
                            param_value_extracted = tool_input_from_llm # Assume raw string is the ID

                    if param_value_extracted is None:
                        raise ValueError(f"Could not extract 'id_or_opportunity_id' for '{tool_name}' from input: {tool_input_from_llm}")
                    
                    final_tool_argument_for_mcp = {"input": {"id_or_opportunity_id": str(param_value_extracted)}}

                else:
                    # Fallback for any other tools or if LLM sends direct JSON
                    print(f"[Agent] Warning: Unrecognized or unhandled tool '{tool_name}'. Attempting generic input handling.")
                    if isinstance(tool_input_from_llm, dict):
                        final_tool_argument_for_mcp = tool_input_from_llm
                    else:
                        try:
                            # Try to parse as JSON, otherwise send as is within 'input'
                            parsed_input = json.loads(tool_input_from_llm)
                            final_tool_argument_for_mcp = {"input": parsed_input}
                        except json.JSONDecodeError:
                            final_tool_argument_for_mcp = {"input": tool_input_from_llm}

                print(f"[Agent] Calling tool: {tool_name} with processed input: {final_tool_argument_for_mcp}")
                observation = await found_tool.ainvoke(final_tool_argument_for_mcp)
                
                # Use getattr for tool_call_id for broader compatibility
                tool_call_id_val = getattr(action, 'tool_call_id', str(id(action))) 
                tool_messages.append(ToolMessage(content=str(observation), tool_call_id=tool_call_id_val))

            except Exception as e:
                error_msg = f"Error executing tool '{tool_name}': {type(e).__name__}: {e}"
                print(f"[Agent Error] {error_msg}")
                tool_messages.append(ToolMessage(content=error_msg, tool_call_id=getattr(action, 'tool_call_id', str(id(action)))))
        else:
            error_msg = f"Tool '{tool_name}' not found in MCP client's loaded tools."
            print(f"[Agent Error] {error_msg}")
            tool_messages.append(ToolMessage(content=error_msg, tool_call_id=getattr(action, 'tool_call_id', str(id(action)))))

    return {"agent_outcome": tool_messages}

def decide_next_step(state: AgentState):
    """
    Decides the next step in the LangGraph workflow based on the last agent outcome.
    """
    last_outcome_list = state.get("agent_outcome")
    if not last_outcome_list:
        print("[Agent] decide_next_step: agent_outcome is empty. Returning to agent.")
        return "agent" # Should ideally not happen after the first agent call, but as a fallback

    last_outcome = last_outcome_list[-1] # Get the very last message/outcome

    if isinstance(last_outcome, AgentFinish):
        print(f"[Agent] decide_next_step: AgentFinish detected. Ending graph.")
        return END
    elif isinstance(last_outcome, AgentAction):
        print(f"[Agent] decide_next_step: AgentAction detected. Moving to tools.")
        return "tools"
    elif isinstance(last_outcome, ToolMessage):
        print(f"[Agent] decide_next_step: ToolMessage detected. Moving back to agent.")
        return "agent"
    elif isinstance(last_outcome, AIMessage): # This typically means the LLM directly replied
        print(f"[Agent] decide_next_step: AIMessage detected (direct LLM reply). Ending graph.")
        return END
    else:
        print(f"[Agent] Warning: decide_next_step encountered unexpected last_outcome type: {type(last_outcome)}. Content: {last_outcome}. Ending graph for safety.")
        return END 

def _build_langgraph_app():
    global langgraph_app
    workflow = StateGraph(AgentState)
    workflow.add_node("agent", call_agent)
    workflow.add_node("tools", execute_tools)

    workflow.set_entry_point("agent")

    workflow.add_conditional_edges(
        "agent",
        decide_next_step,
        {
            "tools": "tools",
            END: END
        }
    )
    workflow.add_edge("tools", "agent") # After tools execute, go back to agent for reasoning

    langgraph_app = workflow.compile()
    print("[Agent] LangGraph workflow compiled.")

# --- Functions for UI Integration ---

async def setup_agent_for_ui():
    """
    Initializes the agent components and builds the LangGraph app.
    Call this once when your application starts.
    """
    global langgraph_app, mcp_tools
    print("[Agent] Setting up agent for UI integration...")
    if langgraph_app is None or mcp_tools is None:
        await _initialize_agent_components()
        _build_langgraph_app()
    return langgraph_app, mcp_tools

async def invoke_agent_with_history(agent_app_instance, user_question: str, chat_history: List[BaseMessage]) -> str:
    """
    Invokes the LangGraph agent with a new user question and previous chat history.
    Returns the final string response from the agent.
    """
    if agent_app_instance is None:
        raise RuntimeError("LangGraph agent_app is not initialized. Call setup_agent_for_ui first.")

    initial_state = {
        "input": user_question,
        "chat_history": chat_history,
        "agent_outcome": [] 
    }

    final_result_message = None 

    try:
        print(f"[UI Backend Chat] Starting agent stream with user question: '{user_question}'")
        
        async for state_update in agent_app_instance.astream(initial_state):
            print(f"[UI Backend Chat] Received state_update in stream: {state_update}")
            
            # Identify which node just executed and extract its specific output
            if state_update: 
                # state_update will have a single key representing the node name
                node_name = list(state_update.keys())[0] 
                node_output = state_update[node_name]
                
                # Check the 'agent_outcome' sequence that was appended by the node
                if "agent_outcome" in node_output and node_output["agent_outcome"]:
                    for msg in node_output["agent_outcome"]:
                        if isinstance(msg, AgentAction):
                            print(f"[Trace] Agent Action: Tool='{msg.tool}', Input={msg.tool_input}")
                        elif isinstance(msg, ToolMessage):
                            # Ensure content is string for printing
                            content_to_print = str(msg.content)
                            if len(content_to_print) > 200:
                                content_to_print = content_to_print[:200] + "..."
                            print(f"[Trace] Tool Observation: {content_to_print}")
                        elif isinstance(msg, AIMessage):
                            print(f"[Trace] AI Message (from LLM): {msg.content}")
                            final_result_message = msg 
                        elif isinstance(msg, AgentFinish):
                            print(f"[Trace] Agent Finished: {msg.return_values}")
                            final_result_message = msg 
                else:
                    print(f"[Trace] Node '{node_name}' did not add to 'agent_outcome' or it was empty.")
            else:
                print("[UI Backend Chat] Received empty state_update in stream (likely END).")


    except OutputParserException as e:
        print(f"[UI Backend] OutputParserException during agent invocation (LangGraph stream): {e}")
        import traceback
        traceback.print_exc() 
        raw_output_match = re.search(r"LLM output: `(.*)`", str(e), re.DOTALL)
        if raw_output_match:
            final_response = raw_output_match.group(1).strip()
            print(f"[UI Backend] Recovered partial LLM output: {final_response[:100]}...")
        else:
            final_response = f"An internal parsing error occurred: {type(e).__name__}: {e}. Please try again or contact support."
        return final_response
        
    except Exception as e:
        print(f"[UI Backend] General error during agent invocation (LangGraph stream): {e}")
        import traceback
        traceback.print_exc() 
        return f"An internal error occurred during processing: {type(e).__name__}: {e}. Please try again or contact support."

    final_response = "No response from agent."
    if final_result_message:
        if isinstance(final_result_message, AgentFinish):
            final_response = final_result_message.return_values.get("output", "Agent finished with no output.")
            print(f"[UI Backend Chat] Final AgentFinish output: {final_response}")
        elif isinstance(final_result_message, AIMessage):
            final_response = final_result_message.content
            print(f"[UI Backend Chat] Final AIMessage content: {final_response}")
        elif isinstance(final_result_message, ToolMessage): # Should typically be an AIMessage or AgentFinish
            final_response = final_result_message.content
            print(f"[UI Backend Chat] Agent ended unexpectedly with ToolMessage: {final_response}. This indicates the LLM didn't produce a final answer after tool use.")
        else:
            final_response = f"Agent's final thought (unexpected type): {str(final_result_message)}"
            print(f"[UI Backend Chat] Unexpected final_result_message type: {type(final_result_message)}")
    else:
        print(f"[UI Backend Chat] No final result message (AgentFinish/AIMessage) was captured from the stream.")
        final_response = "The agent completed its process, but no final message or tool output was generated. This might indicate an issue with the LLM's final response generation or a state where it didn't explicitly finish."

    return final_response

