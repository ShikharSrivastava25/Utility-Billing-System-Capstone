export interface UtilityType {
  id: string;
  name: string;
  description: string;
  status: 'Enabled' | 'Disabled';
  billingCycleId?: string;
}

export interface UtilityOptionDto {
  id: string;
  name: string;
}

