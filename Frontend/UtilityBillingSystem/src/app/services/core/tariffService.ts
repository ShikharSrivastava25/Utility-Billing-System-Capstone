import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { TariffPlan } from '../../models/tariff';
import { MessageResponseDto } from '../../models/api-response';
import { Observable, tap } from 'rxjs';
import { LogService } from '../shared/logService';
import { AuthService } from '../auth/authService';
import { API_BASE_URL } from '../../config/api.config';

@Injectable({
  providedIn: 'root',
})
export class TariffService {
  private http = inject(HttpClient);
  private logService = inject(LogService);
  private authService = inject(AuthService);

  getTariffs(): Observable<TariffPlan[]> {
    return this.http.get<TariffPlan[]>(`${API_BASE_URL}/tariff`);
  }

  createTariff(newTariffData: Omit<TariffPlan, 'id' | 'createdAt' | 'isActive'>): Observable<TariffPlan> {
    return this.http.post<TariffPlan>(`${API_BASE_URL}/tariff`, newTariffData).pipe(
      tap(tariff => {
        const adminName = this.authService.currentUser()?.name ?? 'System';
        this.logService.logAction('TARIFF_CREATE', `Created new tariff plan '${tariff.name}'.`, adminName);
      })
    );
  }

  updateTariff(updatedTariffData: TariffPlan): Observable<TariffPlan> {
    const adminName = this.authService.currentUser()?.name ?? 'System';
    const { id, ...updateData } = updatedTariffData;
    
    return this.http.put<TariffPlan>(`${API_BASE_URL}/tariff/${id}`, updateData).pipe(
      tap(updatedTariff => {
        this.logService.logAction('TARIFF_UPDATE', `Updated tariff plan '${updatedTariff.name}'.`, adminName);
      })
    );
  }

  deleteTariff(tariffId: string): Observable<MessageResponseDto> {
    return this.http.delete<MessageResponseDto>(`${API_BASE_URL}/tariff/${tariffId}`).pipe(
      tap(() => {
        const adminName = this.authService.currentUser()?.name ?? 'System';
        this.logService.logAction('TARIFF_DELETE', `Deleted tariff (ID: ${tariffId}).`, adminName);
      })
    );
  }
}

