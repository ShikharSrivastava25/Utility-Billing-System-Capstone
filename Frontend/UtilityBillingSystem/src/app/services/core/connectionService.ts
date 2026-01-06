import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Connection } from '../../models/connection';
import { UserService } from './userService';
import { UtilityService } from './utilityService';
import { TariffService } from './tariffService';
import { combineLatest, map, Observable, tap } from 'rxjs';
import { LogService } from '../shared/logService';
import { AuthService } from '../auth/authService';
import { DisplayConnection } from '../../models/display/display-connection';
import { MessageResponseDto } from '../../models/api-response';
import { API_BASE_URL } from '../../config/api.config';

@Injectable({
  providedIn: 'root',
})
export class ConnectionService {
  private http = inject(HttpClient);
  private logService = inject(LogService);
  private authService = inject(AuthService);
  private userService = inject(UserService);
  private utilityService = inject(UtilityService);
  private tariffService = inject(TariffService);

  getConnections(): Observable<Connection[]> {
    return this.http.get<Connection[]>(`${API_BASE_URL}/connection`);
  }

  getMyConnections(): Observable<Connection[]> {
    return this.http.get<Connection[]>(`${API_BASE_URL}/connection/my-connections`);
  }

  getDisplayConnections(): Observable<DisplayConnection[]> {
    return this.getConnections().pipe(
      map((connections) => {
        return connections.map(conn => ({
          ...conn,
          consumerName: conn.userName ?? 'Unknown User',
          utilityName: conn.utilityTypeName ?? 'Unknown Utility',
          tariffName: conn.tariffName ?? 'Unknown Tariff',
        }));
      })
    );
  }

  updateConnection(updatedConnection: Connection): Observable<Connection> {
    const { id, ...updateData } = updatedConnection;
    return this.http.put<Connection>(`${API_BASE_URL}/connection/${id}`, updateData).pipe(
      tap(connection => {
        const adminName = this.authService.currentUser()?.name ?? 'System';
        this.logService.logAction('CONNECTION_UPDATE', `Updated connection for meter ${connection.meterNumber}. New status: ${connection.status}.`, adminName);
      })
    );
  }

  createConnection(newConnectionData: Omit<Connection, 'id'>): Observable<Connection> {
    return this.http.post<Connection>(`${API_BASE_URL}/connection`, newConnectionData).pipe(
      tap(connection => {
        const adminName = this.authService.currentUser()?.name ?? 'System';
        this.logService.logAction('CONNECTION_CREATE', `Created new connection for user ID ${connection.userId} with meter ${connection.meterNumber}.`, adminName);
      })
    );
  }

  deleteConnection(connectionId: string): Observable<MessageResponseDto> {
    return this.http.delete<MessageResponseDto>(`${API_BASE_URL}/connection/${connectionId}`).pipe(
      tap(() => {
        const adminName = this.authService.currentUser()?.name ?? 'System';
        this.logService.logAction('CONNECTION_DELETE', `Deleted connection (ID: ${connectionId}).`, adminName);
      })
    );
  }
}

