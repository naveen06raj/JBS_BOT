// src/App.tsx

import React from 'react';
import ChatContainer from './components/ChatContainer'; // Import the main chat component
import './styles.css'; // Import your global styles (optional but good practice)

const App: React.FC = () => {
  return (
    <div className="App">
      <ChatContainer /> {/* Render the ChatContainer */}
    </div>
  );
};

export default App;