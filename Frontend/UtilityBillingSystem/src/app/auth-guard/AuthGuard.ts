import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth/authService';
import { Role } from '../models/user';
import { NavigationService } from '../services/shared/navigationService';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const navigationService = inject(NavigationService);
  const router = inject(Router);
  const currentUser = authService.currentUser();
  
  const requiredRoles = route.data['roles'] as Role[];

  if (currentUser) {
    if (requiredRoles && requiredRoles.length > 0) {
      if (requiredRoles.includes(currentUser.role)) {
        // User is logged in and has the required role
        return true;
      } else {
        // User is logged in but does not have the required role
        // Redirect to their default dashboard
        const defaultRoute = navigationService.getDefaultRoute(currentUser.role);
        router.navigate([defaultRoute]);
        return false;
      }
    }
    // Route doesn't require a specific role, just authentication
    return true;
  }

  // User is not logged in, redirect to login page
  router.navigate(['/auth'], { queryParams: { returnUrl: state.url } });
  return false;
};

export const loginGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const navigationService = inject(NavigationService);
  const router = inject(Router);
  const currentUser = authService.currentUser();

  if (currentUser) {
    // If user is already logged in, redirect them away from the auth page
    const defaultRoute = navigationService.getDefaultRoute(currentUser.role);
    router.navigate([defaultRoute]);
    return false;
  }
  
  // User is not logged in, allow access to the auth page
  return true;
};