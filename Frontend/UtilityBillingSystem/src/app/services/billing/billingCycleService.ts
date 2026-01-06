import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { LogService } from '../shared/logService';
import { AuthService } from '../auth/authService';
import { MessageResponseDto } from '../../models/api-response';
import { Observable, tap } from 'rxjs';
import { BillingCycle } from '../../models/billing/billing-cycle';
import { API_BASE_URL } from '../../config/api.config';

@Injectable({
  providedIn: 'root',
})
export class BillingCycleService {
  private http = inject(HttpClient);
  private logService = inject(LogService);
  private authService = inject(AuthService);

  getBillingCycles(): Observable<BillingCycle[]> {
    return this.http.get<BillingCycle[]>(`${API_BASE_URL}/billingcycle`);
  }

  updateBillingCycle(updatedCycle: BillingCycle): Observable<BillingCycle> {
    const { id, ...updateData } = updatedCycle;
    return this.http.put<BillingCycle>(`${API_BASE_URL}/billingcycle/${id}`, updateData).pipe(
      tap(cycle => {
        const adminName = this.authService.currentUser()?.name ?? 'System';
        this.logService.logAction('BILLING_CYCLE_UPDATE', `Updated billing cycle '${cycle.name}'.`, adminName);
      })
    );
  }

  createBillingCycle(newCycleData: Omit<BillingCycle, 'id'>): Observable<BillingCycle> {
    return this.http.post<BillingCycle>(`${API_BASE_URL}/billingcycle`, newCycleData).pipe(
      tap(cycle => {
        const adminName = this.authService.currentUser()?.name ?? 'System';
        this.logService.logAction('BILLING_CYCLE_CREATE', `Created new billing cycle '${cycle.name}'.`, adminName);
      })
    );
  }

  deleteBillingCycle(cycleId: string): Observable<MessageResponseDto> {
    return this.http.delete<MessageResponseDto>(`${API_BASE_URL}/billingcycle/${cycleId}`).pipe(
      tap(() => {
        const adminName = this.authService.currentUser()?.name ?? 'System';
        this.logService.logAction('BILLING_CYCLE_DELETE', `Deleted billing cycle (ID: ${cycleId}).`, adminName);
      })
    );
  }
}

