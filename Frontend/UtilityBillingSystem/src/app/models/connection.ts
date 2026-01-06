export interface Connection {
  id: string;
  userId: string; 
  userName?: string; 
  utilityTypeId: string;
  utilityTypeName?: string; 
  tariffId: string;
  tariffName?: string; 
  meterNumber: string;
  status: 'Active' | 'Inactive';
}

export interface ConnectionApprovalDto {
  tariffId: string;
  meterNumber: string;
}

