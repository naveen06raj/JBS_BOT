// src/components/ChatHeader.tsx

import React from 'react';

interface ChatHeaderProps {
  title: string;
}

const ChatHeader: React.FC<ChatHeaderProps> = ({ title }) => {
  return (
    <div style={{
      backgroundColor: '#343a40', // Darker, almost black background
      color: '#f8f9fa', // Light gray text
      padding: '12px 20px',
      fontSize: '20px', // Slightly smaller title
      fontWeight: 'bold',
      textAlign: 'left', // Align text left
      borderBottom: '1px solid #495057', // Subtle border at the bottom
      borderRadius: '8px 8px 0 0',
      boxShadow: '0 2px 5px rgba(0,0,0,0.2)' // Slightly more pronounced shadow
    }}>
      {title}
    </div>
  );
};

export default ChatHeader;