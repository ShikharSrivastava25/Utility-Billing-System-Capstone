import { MeterReading } from "../billing-officer/meter-reading";

export interface DisplayMeterReading extends MeterReading {
  userId?: string;
  consumerName: string;
  utilityName: string;
  meterNumber: string;
  month: string;
}

