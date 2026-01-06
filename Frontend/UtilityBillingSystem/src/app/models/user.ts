export enum Role {
  Admin = 'Admin',
  BillingOfficer = 'Billing Officer',
  AccountOfficer = 'Account Officer',
  Consumer = 'Consumer',
}

export interface User {
  id: string;
  name: string;
  email: string;
  role: Role;
  status: 'Active' | 'Inactive';
}

