export interface BillingCycle {
  id: string;
  name: string;
  generationDay: number;
  dueDateOffset: number;
  gracePeriod: number;
  isActive: boolean;
}

