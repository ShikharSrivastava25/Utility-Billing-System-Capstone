import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, OnDestroy, signal } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatBadgeModule } from '@angular/material/badge';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { NotificationPollingService } from '../../../services/notification/notificationPollingService';
import { NotificationApiService } from '../../../services/notification/notificationApiService';
import { NotificationDialogComponent } from '../notification-dialog/notification-dialog';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-notification-bell',
  standalone: true,
  imports: [
    CommonModule,
    MatIconModule,
    MatBadgeModule,
    MatButtonModule,
    MatDialogModule
  ],
  templateUrl: './notification-bell.html',
  styleUrl: './notification-bell.css'
})
export class NotificationBellComponent implements OnInit, OnDestroy {
  private pollingService = inject(NotificationPollingService);
  private notificationApiService = inject(NotificationApiService);
  private dialog = inject(MatDialog);
  private subscriptions = new Subscription();

  unreadCount = signal<number>(0);

  ngOnInit(): void {
    // Start polling for notifications
    this.pollingService.startPolling(10000); // 10 seconds

    // Subscribe to unread count updates
    const countSubscription = this.pollingService.unreadCount$.subscribe(count => {
      this.unreadCount.set(count);
    });
    this.subscriptions.add(countSubscription);

    // Get initial unread count
    this.loadUnreadCount();
  }

  ngOnDestroy(): void {
    this.pollingService.stopPolling();
    this.subscriptions.unsubscribe();
  }

  openNotificationDialog(): void {
    const dialogRef = this.dialog.open(NotificationDialogComponent, {
      width: '500px',
      maxWidth: '90vw',
      disableClose: false
    });

    // Refresh count when dialog closes
    dialogRef.afterClosed().subscribe(() => {
      this.loadUnreadCount();
      this.pollingService.refreshNotifications();
    });
  }

  private loadUnreadCount(): void {
    this.notificationApiService.getUnreadCount().subscribe({
      next: (count) => this.unreadCount.set(count),
      error: (error) => console.error('Error loading unread count:', error)
    });
  }
}

