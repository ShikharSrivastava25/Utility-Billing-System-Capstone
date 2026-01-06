export interface MeterReading {
  id?: string;
  connectionId: string;
  previousReading: number;
  currentReading: number;
  readingDate: string;
  consumption: number;
  status: 'ReadyForBilling' | 'Billed';
  recordedBy: string;
  billingCycleId: string;
}

export interface MeterReadingRequest {
  connectionId: string;
  currentReading: number;
  readingDate: string;
}

