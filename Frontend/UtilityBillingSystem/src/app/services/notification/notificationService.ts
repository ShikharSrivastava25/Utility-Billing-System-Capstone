import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ToastrService } from 'ngx-toastr';

@Injectable({
  providedIn: 'root',
})
export class NotificationService {
  private snackBar = inject(MatSnackBar);
  private toastr = inject(ToastrService);

  show(message: string, type: 'success' | 'error' = 'success', duration: number = 3000): void {
    this.snackBar.open(message, 'Close', {
      duration: duration,
      horizontalPosition: 'end',
      verticalPosition: 'top',
      panelClass: type === 'success' ? 'snackbar-success' : 'snackbar-error',
    });
  }

  hide(): void {
    this.snackBar.dismiss();
  }

  showToast(message: string, title: string, type: 'success' | 'info' | 'warning' | 'error' = 'info'): void {
    switch (type) {
      case 'success':
        this.toastr.success(message, title);
        break;
      case 'info':
        this.toastr.info(message, title);
        break;
      case 'warning':
        this.toastr.warning(message, title);
        break;
      case 'error':
        this.toastr.error(message, title);
        break;
    }
  }

  showNotificationToast(title: string, message: string, notificationType: string): void {
    let toastType: 'success' | 'info' | 'warning' | 'error' = 'info';
    
    const type = notificationType?.toLowerCase();
    
    if (type === 'billgenerated' || type === 'paymentreceived') {
      toastType = 'success';
    } else if (type === 'duedatereminder' || type === 'overdue') {
      toastType = 'warning';
    } else if (type === 'error' || type === 'failed') {
      toastType = 'error';
    }
    
    this.showToast(message, title || 'Notification', toastType);
  }
}

