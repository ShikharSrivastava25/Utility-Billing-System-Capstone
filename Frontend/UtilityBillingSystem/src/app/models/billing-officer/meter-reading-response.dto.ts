export interface MeterReadingResponse {
  id: string;
  connectionId: string;
  userId: string;
  consumerName: string;
  utilityName: string;
  meterNumber: string;
  previousReading: number;
  currentReading: number;
  consumption: number;
  readingDate: string;
  status: string;
  recordedBy: string;
  billingCycleId: string;
  month: string;
  createdAt: string;
}

