import { Routes } from '@angular/router';
import { Auth } from './components/auth/auth/auth';
import { authGuard, loginGuard } from './auth-guard/AuthGuard';
import { Admin } from './components/admin/admin';
import { Role } from './models/user';
import { AdminReports } from './components/admin/pages/admin-reports/admin-reports';
import { UserManagement } from './components/admin/pages/user-management/user-management';
import { UtilityManagement } from './components/admin/pages/utility-management/utility-management';
import { TariffManagement } from './components/admin/pages/tariff-management/tariff-management';
import { BillingCycleManagement } from './components/admin/pages/billing-cycle-management/billing-cycle-management';
import { ConnectionManagement } from './components/admin/pages/connection-management/connection-management';
import { RequestManagement } from './components/admin/pages/request-management/request-management';
import { AuditLogs } from './components/admin/pages/audit-logs/audit-logs';
import { Consumer } from './components/consumer/consumer';
import { MyRequests } from './components/consumer/pages/my-requests/my-requests';
import { ConsumerDashboard } from './components/consumer/pages/dashboard/dashboard';
import { ViewBills } from './components/consumer/pages/view-bills/view-bills';
import { PaymentHistory } from './components/consumer/pages/payment-history/payment-history';
import { ConsumptionDetailsComponent } from './components/consumer/pages/consumption-details/consumption-details';
import { Register } from './components/auth/register/register';
import { BillingOfficer } from './components/billing-officer/billing-officer';
import { MeterReadingEntry } from './components/billing-officer/pages/meter-reading-entry/meter-reading-entry';
import { MeterReadingHistory } from './components/billing-officer/pages/meter-reading-history/meter-reading-history';
import { BillGeneration } from './components/billing-officer/pages/bill-generation/bill-generation';
import { AccountOfficer } from './components/account-officer/account-officer';
import { AccountOfficerDashboard } from './components/account-officer/pages/dashboard/dashboard';
import { TrackPayments } from './components/account-officer/pages/track-payments/track-payments';
import { OutstandingBalances } from './components/account-officer/pages/outstanding-balances/outstanding-balances';
import { ConsumerBillingSummary } from './components/account-officer/pages/billing-summary/billing-summary';

export const routes: Routes = [
  { 
    path: 'auth', 
    component: Auth, 
    canActivate: [loginGuard],
    title: 'Login'
  },
  { 
    path: 'register', 
    component: Register, 
    canActivate: [loginGuard],
    title: 'Register'
  },
  {
    path: 'admin',
    component: Admin,
    canActivate: [authGuard],
    data: { roles: [Role.Admin] },
    children: [
      { path: '', redirectTo: 'reports', pathMatch: 'full' },
      { path: 'reports', component: AdminReports, title: 'Reports & Analytics' },
      { path: 'users', component: UserManagement, title: 'User Management' },
      { path: 'utilities', component: UtilityManagement, title: 'Utility Types' },
      { path: 'tariffs', component: TariffManagement, title: 'Tariff Plans' },
      { path: 'billing', component: BillingCycleManagement, title: 'Billing Cycles' },
      { path: 'connections', component: ConnectionManagement, title: 'Connections' },
      { path: 'requests', component: RequestManagement, title: 'Service Requests' },
      { path: 'logs', component: AuditLogs, title: 'Audit Logs' },
    ]
  },
  {
    path: 'billing',
    component: BillingOfficer,
    canActivate: [authGuard],
    data: { roles: [Role.BillingOfficer] },
    children: [
      { path: '', redirectTo: 'meter-readings/entry', pathMatch: 'full' },
      { path: 'meter-readings/entry', component: MeterReadingEntry, title: 'Meter Reading Entry' },
      { path: 'meter-readings/history', component: MeterReadingHistory, title: 'Reading History' },
      { path: 'bill-generation', component: BillGeneration, title: 'Bill Generation' },
    ]
  },
  {
    path: 'consumer',
    component: Consumer,
    canActivate: [authGuard],
    data: { roles: [Role.Consumer] },
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: ConsumerDashboard, title: 'Dashboard' },
      { path: 'bills', component: ViewBills, title: 'View Bills' },
      { path: 'payments', component: PaymentHistory, title: 'Payment History' },
      { path: 'consumption', component: ConsumptionDetailsComponent, title: 'Consumption Details' },
      { path: 'requests', component: MyRequests, title: 'My Requests' },
    ]
  },
  {
    path: 'account-officer',
    component: AccountOfficer,
    canActivate: [authGuard],
    data: { roles: [Role.AccountOfficer] },
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: AccountOfficerDashboard, title: 'Dashboard' },
      { path: 'payments', component: TrackPayments, title: 'Track Payments' },
      { path: 'outstanding', component: OutstandingBalances, title: 'Outstanding Balances' },
      { path: 'billing-summary', component: ConsumerBillingSummary, title: 'Consumer Billing Summary' },
    ]
  },
  { path: '', redirectTo: '/auth', pathMatch: 'full' },
  { path: '**', redirectTo: '/auth' }
];
