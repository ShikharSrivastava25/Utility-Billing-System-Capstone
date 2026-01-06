import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatButtonModule } from '@angular/material/button';
import { AuthService } from '../../services/auth/authService';
import { SidebarComponent } from '../shared/sidebar/sidebar';
import { NavigationService } from '../../services/shared/navigationService';

@Component({
  selector: 'app-billing-officer',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    MatSidenavModule,
    MatListModule,
    MatButtonModule,
    SidebarComponent
  ],
  templateUrl: './billing-officer.html',
  styleUrl: './billing-officer.css',
})
export class BillingOfficer {
  authService = inject(AuthService);
  private navigationService = inject(NavigationService);

  currentUser = this.authService.currentUser;
  navItems = this.navigationService.navItems;
  pageTitle = this.navigationService.pageTitle;

  logout() {
    this.authService.logout();
  }
}
