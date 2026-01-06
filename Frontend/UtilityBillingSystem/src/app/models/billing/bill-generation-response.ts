export interface SingleBillGenerationResponse {
  id: string;
  connectionId: string;
  billingPeriod: string;
  totalAmount: number;
  status: string;
}

export interface BillGenerationResponse {
  generatedCount: number;
  failedCount: number;
  generatedBillIds: string[];
  errors: string[];
}