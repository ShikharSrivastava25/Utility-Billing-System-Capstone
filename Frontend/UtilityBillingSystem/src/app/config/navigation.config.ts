import { Role } from '../models/user';
import { NavItem } from '../models/nav-item';

export const ROLE_NAVIGATION: Record<Role, NavItem[]> = {
  [Role.Admin]: [
    { label: 'Reports', route: '/admin/reports', icon: 'assessment' },
    { label: 'User Management', route: '/admin/users', icon: 'people' },
    { label: 'Utility Types', route: '/admin/utilities', icon: 'category' },
    { label: 'Tariff Plans', route: '/admin/tariffs', icon: 'description' },
    { label: 'Billing Cycles', route: '/admin/billing', icon: 'event' },
    { label: 'Connections', route: '/admin/connections', icon: 'settings_input_component' },
    { label: 'Service Requests', route: '/admin/requests', icon: 'assignment' },
    { label: 'Audit Logs', route: '/admin/logs', icon: 'history_edu' },
  ],
  [Role.AccountOfficer]: [
    { label: 'Dashboard', route: '/account-officer/dashboard', icon: 'dashboard' },
    { label: 'Payments History', route: '/account-officer/payments', icon: 'payments' },
    { label: 'Outstanding Balances', route: '/account-officer/outstanding', icon: 'account_balance_wallet' },
    { label: 'Consumer Billing Summary', route: '/account-officer/billing-summary', icon: 'summarize' },
  ],
  [Role.BillingOfficer]: [
    { label: 'Meter Reading Entry', route: '/billing/meter-readings/entry', icon: 'edit_note' },
    { label: 'Consumer Billing History', route: '/billing/meter-readings/history', icon: 'history' },
    { label: 'Bill Generation', route: '/billing/bill-generation', icon: 'receipt_long' },
  ],
  [Role.Consumer]: [
    { label: 'Dashboard', route: '/consumer/dashboard', icon: 'dashboard' },
    { label: 'View Bills and Invoice', route: '/consumer/bills', icon: 'receipt' },
    { label: 'Payment History', route: '/consumer/payments', icon: 'history' },
    { label: 'Consumption Details', route: '/consumer/consumption', icon: 'bar_chart' },
    { label: 'Service Requests', route: '/consumer/requests', icon: 'add_circle' },
  ],
};

export const DEFAULT_ROUTES: Record<Role, string> = {
  [Role.Admin]: '/admin',
  [Role.BillingOfficer]: '/billing',
  [Role.AccountOfficer]: '/account-officer',
  [Role.Consumer]: '/consumer',
};
