// src/components/ChatContainer.tsx
import React, { useState, useEffect, useRef } from 'react';
import ChatHeader from './ChatHeader';
import MessageBubble from './MessageBubble';
import UserInput from './UserInput';
// We'll update the Message type to include an imageSrc property
import { Message } from '../types';
import { v4 as uuidv4 } from 'uuid';

// Define the API endpoint for your backend
const API_URL = 'http://localhost:8004/query';

const ChatContainer: React.FC = () => {
  const [messages, setMessages] = useState<Message[]>([
    {
      id: uuidv4(),
      sender: 'AI',
      text: 'Hello! I am your JBS AI Assistant. How can I help you today?',
    },
  ]);
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  };

  useEffect(() => {
    scrollToBottom();
  }, [messages]);

  const handleSendMessage = async (text: string) => {
    const newUserMessage: Message = {
      id: uuidv4(),
      sender: 'You',
      text: text,
    };
    setMessages((prevMessages) => [...prevMessages, newUserMessage]);
    setIsLoading(true);

    try {
      const response = await fetch(API_URL, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          query: text,
          include_sql: true,
          include_results: true,
        }),
      });

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.detail || 'Failed to get response from API.');
      }

      const data = await response.json();
      console.log("API Response:", data);

      let aiMessageText: string;
      let chartImageSrc: string | undefined = undefined;

      if (data.success) {
        aiMessageText = data.response;

        // NEW: Check if chart_image_base64 is present and assign it
        if (data.chart_image_base64) {
          // Construct the data URL prefix and append the base64 string
          chartImageSrc = `data:image/png;base64,${data.chart_image_base64}`;
        }
      } else {
        aiMessageText = data.error || "An error occurred on the server.";
        console.error("Backend Error:", data.error);
      }

      // Add AI's response to chat
      const newAiMessage: Message = {
        id: uuidv4(),
        sender: 'AI',
        text: aiMessageText,
        // NEW: Add the imageSrc property to the message
        imageSrc: chartImageSrc,
      };
      setMessages((prevMessages) => [...prevMessages, newAiMessage]);

    } catch (error) {
      console.error('Error sending message to API:', error);
      const errorMessage: Message = {
        id: uuidv4(),
        sender: 'AI',
        text: `I'm sorry, I couldn't connect to the backend or process your request. Please try again. Error: ${error instanceof Error ? error.message : String(error)}`,
      };
      setMessages((prevMessages) => [...prevMessages, errorMessage]);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div style={{
      width: '100%',
      maxWidth: '750px',
      height: '85vh',
      margin: '20px auto',
      border: 'none',
      borderRadius: '8px',
      display: 'flex',
      flexDirection: 'column',
      boxShadow: '0 8px 25px rgba(0,0,0,0.25)',
      backgroundColor: 'white',
      overflow: 'hidden'
    }}>
      <ChatHeader title="JBS AI Assistant" />
      <div style={{
        flexGrow: 1,
        overflowY: 'auto',
        padding: '10px 0',
        backgroundColor: '#f8f9fa',
        borderBottom: '1px solid #e0e0e0'
      }}>
        {messages.map((msg) => (
          <MessageBubble key={msg.id} message={msg} />
        ))}
        {isLoading && (
          <div style={{
            display: 'flex',
            justifyContent: 'flex-start',
            margin: '8px 15px',
            opacity: 0.8
          }}>
            <div style={{
              backgroundColor: '#e9ecef',
              padding: '12px 16px',
              borderRadius: '18px',
              maxWidth: '75%',
              color: '#212529',
              fontStyle: 'italic'
            }}>
              AI is typing...
            </div>
          </div>
        )}
        <div ref={messagesEndRef} />
      </div>
      <UserInput onSendMessage={handleSendMessage} />
    </div>
  );
};

export default ChatContainer;