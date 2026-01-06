export interface PendingBillResponse {
  readingId: string;
  connectionId: string;
  consumerName: string;
  utilityName: string;
  meterNumber: string;
  units: number;
  expectedAmount: number;
  status: string;
  readingDate: string;
  billingPeriod: string;
}

