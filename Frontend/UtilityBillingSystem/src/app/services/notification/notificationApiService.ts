import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Notification } from '../../models/notification';
import { MessageResponseDto } from '../../models/api-response';
import { API_BASE_URL } from '../../config/api.config';

@Injectable({
  providedIn: 'root',
})
export class NotificationApiService {
  private http = inject(HttpClient);

  getNotifications(unreadOnly?: boolean): Observable<Notification[]> {
    let params: Record<string, string> | undefined;
    if (unreadOnly !== undefined) {
      params = { unreadOnly: unreadOnly.toString() };
    }
    return this.http.get<Notification[]>(`${API_BASE_URL}/notification`, params ? { params } : {});
  }

  getUnreadCount(): Observable<number> {
    return this.http.get<number>(`${API_BASE_URL}/notification/unread-count`);
  }

  markAsRead(notificationId: string): Observable<MessageResponseDto> {
    return this.http.put<MessageResponseDto>(`${API_BASE_URL}/notification/${notificationId}/read`, {});
  }

  markAllAsRead(): Observable<MessageResponseDto> {
    return this.http.put<MessageResponseDto>(`${API_BASE_URL}/notification/mark-all-read`, {});
  }

  deleteAllNotifications(): Observable<MessageResponseDto> {
    return this.http.delete<MessageResponseDto>(`${API_BASE_URL}/notification/all`);
  }
}

