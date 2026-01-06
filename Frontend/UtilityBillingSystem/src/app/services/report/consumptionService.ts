import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../config/api.config';
import { ConsumptionTrendPoint, ConsumptionTableRow } from '../../models/report';

@Injectable({
  providedIn: 'root',
})
export class ConsumptionService {
  private http = inject(HttpClient);

  getCombinedConsumptionTrend(): Observable<ConsumptionTrendPoint[]> {
    return this.http.get<ConsumptionTrendPoint[]>(`${API_BASE_URL}/report/my-consumption`);
  }

  getUtilityConsumptionTable(utilityTypeId: string): Observable<ConsumptionTableRow[]> {
    const params = new HttpParams().set('utilityTypeId', utilityTypeId);
    return this.http.get<ConsumptionTableRow[]>(`${API_BASE_URL}/report/my-consumption`, { params });
  }
}


