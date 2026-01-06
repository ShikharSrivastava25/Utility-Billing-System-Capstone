import { Component, OnDestroy, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormGroup, FormControl, Validators, ReactiveFormsModule, AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { AuthService } from '../../../services/auth/authService';
import { RegisterRequestDto } from '../../../models/auth/register-request';
import { Subject, takeUntil } from 'rxjs';
import { trigger, state, style, animate, transition } from '@angular/animations';

@Component({
  selector: 'app-register',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    RouterModule
  ],
  templateUrl: './register.html',
  styleUrl: './register.css',
  animations: [
    trigger('pageTransition', [
      transition(':enter', [
        style({ opacity: 0, transform: 'translateY(20px)' }),
        animate('600ms ease-out', style({ opacity: 1, transform: 'translateY(0)' }))
      ]),
      state('visible', style({ opacity: 1, transform: 'translateY(0)' })),
      state('hidden', style({ opacity: 0, transform: 'translateY(-20px)' })),
      transition('visible => hidden', [
        animate('600ms ease-in')
      ])
    ])
  ]
})
export class Register implements OnDestroy {
  error = signal<string>('');
  success = signal<string>('');
  hidePassword = signal(true);
  hideConfirmPassword = signal(true);
  isRedirecting = signal(false);
  private destroy$ = new Subject<void>();
  private redirectInterval: any;

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnDestroy(): void {
    if (this.redirectInterval) {
      clearInterval(this.redirectInterval);
    }
    this.destroy$.next();
    this.destroy$.complete();
  }

  passwordMatchValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
    const password = control.get('password');
    const confirmPassword = control.get('confirmPassword');

    return password && confirmPassword && password.value !== confirmPassword.value
      ? { passwordMismatch: true }
      : null;
  };

  registerForm = new FormGroup({
    name: new FormControl<string>('', {
      nonNullable: true,
      validators: [Validators.required]
    }),
    email: new FormControl<string>('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email]
    }),
    password: new FormControl<string>('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(6)]
    }),
    confirmPassword: new FormControl<string>('', {
      nonNullable: true,
      validators: [Validators.required]
    })
  }, { validators: this.passwordMatchValidator });

  submit(): void {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    const payload: RegisterRequestDto = {
      name: this.registerForm.controls.name.value,
      email: this.registerForm.controls.email.value,
      password: this.registerForm.controls.password.value
    };

    this.authService.register(payload)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.error.set('');
          let seconds = 3;
          this.success.set(`Registration successful. Redirecting to login in ${seconds}s...`);

          this.redirectInterval = setInterval(() => {
            seconds--;
            if (seconds > 0) {
              this.success.set(`Registration successful. Redirecting to login in ${seconds}s...`);
            } else {
              clearInterval(this.redirectInterval);
              this.isRedirecting.set(true);
              setTimeout(() => {
                this.router.navigate(['/auth']);
              }, 600);
            }
          }, 1000);
        },
        error: (err) => {
          this.success.set('');
          const message = err?.message || err?.error?.message || 'Registration failed. Please try again.';
          this.error.set(message);
          console.error('Registration failed:', err);
        }
      });
  }
}
