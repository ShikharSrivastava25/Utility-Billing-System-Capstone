import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, map, tap } from 'rxjs/operators';
import { PendingBill, BillGenerationRequest } from '../../models/billing/bill-generation';
import { NotificationService } from '../notification/notificationService';
import { API_BASE_URL } from '../../config/api.config';
import { SingleBillGenerationResponse, BillGenerationResponse } from '../../models/billing/bill-generation-response';
import { PendingBillResponse } from '../../models/billing/pending-bill-response';

@Injectable({
  providedIn: 'root'
})
export class BillGenerationService {
  private http = inject(HttpClient);
  private notificationService = inject(NotificationService);

  getPendingBills(): Observable<PendingBillResponse[]> {
    return this.http.get<PendingBillResponse[]>(`${API_BASE_URL}/bill/pending`).pipe(
      catchError(error => {
        const errorMessage = error?.error?.error?.message || 'Failed to load pending bills.';
        console.error('Error fetching pending bills:', error);
        this.notificationService.show(errorMessage, 'error');
        return throwError(() => error);
      })
    );
  }

  getPendingBillsForDisplay(): Observable<PendingBill[]> {
    return this.getPendingBills().pipe(
      map(pendingBills => pendingBills.map(bill => ({
        id: bill.readingId,
        connectionId: bill.connectionId,
        consumerName: bill.consumerName,
        utilityName: bill.utilityName,
        meterNumber: bill.meterNumber,
        previousReading: 0, 
        currentReading: 0, 
        readingDate: bill.readingDate,
        consumption: bill.units,
        status: bill.status as 'ReadyForBilling' | 'Billed',
        recordedBy: '',
        billingCycleId: '',
        month: bill.billingPeriod,
        units: bill.units,
        expectedAmount: bill.expectedAmount
      })))
    );
  }

  generateBills(request: BillGenerationRequest): Observable<BillGenerationResponse> {
    return this.http.post<BillGenerationResponse>(`${API_BASE_URL}/bill/generate/batch`, request).pipe(
      tap(response => {
        if (response.generatedCount > 0) {
          const message = `${response.generatedCount} bill(s) generated successfully.${response.failedCount > 0 ? ` ${response.failedCount} failed.` : ''}`;
          this.notificationService.show(
            message,
            response.failedCount > 0 ? 'error' : 'success'
          );
        } else {
          this.notificationService.show('No bills were generated.', 'error');
        }
      }),
      catchError(error => {
        const errorMessage = error?.error?.error?.message || 'Failed to generate bills.';
        this.notificationService.show(errorMessage, 'error');
        return throwError(() => error);
      })
    );
  }

  generateSingleBill(readingId: string): Observable<SingleBillGenerationResponse> {
    return this.http.post<SingleBillGenerationResponse>(`${API_BASE_URL}/bill/generate`, { readingId }).pipe(
      tap(() => {
        this.notificationService.show('Bill generated successfully.', 'success');
      }),
      catchError(error => {
        const errorMessage = error?.error?.error?.message || 'Failed to generate bill.';
        this.notificationService.show(errorMessage, 'error');
        return throwError(() => error);
      })
    );
  }
}

