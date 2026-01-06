export interface PaymentAuditDto {
  date: Date;
  consumerId: string;
  consumerName: string;
  utilityName: string;
  amount: number;
  method: string;
  reference: string;
}

