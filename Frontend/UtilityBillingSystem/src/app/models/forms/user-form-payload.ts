import { User, Role } from '../user';

export interface CreateUserPayload {
  name: string;
  email: string;
  password: string;
  role: Role;
  status: 'Active' | 'Inactive';
}

export interface UpdateUserPayload {
  name: string;
  email: string;
  role: Role;
  status: 'Active' | 'Inactive';
}

export type UserFormPayload = CreateUserPayload | UpdateUserPayload | User;

