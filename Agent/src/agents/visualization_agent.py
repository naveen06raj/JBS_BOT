# src/agents/visualization_agent.py

import logging
from typing import Dict, Any, List
import pandas as pd
import matplotlib.pyplot as plt
import base64
from io import BytesIO

class VisualizationAgent:
    """
    Agent that generates visualizations from data using matplotlib.
    """
    def __init__(self):
        pass
    
    async def generate_visualization(self, user_query: str, data: List[Dict[str, Any]]) -> Dict[str, str]:
        """
        Generates a visualization based on the data and user query.
        Returns a dict with:
        - visualization_type: type of visualization generated
        - image_base64: base64 encoded image
        - explanation: text explanation of the visualization
        """
        try:
            # Convert data to pandas DataFrame for easier processing
            df = pd.DataFrame(data)
            
            if len(df) == 0:
                return {"error": "No data available for visualization"}
            
            # Simple keyword-based visualization type detection
            query_lower = user_query.lower()
            
            if "pie" in query_lower or "percentage" in query_lower or "proportion" in query_lower:
                return await self._generate_pie_chart(df, user_query)
            elif "line" in query_lower or "trend" in query_lower or "over time" in query_lower:
                return await self._generate_line_chart(df, user_query)
            elif "bar" in query_lower or "compare" in query_lower:
                return await self._generate_bar_chart(df, user_query)
            else:
                # Default to bar chart for most cases
                return await self._generate_bar_chart(df, user_query)
                
        except Exception as e:
            logging.error(f"Error generating visualization: {e}")
            return {
                "error": f"Could not generate visualization: {str(e)}"
            }
    
    async def _generate_bar_chart(self, df: pd.DataFrame, title: str) -> Dict[str, str]:
        """Generates a bar chart from the data."""
        try:
            # Find suitable columns for the chart
            numeric_cols = df.select_dtypes(include=['number']).columns
            category_cols = df.select_dtypes(include=['object', 'category']).columns
            
            if len(numeric_cols) == 0 or len(category_cols) == 0:
                raise ValueError("Insufficient data for bar chart")
                
            x_col = category_cols[0]
            y_col = numeric_cols[0]
            
            plt.figure(figsize=(10, 6))
            df.plot.bar(x=x_col, y=y_col)
            plt.title(title)
            plt.tight_layout()
            
            return await self._save_plot_to_base64("bar", f"Bar chart showing {y_col} by {x_col}")
        except Exception as e:
            raise Exception(f"Failed to generate bar chart: {str(e)}")
    
    async def _generate_pie_chart(self, df: pd.DataFrame, title: str) -> Dict[str, str]:
        """Generates a pie chart from the data."""
        try:
            numeric_cols = df.select_dtypes(include=['number']).columns
            category_cols = df.select_dtypes(include=['object', 'category']).columns
            
            if len(numeric_cols) == 0 or len(category_cols) == 0:
                raise ValueError("Insufficient data for pie chart")
                
            labels_col = category_cols[0]
            values_col = numeric_cols[0]
            
            plt.figure(figsize=(8, 8))
            df.plot.pie(y=values_col, labels=df[labels_col], autopct='%1.1f%%')
            plt.title(title)
            plt.ylabel('')
            
            return await self._save_plot_to_base64("pie", f"Pie chart showing distribution of {values_col}")
        except Exception as e:
            raise Exception(f"Failed to generate pie chart: {str(e)}")
    
    async def _generate_line_chart(self, df: pd.DataFrame, title: str) -> Dict[str, str]:
        """Generates a line chart from the data."""
        try:
            numeric_cols = df.select_dtypes(include=['number']).columns
            date_cols = df.select_dtypes(include=['datetime']).columns
            
            if len(numeric_cols) == 0:
                raise ValueError("No numeric data for line chart")
            
            # Use first date column if available, otherwise first column
            x_col = date_cols[0] if len(date_cols) > 0 else df.columns[0]
            y_col = numeric_cols[0]
            
            plt.figure(figsize=(10, 6))
            df.plot.line(x=x_col, y=y_col, marker='o')
            plt.title(title)
            plt.tight_layout()
            
            return await self._save_plot_to_base64("line", f"Line chart showing {y_col} over {x_col}")
        except Exception as e:
            raise Exception(f"Failed to generate line chart: {str(e)}")
    
    async def _save_plot_to_base64(self, chart_type: str, explanation: str) -> Dict[str, str]:
        """Saves the current matplotlib plot to base64 and closes the figure."""
        img_bytes = BytesIO()
        plt.savefig(img_bytes, format='png', bbox_inches='tight')
        img_bytes.seek(0)
        plt.close()
        
        return {
            "visualization_type": chart_type,
            "image_base64": base64.b64encode(img_bytes.read()).decode('utf-8'),
            "explanation": explanation
        }