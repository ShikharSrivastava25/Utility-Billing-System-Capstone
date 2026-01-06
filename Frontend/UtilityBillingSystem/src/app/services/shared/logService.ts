import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AuditLog } from '../../models/audit-log';
import { API_BASE_URL } from '../../config/api.config';

@Injectable({
  providedIn: 'root',
})
export class LogService {
  private http = inject(HttpClient);
  private localLogs: AuditLog[] = [];

  getLogs(): Observable<AuditLog[]> {
    return this.http.get<AuditLog[]>(`${API_BASE_URL}/auditlog`);
  }

  logAction(action: string, details: string, performedBy: string): void {
    const newLog: AuditLog = {
      timestamp: new Date(),
      action,
      details,
      performedBy,
    };
    this.localLogs = [newLog, ...this.localLogs];
  }
}

