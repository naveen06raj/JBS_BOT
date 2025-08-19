// src/components/QuoteDisplay.tsx

import React from 'react';
import { QuoteDisplayProps, QuoteItem } from '../types';

const QuoteDisplay: React.FC<QuoteDisplayProps> = ({
  quotationNumber,
  isRevision,
  status,
  date,
  totalAmount,
  items,
  leadInfo
}) => {
  return (
    <div style={{
      border: '1px solid #dcdcdc', // Lighter border
      borderRadius: '6px', // Slightly less rounded than bubbles
      padding: '12px', // Slightly less padding
      marginTop: '8px', // Reduced margin-top
      backgroundColor: '#fefefe', // Whiter background
      fontSize: '0.85em', // Slightly smaller font
      color: '#343a40', // Darker text
      boxShadow: '0 1px 2px rgba(0,0,0,0.08)' // Subtle shadow
    }}>
      <p style={{ margin: '0 0 8px 0', fontWeight: 'bold', color: '#0056b3' }}> {/* Blue for quote number */}
        Quotation: {quotationNumber} {isRevision && <span style={{fontWeight: 'normal', fontStyle: 'italic', opacity: 0.8}}>(Revision)</span>}
      </p>
      {leadInfo && <p style={{ margin: '4px 0', color: '#555' }}>{leadInfo}</p>}
      <ul style={{ listStyle: 'none', padding: 0, margin: '8px 0' }}>
        <li style={{ marginBottom: '3px' }}>
          <strong style={{ color: '#6c757d' }}>Status:</strong> {status}
        </li>
        <li style={{ marginBottom: '3px' }}>
          <strong style={{ color: '#6c757d' }}>Date:</strong> {date}
        </li>
        <li style={{ marginBottom: '8px' }}>
          <strong style={{ color: '#6c757d' }}>Total Amount:</strong> ${totalAmount.toFixed(2)}
        </li>
      </ul>
      <p style={{ margin: '0 0 5px 0', fontWeight: 'bold', color: '#0056b3' }}>Items:</p>
      <ul style={{ paddingLeft: '15px', margin: 0 }}> {/* Reduced indent for items */}
        {items.map((item, index) => (
          <li key={index} style={{ listStyle: 'disc', marginBottom: '2px', color: '#495057' }}>
            ID {item.itemID}: {item.units} units @ ${item.unitPrice.toFixed(2)} (Total: ${item.total.toFixed(2)})
          </li>
        ))}
      </ul>
    </div>
  );
};

export default QuoteDisplay;