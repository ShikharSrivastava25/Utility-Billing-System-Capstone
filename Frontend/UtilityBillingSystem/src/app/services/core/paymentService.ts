import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PaymentHistoryFiltersDto, PaymentHistoryItem } from '../../models/payment';
import { API_BASE_URL } from '../../config/api.config';

@Injectable({
  providedIn: 'root',
})
export class PaymentService {
  private http = inject(HttpClient);

  getPaymentHistory(filters?: PaymentHistoryFiltersDto): Observable<PaymentHistoryItem[]> {
    let params = new HttpParams();

    if (filters?.startDate) {
      params = params.set('startDate', filters.startDate.toISOString());
    }
    if (filters?.endDate) {
      params = params.set('endDate', filters.endDate.toISOString());
    }
    if (filters?.utilityTypeId) {
      params = params.set('utilityTypeId', filters.utilityTypeId);
    }

    return this.http.get<PaymentHistoryItem[]>(`${API_BASE_URL}/payment/my-payments`, { params });
  }
}


