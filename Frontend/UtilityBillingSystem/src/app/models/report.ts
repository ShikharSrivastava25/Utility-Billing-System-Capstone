export interface ReportSummary {
  activeBillingOfficers: number;
  activeAccountOfficers: number;
  totalConsumers: number;
  pendingUtilityRequests: number;
}

export interface OverdueBill {
  billId: string;
  consumerName: string;
  utilityName: string;
  amount: number;
  dueDate: Date;
}

export interface ConsumptionData {
  utilityName: string;
  consumption: number;
}

export interface MonthlyRevenue {
  month: string;
  revenue: number;
}

export interface AverageConsumption {
  utilityName: string;
  averageConsumption: number;
}

export interface ConnectionsByUtility {
  utilityName: string;
  connectionCount: number;
}

export interface ConsumptionTrendPoint {
  month: string;
  totalConsumption: number;
}

export interface ConsumptionTableRow {
  month: string;
  units: number;
  estimatedCost: number;
}

