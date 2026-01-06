import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Role, User } from '../../models/user';
import { LogService } from '../shared/logService';
import { AuthService } from '../auth/authService';
import { MessageResponseDto } from '../../models/api-response';
import { Observable, tap } from 'rxjs';
import { API_BASE_URL } from '../../config/api.config';

@Injectable({
  providedIn: 'root',
})
export class UserService {
  private http = inject(HttpClient);
  private logService = inject(LogService);
  private authService = inject(AuthService);

  getUsers(): Observable<User[]> {
    return this.http.get<User[]>(`${API_BASE_URL}/user`);
  }

  updateUser(updatedUser: User): Observable<User> {
    const { id, ...updateData } = updatedUser;
    return this.http.put<User>(`${API_BASE_URL}/user/${id}`, updateData).pipe(
      tap(user => {
        const adminName = this.authService.currentUser()?.name ?? 'System';
        this.logService.logAction('USER_UPDATE', `Updated user '${user.name}' (ID: ${user.id}).`, adminName);
      })
    );
  }

  createUser(newUser: Omit<User, 'id'> & { password?: string }): Observable<User> {
    return this.http.post<User>(`${API_BASE_URL}/user`, newUser).pipe(
      tap(user => {
        const adminName = this.authService.currentUser()?.name ?? 'System';
        this.logService.logAction('USER_CREATE', `Created new user '${user.name}' with role '${user.role}'.`, adminName);
      })
    );
  }

  deleteUser(userId: string): Observable<MessageResponseDto> {
    return this.http.delete<MessageResponseDto>(`${API_BASE_URL}/user/${userId}`).pipe(
      tap(() => {
        const adminName = this.authService.currentUser()?.name ?? 'System';
        this.logService.logAction('USER_DELETE', `Deleted user (ID: ${userId}).`, adminName);
      })
    );
  }
}

