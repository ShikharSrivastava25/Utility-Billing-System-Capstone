import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { catchError, map, Observable, tap, throwError } from 'rxjs';
import { LogService } from '../shared/logService';
import { Role, User } from '../../models/user';
import { Router } from '@angular/router';
import { LoginRequest } from '../../models/auth/login-request';
import { API_BASE_URL } from '../../config/api.config';
import { LoginResponse } from '../../models/auth/login-response';
import { RegisterRequestDto } from '../../models/auth/register-request';
import { RegisterResponseDto } from '../../models/auth/register-response';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private http = inject(HttpClient);
  private logService = inject(LogService);
  private router = inject(Router);
  currentUser = signal<User | null>(null);

  constructor() {
    this.checkAuthStatus();
  }

  saveToken(token: string): void {
    localStorage.setItem('token', token);
  }

  getToken(): string | null {
    return localStorage.getItem('token');
  }

  private removeToken(): void {
    localStorage.removeItem('token');
  }

  private checkAuthStatus(): void {
    const token = this.getToken();
    if (token) {
      try {
        const parts = token.split('.');
        if (parts.length !== 3) {
          this.removeToken();
          return;
        }
        
        const payload = JSON.parse(atob(parts[1]));
        
        // Try different claim types for user ID
        const userId = payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] 
          || payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier']
          || payload.sub 
          || payload.nameid
          || payload.userId
          || '';
          
        // Try different claim types for name
        const name = payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name']
          || payload.name
          || payload.unique_name
          || '';
          
        // Try different claim types for email
        const email = payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress']
          || payload.email
          || '';
          
        // Try different claim types for role
        const role = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
          || payload.role
          || Role.Consumer;
        
        if (userId) {
          const user: User = {
            id: userId,
            name: name,
            email: email,
            role: role,
            status: 'Active'
          };
          this.currentUser.set(user);
        } else {
          this.removeToken();
        }
      } catch (e) {
        console.error('Invalid token found, logging out.', e);
        this.removeToken();
      }
    }
  }

  login(credentials: LoginRequest): Observable<User> {
    return this.http.post<LoginResponse>(`${API_BASE_URL}/auth/login`, {
      email: credentials.email,
      password: credentials.password
    }).pipe(
      tap(response => {
        this.saveToken(response.token);
        this.currentUser.set(response.user);
        if (response.user.role === Role.Admin) {
          this.logService.logAction('ADMIN_LOGIN', `Admin user '${response.user.name}' logged in.`, response.user.name);
        }
      }),
      map(response => response.user),
      catchError(error => {
        const message = error.error?.error?.message || 'Invalid email or password';
        return throwError(() => new Error(message));
      })
    );
  }

  register(userData: RegisterRequestDto): Observable<RegisterResponseDto> {
    return this.http.post<RegisterResponseDto>(`${API_BASE_URL}/auth/register`, userData).pipe(
      catchError(error => {
        let message = 'Registration failed';
        
        if (error?.error?.error?.message) {
          message = error.error.error.message;
        } else if (error?.error?.message) {
          message = error.error.message;
        } else if (error?.message) {
          message = error.message;
        }
        
        return throwError(() => ({ message }));
      })
    );
  }

  logout(): void {
    const user = this.currentUser();
    if (user && user.role === Role.Admin) {
      this.logService.logAction('ADMIN_LOGOUT', `Admin user '${user.name}' logged out.`, user.name);
    }
    this.removeToken();
    this.currentUser.set(null);
    this.router.navigate(['/auth']);
  }
}

