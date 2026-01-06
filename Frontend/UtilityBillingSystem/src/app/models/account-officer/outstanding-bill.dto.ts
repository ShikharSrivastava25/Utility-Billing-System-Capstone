export interface OutstandingBillDto {
  billId: string;
  consumerId: string;
  consumerName: string;
  utilityName: string;
  billMonth: string;
  amount: number;
  status: 'Due' | 'Overdue';
  dueDate: Date;
}

