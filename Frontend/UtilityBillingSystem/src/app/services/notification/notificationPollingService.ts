import { Injectable, inject, OnDestroy } from '@angular/core';
import { interval, Subject, Subscription, switchMap, startWith, catchError, of, timer, forkJoin } from 'rxjs';
import { NotificationApiService } from './notificationApiService';
import { Notification } from '../../models/notification';

@Injectable({
  providedIn: 'root',
})
export class NotificationPollingService implements OnDestroy {
  private notificationApiService = inject(NotificationApiService);
  private subscription?: Subscription;
  private previousNotifications: Notification[] = [];
  private isInitialized = false;
  
  private newNotificationsSubject = new Subject<Notification[]>();
  public newNotifications$ = this.newNotificationsSubject.asObservable();

  private unreadCountSubject = new Subject<number>();
  public unreadCount$ = this.unreadCountSubject.asObservable();

  startPolling(intervalMs: number = 10000): void {
    this.stopPolling();
    this.isInitialized = false;

    this.subscription = timer(0, intervalMs)
      .pipe(
        switchMap(() => 
          forkJoin({
            notifications: this.notificationApiService.getNotifications().pipe(
              catchError(error => {
                console.error('Error fetching notifications:', error);
                return of([] as Notification[]);
              })
            ),
            unreadCount: this.notificationApiService.getUnreadCount().pipe(
              catchError(error => {
                console.error('Error fetching unread count:', error);
                return of(0);
              })
            )
          })
        )
      )
      .subscribe(({ notifications, unreadCount }) => {
        if (!this.isInitialized) {
          this.previousNotifications = [...notifications];
          this.isInitialized = true;
          console.log('Initialized with existing notifications:', notifications.length);
        } else {
          // Subsequent polls: detect new notifications and trigger toasts
          this.detectNewNotifications(notifications);
          this.previousNotifications = [...notifications];
        }
        
        // Update unread count subject
        this.unreadCountSubject.next(unreadCount);
      });
  }

  stopPolling(): void {
    if (this.subscription) {
      this.subscription.unsubscribe();
      this.subscription = undefined;
    }
    this.previousNotifications = [];
    this.isInitialized = false;
  }

  private detectNewNotifications(currentNotifications: Notification[]): void {
    const previousIds = new Set(this.previousNotifications.map(n => n.id));
    const newNotifications = currentNotifications.filter(
      n => !previousIds.has(n.id)
    );

    if (newNotifications.length > 0) {
      console.log('New notifications detected:', newNotifications);
      this.newNotificationsSubject.next(newNotifications);
    }
  }

  refreshNotifications(): void {
    this.notificationApiService.getNotifications()
      .pipe(
        catchError(error => {
          console.error('Error refreshing notifications:', error);
          return of([]);
        })
      )
      .subscribe(notifications => {
        this.previousNotifications = [...notifications];
      });
  }

  ngOnDestroy(): void {
    this.stopPolling();
  }
}

