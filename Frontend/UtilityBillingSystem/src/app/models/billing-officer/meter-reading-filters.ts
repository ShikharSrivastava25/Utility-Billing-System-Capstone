export interface MeterReadingFilters {
  utilityTypeId?: string;
  status?: 'ReadyForBilling' | 'Billed';
  consumerName?: string;
}

