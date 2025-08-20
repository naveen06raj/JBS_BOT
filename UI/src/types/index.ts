// src/types/index.ts

export interface Message {
  id: string; // Unique ID for the message
  sender: 'AI' | 'You'; // Who sent the message
  text?: string; // Optional: for simple text messages
  quoteDetails?: QuoteDisplayProps; // Optional: for messages displaying quote details
  // NEW: Add an optional property for the image source
  imageSrc?: string;
}

export interface QuoteItem {
  itemID: string;
  units: number;
  unitPrice: number;
  total: number;
}

export interface QuoteDisplayProps {
  quotationNumber: string;
  isRevision?: boolean; // True if it's a revision of a quote
  status: string;
  date: string; // Date of the quote (e.g., "July 12, 2025")
  totalAmount: number;
  items: QuoteItem[];
  leadInfo?: string; // Additional info related to the lead (e.g., "for Kumaran Hospital")
}