// src/index.tsx

import React from 'react';
import ReactDOM from 'react-dom/client';
import App from './App'; // Import your App component

// Get the root DOM element where your React app will be mounted
const root = ReactDOM.createRoot(
  document.getElementById('root') as HTMLElement
);

// Render your App component inside React.StrictMode for development checks
root.render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);