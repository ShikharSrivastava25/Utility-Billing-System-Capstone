import { CommonModule } from '@angular/common';
import { Component, OnDestroy, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Router, RouterModule } from '@angular/router';
import { LoginRequest } from '../../../models/auth/login-request';
import { AuthService } from '../../../services/auth/authService';
import { Role } from '../../../models/user';
import { Subject, takeUntil } from 'rxjs';
import { trigger, transition, style, animate } from '@angular/animations';

@Component({
  selector: 'app-login',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatProgressSpinnerModule,
    RouterModule
  ],
  templateUrl: './login.html',
  styleUrl: './login.css',
  animations: [
    trigger('pageTransition', [
      transition(':enter', [
        style({ opacity: 0, transform: 'translateY(20px)' }),
        animate('600ms ease-out', style({ opacity: 1, transform: 'translateY(0)' }))
      ])
    ])
  ]
})
export class Login implements OnDestroy {
  error = signal<string>('');
  success = signal<string>('');
  loading = signal(false);
  isRedirecting = signal(false);
  hidePassword = signal(true);
  private destroy$ = new Subject<void>();

  constructor(
      private authService: AuthService,
      private router: Router
  ) {}

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loginForm = new FormGroup({
    email: new FormControl<string>('', { 
      nonNullable: true,
      validators: [Validators.required, Validators.email]
    }),
    password: new FormControl<string>('', {
      nonNullable: true,
      validators: [Validators.required]
    })
  });

  submit(): void {
    if (this.loginForm.invalid || this.loading()) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.error.set('');
    this.loading.set(true);

    const credentials = this.loginForm.getRawValue();
    this.authService.login(credentials)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (user) => {
          console.log('Login successful for user:', user);
          this.loading.set(false);
          this.success.set('Login successful! Redirecting...');
          this.isRedirecting.set(true);

          setTimeout(() => {
            let route: string;
            switch (user.role) {
              case Role.Admin:
                route = '/admin';
                break;
              case Role.BillingOfficer:
                route = '/billing';
                break;
              case Role.AccountOfficer:
                route = '/account-officer';
                break;
              case Role.Consumer:
              default:
                route = '/consumer';
                break;
            }
            this.router.navigate([route]);
          }, 1000);
        },
        error: (err) => {
          this.loading.set(false);
          this.error.set(err?.message || 'An error occurred during login');
          console.error('Login failed:', err);
        }
      });
  }
}
