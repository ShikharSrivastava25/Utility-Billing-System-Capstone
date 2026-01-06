import { inject, Injectable, computed } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, NavigationEnd, Router } from '@angular/router';
import { filter, map, mergeMap, startWith } from 'rxjs';
import { AuthService } from '../auth/authService';
import { ROLE_NAVIGATION, DEFAULT_ROUTES } from '../../config/navigation.config';
import { Role } from '../../models/user';

@Injectable({ providedIn: 'root' })
export class NavigationService {
  private authService = inject(AuthService);
  private router = inject(Router);
  private activatedRoute = inject(ActivatedRoute);

  navItems = computed(() => {
    const user = this.authService.currentUser();
    return user ? ROLE_NAVIGATION[user.role] || [] : [];
  });

  pageTitle = toSignal(
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      startWith(null),
      map(() => {
        let route = this.activatedRoute;
        while (route.firstChild) {
          route = route.firstChild;
        }
        return route;
      }),
      mergeMap((route) => route.title),
      map((title) => title ?? 'Dashboard')
    ),
    { initialValue: 'Dashboard' }
  );

  getDefaultRoute(role: Role): string {
    return DEFAULT_ROUTES[role] || '/auth';
  }
}
