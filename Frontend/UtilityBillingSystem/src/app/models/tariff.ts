export interface TariffPlan {
  id: string;
  name: string;
  utilityTypeId: string;
  baseRate: number; 
  fixedCharge: number; 
  taxPercentage: number;
  createdAt: Date;
  isActive: boolean;
}

