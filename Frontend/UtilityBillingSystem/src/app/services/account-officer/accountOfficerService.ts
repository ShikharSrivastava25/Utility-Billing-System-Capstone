import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { API_BASE_URL } from '../../config/api.config';
import { AccountOfficerDashboardDto } from '../../models/account-officer/account-officer-dashboard.dto';
import { MonthlyRevenueDto } from '../../models/account-officer/monthly-revenue.dto';
import { RecentPaymentDto } from '../../models/account-officer/recent-payment.dto';
import { OutstandingByUtilityDto } from '../../models/account-officer/outstanding-by-utility.dto';
import { PaymentAuditDto } from '../../models/account-officer/payment-audit.dto';
import { OutstandingBillDto } from '../../models/account-officer/outstanding-bill.dto';
import { ConsumerBillingSummaryDto } from '../../models/account-officer/consumer-billing-summary.dto';
import { MonthlyRevenueResponse } from '../../models/account-officer/monthly-revenue-response';
import { PagedResult } from '../../models/common/paged-result';
import { AverageConsumption, ConsumptionData } from '../../models/report';
import { BillDetailData } from '../../models/billing/bill-display';

@Injectable({
  providedIn: 'root'
})
export class AccountOfficerService {
  private http = inject(HttpClient);

  getDashboardSummary(): Observable<AccountOfficerDashboardDto> {
    return this.http.get<AccountOfficerDashboardDto>(`${API_BASE_URL}/report/dashboard/summary`);
  }

  getMonthlyRevenue(filters?: {
    startDate?: Date;
    endDate?: Date;
    month?: number;
    year?: number;
  }): Observable<MonthlyRevenueDto[]> {
    let params = new HttpParams();

    if (filters) {
      if (filters.startDate) {
        params = params.set('startDate', filters.startDate.toISOString());
      }
      if (filters.endDate) {
        params = params.set('endDate', filters.endDate.toISOString());
      }
      if (filters.month !== undefined && filters.month !== null) {
        params = params.set('month', filters.month.toString());
      }
      if (filters.year !== undefined && filters.year !== null) {
        params = params.set('year', filters.year.toString());
      }
    }

    return this.http.get<MonthlyRevenueResponse[]>(`${API_BASE_URL}/report/monthly-revenue-by-billing`, { params }).pipe(
      map(data => data.map(item => ({
        month: item.month,
        totalRevenue: item.revenue
      })))
    );
  }

  getRecentPayments(count: number = 5): Observable<RecentPaymentDto[]> {
    const params = new HttpParams().set('count', count.toString());
    return this.http.get<RecentPaymentDto[]>(`${API_BASE_URL}/report/dashboard/recent-payments`, { params }).pipe(
      map(payments => payments.map(payment => ({
        ...payment,
        date: new Date(payment.date)
      })))
    );
  }

  getAllPayments(): Observable<PaymentAuditDto[]> {
    const params = new HttpParams()
      .set('page', '1')
      .set('pageSize', '10000');
    
    return this.http.get<PagedResult<PaymentAuditDto>>(`${API_BASE_URL}/payment/audit`, { params }).pipe(
      map(result => result.data)
    );
  }

  getOutstandingBills(): Observable<OutstandingBillDto[]> {
    const params = new HttpParams()
      .set('page', '1')
      .set('pageSize', '10000');
    
    return this.http.get<PagedResult<OutstandingBillDto>>(`${API_BASE_URL}/bill/outstanding`, { params }).pipe(
      map(result => {
        return result.data.map(bill => ({
          ...bill,
          dueDate: new Date(bill.dueDate)
        }));
      })
    );
  }

  getOutstandingByUtility(): Observable<OutstandingByUtilityDto[]> {
    return this.http.get<OutstandingByUtilityDto[]>(`${API_BASE_URL}/report/dashboard/outstanding-by-utility`);
  }

  getConsumerBillingSummary(): Observable<ConsumerBillingSummaryDto[]> {
    const params = new HttpParams()
      .set('page', '1')
      .set('pageSize', '10000');
    
    return this.http.get<PagedResult<ConsumerBillingSummaryDto>>(`${API_BASE_URL}/bill/consumers/summary`, { params }).pipe(
      map(result => result.data)
    );
  }

  getBillById(billId: string): Observable<BillDetailData> {
    return this.http.get<BillDetailData>(`${API_BASE_URL}/bill/${billId}`);
  }

  getAverageConsumption(): Observable<AverageConsumption[]> {
    return this.http.get<AverageConsumption[]>(`${API_BASE_URL}/report/average-consumption`);
  }

  getConsumptionByUtility(): Observable<ConsumptionData[]> {
    return this.http.get<ConsumptionData[]>(`${API_BASE_URL}/report/consumption`);
  }
}

