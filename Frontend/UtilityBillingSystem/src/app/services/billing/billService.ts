import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { Bill } from '../../models/billing/bill';
import { PaymentRequestDto } from '../../models/payment';
import { API_BASE_URL } from '../../config/api.config';
import { BillDetailDto } from '../../models/billing/bill-detail.dto';

@Injectable({
  providedIn: 'root',
})
export class BillService {
  private http = inject(HttpClient);

  getConsumerBills(): Observable<BillDetailDto[]> {
    return this.http.get<BillDetailDto[]>(`${API_BASE_URL}/bill/my-bills`);
  }

  payBill(billId: string, payload: PaymentRequestDto): Observable<void> {
    return this.http
      .post(`${API_BASE_URL}/bill/${billId}/pay`, payload)
      .pipe(map(() => void 0));
  }
}

