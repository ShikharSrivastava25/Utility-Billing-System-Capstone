export interface ConnectionForReading {
  id: string;
  userId: string;
  consumerName: string;
  utilityTypeId: string;
  utilityName: string;
  tariffId: string;
  meterNumber: string;
  previousReading: number | null;
  billingCycleId: string;
  billingCycleName: string;
}

