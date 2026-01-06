export interface PaymentRequestDto {
  paymentMethod: string;
  receiptNumber?: string;
  upiId?: string;
}

export interface PaymentHistoryFiltersDto {
  startDate?: Date | null;
  endDate?: Date | null;
  utilityTypeId?: string | null;
}

export interface PaymentHistoryItem {
  id: string;
  paymentDate: string;
  billId: string;
  billingPeriod: string;
  utilityName: string;
  amount: number;
  paymentMethod: string;
  status: string;
}

