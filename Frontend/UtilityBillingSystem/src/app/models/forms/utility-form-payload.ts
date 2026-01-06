import { UtilityType } from '../utility';

export interface UtilityFormPayload {
  name: string;
  description: string;
  status: 'Enabled' | 'Disabled';
  billingCycleId?: string;
}

export function isUtilityUpdate(payload: UtilityFormPayload | UtilityType): payload is UtilityType {
  return 'id' in payload;
}

