import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { LogService } from '../shared/logService';
import { AuthService } from '../auth/authService';
import { MessageResponseDto } from '../../models/api-response';
import { Observable, tap } from 'rxjs';
import { UtilityType } from '../../models/utility';
import { API_BASE_URL } from '../../config/api.config';

@Injectable({
  providedIn: 'root',
})
export class UtilityService {
  private http = inject(HttpClient);
  private logService = inject(LogService);
  private authService = inject(AuthService);

  getUtilities(): Observable<UtilityType[]> {
    return this.http.get<UtilityType[]>(`${API_BASE_URL}/utilitytype`);
  }

  updateUtility(updatedUtility: UtilityType): Observable<UtilityType> {
    const { id, ...updateData } = updatedUtility;
    return this.http.put<UtilityType>(`${API_BASE_URL}/utilitytype/${id}`, updateData).pipe(
      tap(utility => {
        const adminName = this.authService.currentUser()?.name ?? 'System';
        this.logService.logAction('UTILITY_UPDATE', `Updated utility '${utility.name}'. New status: ${utility.status}.`, adminName);
      })
    );
  }

  createUtility(newUtility: Omit<UtilityType, 'id'>): Observable<UtilityType> {
    return this.http.post<UtilityType>(`${API_BASE_URL}/utilitytype`, newUtility).pipe(
      tap(utility => {
        const adminName = this.authService.currentUser()?.name ?? 'System';
        this.logService.logAction('UTILITY_CREATE', `Created new utility '${utility.name}'.`, adminName);
      })
    );
  }

  deleteUtility(utilityId: string): Observable<MessageResponseDto> {
    return this.http.delete<MessageResponseDto>(`${API_BASE_URL}/utilitytype/${utilityId}`).pipe(
      tap(() => {
        const adminName = this.authService.currentUser()?.name ?? 'System';
        this.logService.logAction('UTILITY_DELETE', `Deleted utility type (ID: ${utilityId}).`, adminName);
      })
    );
  }
}

