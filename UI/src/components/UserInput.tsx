// src/components/UserInput.tsx

import React, { useState } from 'react';

interface UserInputProps {
  onSendMessage: (message: string) => void;
}

const UserInput: React.FC<UserInputProps> = ({ onSendMessage }) => {
  const [message, setMessage] = useState('');

  const handleSend = () => {
    if (message.trim()) {
      onSendMessage(message);
      setMessage('');
    }
  };

  const handleKeyPress = (event: React.KeyboardEvent<HTMLInputElement>) => {
    if (event.key === 'Enter') {
      handleSend();
    }
  };

  return (
    <div style={{
      display: 'flex',
      padding: '15px',
      borderTop: '1px solid #e0e0e0', // Lighter border
      borderRadius: '0 0 8px 8px',
      backgroundColor: '#f8f9fa', // Very light background
      boxShadow: '0 -2px 5px rgba(0,0,0,0.1)' // Shadow at the top of the input area
    }}>
      <input
        type="text"
        placeholder="Type your message..."
        value={message}
        onChange={(e) => setMessage(e.target.value)}
        onKeyPress={handleKeyPress}
        style={{
          flexGrow: 1,
          padding: '12px 18px', // More padding
          border: '1px solid #ced4da', // Lighter border
          borderRadius: '25px', // More rounded
          marginRight: '10px',
          fontSize: '1em',
          outline: 'none', // Remove default outline
          boxShadow: 'inset 0 1px 2px rgba(0,0,0,0.07)' // Inner shadow for depth
        }}
      />
      <button
        onClick={handleSend}
        style={{
          backgroundColor: '#007bff', // Blue send button
          color: 'white',
          border: 'none',
          borderRadius: '25px', // More rounded
          padding: '12px 22px', // More padding
          cursor: 'pointer',
          fontSize: '1em',
          fontWeight: 'bold',
          transition: 'background-color 0.2s ease, transform 0.1s ease',
          boxShadow: '0 2px 4px rgba(0,0,0,0.1)' // Shadow for button
        }}
        onMouseOver={(e) => (e.currentTarget.style.backgroundColor = '#0056b3')}
        onMouseOut={(e) => (e.currentTarget.style.backgroundColor = '#007bff')}
        onMouseDown={(e) => (e.currentTarget.style.transform = 'translateY(1px)')}
        onMouseUp={(e) => (e.currentTarget.style.transform = 'translateY(0)')}
      >
        Send
      </button>
    </div>
  );
};

export default UserInput;