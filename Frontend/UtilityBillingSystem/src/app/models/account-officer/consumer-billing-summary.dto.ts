export interface ConsumerBillingSummaryDto {
  consumerId: string;
  consumerName: string;
  totalBilled: number;
  totalPaid: number;
  outstandingBalance: number;
  overdueCount: number;
}

