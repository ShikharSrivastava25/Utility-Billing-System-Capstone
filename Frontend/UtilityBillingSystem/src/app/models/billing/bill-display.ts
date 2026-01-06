export interface BillDisplayItem {
  id: string;
  month: string;
  utilityType: string;
  units: number;
  amount: number;
  dueDate: Date;
  status: 'Due' | 'Paid' | 'Overdue';
}

export interface BillDetailData {
  id: string;
  connectionId: string;
  consumerName?: string;
  utilityName?: string;
  meterNumber?: string;
  billingPeriod: string;
  generationDate: string | Date;
  dueDate: string | Date;
  previousReading: number;
  currentReading: number;
  consumption: number;
  baseAmount: number;
  taxAmount: number;
  penaltyAmount?: number;
  PenaltyAmount?: number; // Backend may send PascalCase
  totalAmount: number;
  status: string;
}

