export interface BillDetailDto {
  id: string;
  connectionId: string;
  consumerName: string;
  utilityName: string;
  meterNumber: string;
  billingPeriod: string;
  generationDate: string;
  dueDate: string;
  previousReading: number;
  currentReading: number;
  consumption: number;
  baseAmount: number;
  taxAmount: number;
  penaltyAmount: number;
  totalAmount: number;
  status: string;
}

