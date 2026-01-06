import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { AuthService } from '../../services/auth/authService';
import { SidebarComponent } from '../shared/sidebar/sidebar';
import { NavigationService } from '../../services/shared/navigationService';
import { NotificationBellComponent } from './notification-bell/notification-bell';
import { NotificationPollingService } from '../../services/notification/notificationPollingService';
import { NotificationService } from '../../services/notification/notificationService';
import { Subscription } from 'rxjs';
import { Notification } from '../../models/notification';

@Component({
  selector: 'app-consumer',
  imports: [
    CommonModule,
    RouterOutlet,
    MatSidenavModule,
    MatListModule,
    MatButtonModule,
    MatIconModule,
    SidebarComponent,
    NotificationBellComponent
  ],
  templateUrl: './consumer.html',
  styleUrl: './consumer.css',
})
export class Consumer implements OnInit, OnDestroy {
  authService = inject(AuthService);
  private navigationService = inject(NavigationService);
  private pollingService = inject(NotificationPollingService);
  private notificationService = inject(NotificationService);
  private subscriptions = new Subscription();

  currentUser = this.authService.currentUser;
  navItems = this.navigationService.navItems;
  pageTitle = this.navigationService.pageTitle;

  ngOnInit(): void {
    const newNotificationsSubscription = this.pollingService.newNotifications$.subscribe(
      (notifications: Notification[]) => {
        console.log('Consumer received new notifications:', notifications);
        notifications.forEach(notification => {
          console.log('Showing toast for notification:', notification.title, notification.message);
          try {
            this.notificationService.showNotificationToast(
              notification.title,
              notification.message,
              notification.type
            );
            console.log('Toast method called successfully');
          } catch (error) {
            console.error('Error showing toast:', error);
          }
        });
      }
    );
    this.subscriptions.add(newNotificationsSubscription);
    console.log('Consumer component subscribed to newNotifications$');
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  logout() {
    this.authService.logout();
  }
}
