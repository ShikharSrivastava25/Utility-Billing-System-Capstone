import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { UtilityRequest, CreateUtilityRequestDto } from '../../models/utility-request';
import { ConnectionService } from './connectionService';
import { LogService } from '../shared/logService';
import { AuthService } from '../auth/authService';
import { UtilityService } from './utilityService';
import { combineLatest, map, Observable, startWith, Subject, switchMap, tap } from 'rxjs';
import { Connection, ConnectionApprovalDto } from '../../models/connection';
import { UtilityType } from '../../models/utility';
import { DisplayRequest } from '../../models/display/display-request';
import { API_BASE_URL } from '../../config/api.config';

@Injectable({
  providedIn: 'root',
})
export class UtilityRequestService {
  private http = inject(HttpClient);
  private connectionService = inject(ConnectionService);
  private utilityService = inject(UtilityService);
  private logService = inject(LogService);
  private authService = inject(AuthService);

  getRequests(): Observable<UtilityRequest[]> {
    return this.http.get<UtilityRequest[]>(`${API_BASE_URL}/utilityrequest`);
  }
  
  getRequestsForUser(userId: string): Observable<UtilityRequest[]> {
    return this.http.get<UtilityRequest[]>(`${API_BASE_URL}/utilityrequest/my-requests`);
  }

  getDisplayRequestsForUser(userId: string, refreshTrigger$?: Subject<void>): Observable<DisplayRequest[]> {
    const requests$ = refreshTrigger$ 
      ? refreshTrigger$.pipe(
          startWith(null),
          switchMap(() => this.getRequestsForUser(userId))
        )
      : this.getRequestsForUser(userId);
    
    return combineLatest([
      requests$,
      this.utilityService.getUtilities()
    ]).pipe(
      map(([requests, utilities]) => {
        const utilityMap = new Map(utilities.map(u => [u.id, u.name]));
        const consumerName = this.authService.currentUser()?.name ?? 'Unknown';
        const parseDate = (dateStr: string | Date): Date => {
          if (dateStr instanceof Date) return dateStr;
          const str = typeof dateStr === 'string' ? dateStr : String(dateStr);
          if (!str.includes('Z') && !str.match(/[+-]\d{2}:?\d{2}$/)) {
            return new Date(str + 'Z');
          }
          return new Date(str);
        };
        
        return requests.map(req => ({
          ...req,
          requestDate: parseDate(req.requestDate),
          decisionDate: req.decisionDate ? parseDate(req.decisionDate) : undefined,
          consumerName,
          utilityName: utilityMap.get(req.utilityTypeId) ?? 'Unknown Utility',
        })).sort((a, b) => b.requestDate.getTime() - a.requestDate.getTime());
      })
    );
  }

  getAvailableUtilitiesForUser(userId: string, refreshTrigger$?: Subject<void>): Observable<UtilityType[]> {
    const allUtilities$ = this.utilityService.getUtilities().pipe(
      map(utilities => utilities.filter(u => u.status === 'Enabled'))
    );

    const connectedUtilityIds$ = this.connectionService.getMyConnections().pipe(
      map(connections => new Set(
        connections
          .filter(c => c.userId === userId)
          .map(c => c.utilityTypeId)
      ))
    );

    const pendingRequests$ = refreshTrigger$
      ? refreshTrigger$.pipe(
          startWith(null),
          switchMap(() => this.getRequestsForUser(userId))
        )
      : this.getRequestsForUser(userId);
    
    const pendingRequestUtilityIds$ = pendingRequests$.pipe(
      map(requests => new Set(
        requests
          .filter(r => r.status === 'Pending')
          .map(r => r.utilityTypeId)
      ))
    );

    return combineLatest([
      allUtilities$,
      connectedUtilityIds$,
      pendingRequestUtilityIds$
    ]).pipe(
      map(([allUtilities, connectedIds, pendingIds]) => 
        allUtilities.filter(u => !connectedIds.has(u.id) && !pendingIds.has(u.id))
      )
    );
  }

  createRequest(requestData: CreateUtilityRequestDto): Observable<UtilityRequest> {
    return this.http.post<UtilityRequest>(`${API_BASE_URL}/utilityrequest`, requestData);
  }

  approveRequest(request: UtilityRequest, connectionDetails: ConnectionApprovalDto): Observable<Connection> {
    return this.http.post<Connection>(`${API_BASE_URL}/utilityrequest/${request.id}/approve`, connectionDetails).pipe(
      tap(() => {
        const adminName = this.authService.currentUser()?.name ?? 'System';
        this.logService.logAction('REQUEST_APPROVE', `Approved request ID ${request.id} for user ID ${request.userId}.`, adminName);
      })
    );
  }

  rejectRequest(requestId: string): Observable<UtilityRequest> {
    const adminName = this.authService.currentUser()?.name ?? 'System';
    this.logService.logAction('REQUEST_REJECT', `Rejected request ID ${requestId}.`, adminName);
    return this.http.post<UtilityRequest>(`${API_BASE_URL}/utilityrequest/${requestId}/reject`, {});
  }
}

