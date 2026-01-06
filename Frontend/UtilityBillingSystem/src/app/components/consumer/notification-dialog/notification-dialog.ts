import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { NotificationApiService } from '../../../services/notification/notificationApiService';
import { Notification } from '../../../models/notification';

@Component({
  selector: 'app-notification-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatListModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './notification-dialog.html',
  styleUrl: './notification-dialog.css'
})
export class NotificationDialogComponent implements OnInit {
  private notificationApiService = inject(NotificationApiService);
  public dialogRef = inject(MatDialogRef<NotificationDialogComponent>);

  notifications = signal<Notification[]>([]);
  loading = signal<boolean>(true);

  ngOnInit(): void {
    this.loadNotifications();
  }

  loadNotifications(): void {
    this.loading.set(true);
    this.notificationApiService.getNotifications().subscribe({
      next: (notifications) => {
        this.notifications.set(notifications);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading notifications:', error);
        this.loading.set(false);
      }
    });
  }

  markAsRead(notification: Notification): void {
    if (notification.isRead) {
      return;
    }

    this.notificationApiService.markAsRead(notification.id).subscribe({
      next: () => {
        const updated = this.notifications().map(n => 
          n.id === notification.id ? { ...n, isRead: true } : n
        );
        this.notifications.set(updated);
      },
      error: (error) => {
        console.error('Error marking notification as read:', error);
      }
    });
  }

  markAllAsRead(): void {
    const notificationCount = this.notifications().length;
    if (notificationCount === 0) {
      return;
    }

    this.notificationApiService.deleteAllNotifications().subscribe({
      next: () => {
        this.notifications.set([]);
      },
      error: (error) => {
        console.error('Error deleting all notifications:', error);
      }
    });
  }

  close(): void {
    this.dialogRef.close();
  }

  formatDate(dateString: string): string {
    let date: Date;
    if (dateString.endsWith('Z')) {
      date = new Date(dateString);
    } else if (dateString.match(/[+-]\d{2}:\d{2}$/)) {
      date = new Date(dateString);
    } else {
      date = new Date(dateString + 'Z');
    }
    
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    
    if (diffMs < 0) {
      return 'Just now';
    }
    
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) {
      return 'Just now';
    } else if (diffMins < 60) {
      return `${diffMins} minute${diffMins > 1 ? 's' : ''} ago`;
    } else if (diffHours < 24) {
      return `${diffHours} hour${diffHours > 1 ? 's' : ''} ago`;
    } else if (diffDays < 7) {
      return `${diffDays} day${diffDays > 1 ? 's' : ''} ago`;
    } else {
      return date.toLocaleDateString();
    }
  }

  getUnreadCount(): number {
    return this.notifications().filter(n => !n.isRead).length;
  }

  hasNotifications(): boolean {
    return this.notifications().length > 0;
  }
}

