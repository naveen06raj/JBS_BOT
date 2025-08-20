// src/components/MessageBubble.tsx
import React from 'react';
import { Message } from '../types';
import ReactMarkdown from 'react-markdown';

interface MessageBubbleProps {
  message: Message;
}

const MessageBubble: React.FC<MessageBubbleProps> = ({ message }) => {
  const isAI = message.sender === 'AI';

  return (
    <div style={{
      display: 'flex',
      justifyContent: isAI ? 'flex-start' : 'flex-end',
      margin: '8px 0',
      padding: '0 15px'
    }}>
      <div style={{
        maxWidth: '75%',
        padding: '12px 16px',
        borderRadius: '18px',
        backgroundColor: isAI ? '#e9ecef' : '#007bff',
        color: isAI ? '#212529' : 'white',
        wordWrap: 'break-word',
        boxShadow: '0 1px 3px rgba(0,0,0,0.1)'
      }}>
        <strong style={{
          fontSize: '0.8em',
          opacity: 0.7,
          display: 'block',
          marginBottom: '4px',
          color: isAI ? '#495057' : 'rgba(255,255,255,0.8)'
        }}>
          {isAI ? 'AI' : 'You'}
        </strong>

        {/* This is the new block to conditionally render the image */}
        {message.imageSrc && (
          <img
            src={message.imageSrc}
            alt="Generated Chart"
            style={{
              maxWidth: '100%',
              height: 'auto',
              borderRadius: '6px',
              marginBottom: '10px'
            }}
          />
        )}
        
        {/* Use ReactMarkdown to render the text content */}
        {message.text && <ReactMarkdown>{message.text}</ReactMarkdown>}
        
        {/*
        // IMPORTANT: If your backend is now producing formatted text for quotes
        // you might not need the `quoteDetails` property and the `QuoteDisplay` component.
        // The LLM's response.text will contain the formatted quote.
        // You can uncomment the line below ONLY if you still have a separate, custom parsing
        // logic in ChatContainer for 'quoteDetails' and want a *custom UI component*
        // for quotes, rather than just Markdown.
        */}
        {/* {message.quoteDetails && <QuoteDisplay {...message.quoteDetails} />} */}
      </div>
    </div>
  );
};

export default MessageBubble;