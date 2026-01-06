export interface Bill {
  id: string;
  connectionId: string;
  billingPeriod: string; // e.g., "July 2024"
  generationDate: Date;
  dueDate: Date;
  previousReading: number;
  currentReading: number;
  consumption: number;
  baseAmount: number;
  taxAmount: number;
  totalAmount: number;
  status: 'Due' | 'Paid' | 'Overdue';
}

