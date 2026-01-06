export interface RecentPaymentDto {
  date: Date;
  consumerId: string;
  consumerName: string;
  utilityName: string;
  amount: number;
  method: string;
}

