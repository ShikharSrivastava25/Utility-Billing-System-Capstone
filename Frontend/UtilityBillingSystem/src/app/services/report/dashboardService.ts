import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../config/api.config';

export interface DashboardConsumptionPoint {
  month: string;
  totalConsumption: number;
}

export interface ConsumerDashboardResponse {
  outstandingBalance: number;
  monthlySpending: number;
  activeConnections: number;
  dueBillsCount: number;
  consumptionTrend: DashboardConsumptionPoint[];
}

@Injectable({
  providedIn: 'root',
})
export class DashboardService {
  private http = inject(HttpClient);

  getDashboard(): Observable<ConsumerDashboardResponse> {
    return this.http.get<ConsumerDashboardResponse>(`${API_BASE_URL}/report/dashboard`);
  }
}


