import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ConnectionsByUtility, ReportSummary } from '../../models/report';
import { API_BASE_URL } from '../../config/api.config';

@Injectable({
  providedIn: 'root',
})
export class ReportService {
  private http = inject(HttpClient);

  getReportSummary(): Observable<ReportSummary> {
    return this.http.get<ReportSummary>(`${API_BASE_URL}/report/summary`);
  }

  getConnectionsByUtility(): Observable<ConnectionsByUtility[]> {
    return this.http.get<ConnectionsByUtility[]>(`${API_BASE_URL}/report/connections-by-utility`);
  }
}

