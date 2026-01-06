import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
import { MeterReading, MeterReadingRequest } from '../../models/billing-officer/meter-reading';
import { ConnectionForReading } from '../../models/billing-officer/connection-for-reading.dto';
import { MeterReadingResponse } from '../../models/billing-officer/meter-reading-response.dto';
import { NotificationService } from '../notification/notificationService';
import { API_BASE_URL } from '../../config/api.config';

@Injectable({
  providedIn: 'root'
})
export class MeterReadingService {
  private http = inject(HttpClient);
  private notificationService = inject(NotificationService);

  getConnectionsNeedingReadings(): Observable<ConnectionForReading[]> {
    return this.http.get<ConnectionForReading[]>(`${API_BASE_URL}/meterreading/connections`).pipe(
      catchError(error => {
        this.notificationService.show('Failed to load connections.', 'error');
        return throwError(() => error);
      })
    );
  }

  getPreviousReading(connectionId: string): Observable<number> {
    return this.http.get<number>(`${API_BASE_URL}/meterreading/previous/${connectionId}`).pipe(
      catchError(error => {
        return throwError(() => error);
      })
    );
  }

  getReadings(filters?: {
    startDate?: string;
    endDate?: string;
    utilityTypeId?: string;
    consumerName?: string;
    status?: string;
    page?: number;
    pageSize?: number;
  }): Observable<MeterReadingResponse[]> {
    let url = `${API_BASE_URL}/meterreading`;
    const params: string[] = [];
    
    if (filters) {
      if (filters.startDate) params.push(`startDate=${filters.startDate}`);
      if (filters.endDate) params.push(`endDate=${filters.endDate}`);
      if (filters.utilityTypeId) params.push(`utilityTypeId=${filters.utilityTypeId}`);
      if (filters.consumerName) params.push(`consumerName=${encodeURIComponent(filters.consumerName)}`);
      if (filters.status) params.push(`status=${filters.status}`);
      if (filters.page) params.push(`page=${filters.page}`);
      if (filters.pageSize) params.push(`pageSize=${filters.pageSize}`);
    }
    
    if (params.length > 0) {
      url += '?' + params.join('&');
    }

    return this.http.get<MeterReadingResponse[]>(url).pipe(
      catchError(error => {
        this.notificationService.show('Failed to load meter reading history.', 'error');
        return throwError(() => error);
      })
    );
  }

  createReading(request: MeterReadingRequest): Observable<MeterReadingResponse> {
    let readingDate: string;
    if (request.readingDate.match(/^\d{4}-\d{2}-\d{2}$/)) {
      readingDate = `${request.readingDate}T00:00:00.000Z`;
    } else {
      readingDate = new Date(request.readingDate).toISOString();
    }
    
    const payload = {
      ...request,
      readingDate
    };

    return this.http.post<MeterReadingResponse>(`${API_BASE_URL}/meterreading`, payload).pipe(
      tap(() => {
        this.notificationService.show('Meter reading recorded successfully.', 'success');
      }),
      catchError(error => {
        const errorMessage = error?.error?.error?.message || 'Failed to create meter reading.';
        this.notificationService.show(errorMessage, 'error');
        return throwError(() => error);
      })
    );
  }

  updateReading(id: string, currentReading: number): Observable<MeterReadingResponse> {
    return this.http.put<MeterReadingResponse>(`${API_BASE_URL}/meterreading/${id}`, {
      currentReading
    }).pipe(
      tap(() => {
        this.notificationService.show('Meter reading updated successfully.', 'success');
      }),
      catchError(error => {
        const errorMessage = error?.error?.error?.message || 'Failed to update meter reading.';
        this.notificationService.show(errorMessage, 'error');
        return throwError(() => error);
      })
    );
  }

  getReadingById(id: string): Observable<MeterReadingResponse> {
    return this.http.get<MeterReadingResponse>(`${API_BASE_URL}/meterreading/${id}`).pipe(
      catchError(error => {
        this.notificationService.show('Failed to load meter reading.', 'error');
        return throwError(() => error);
      })
    );
  }
}

