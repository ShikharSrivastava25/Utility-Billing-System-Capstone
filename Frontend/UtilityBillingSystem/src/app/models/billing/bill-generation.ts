import { DisplayMeterReading } from "../display/display-meter-reading";

export interface PendingBill extends DisplayMeterReading {
  expectedAmount: number;
  units: number;
}

export interface BillGenerationRequest {
  readingIds: string[];
}

