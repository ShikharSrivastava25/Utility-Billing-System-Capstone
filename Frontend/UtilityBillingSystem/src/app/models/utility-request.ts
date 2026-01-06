export interface UtilityRequest {
  id: string;
  userId: string;
  utilityTypeId: string;
  status: 'Pending' | 'Approved' | 'Rejected' | 'Connected';
  requestDate: Date;
  decisionDate?: Date;
}

export interface CreateUtilityRequestDto {
  userId: string;
  utilityTypeId: string;
}

