import { CommonModule } from '@angular/common';
import { Component, inject, signal, ViewChild, OnInit } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatSortModule, MatSort } from '@angular/material/sort';
import { MatPaginatorModule, MatPaginator } from '@angular/material/paginator';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { UserForm } from '../../forms/user-form/user-form';
import { UserService } from '../../../../services/core/userService';
import { NotificationService } from '../../../../services/notification/notificationService';
import { debounceTime } from 'rxjs';
import { User } from '../../../../models/user';
import { ConfirmationDialogComponent } from '../../../shared/confirmation-dialog/confirmation-dialog';

@Component({
  selector: 'app-user-management',
  imports: [CommonModule, ReactiveFormsModule, MatProgressSpinnerModule, MatTableModule, MatSortModule, MatPaginatorModule, MatButtonModule, MatChipsModule, MatFormFieldModule, MatInputModule, MatIconModule, MatTooltipModule, UserForm, MatDialogModule],
  templateUrl: './user-management.html',
  styleUrl: './user-management.css',
})
export class UserManagement implements OnInit {
  private userService = inject(UserService);
  private notificationService = inject(NotificationService);
  private dialog = inject(MatDialog);

  displayedColumns: string[] = ['name', 'id', 'email', 'role', 'status', 'actions'];
  dataSource = new MatTableDataSource<User>([]);
  searchControl = new FormControl('');
  
  @ViewChild(MatSort) set sort(sort: MatSort) {
    this.dataSource.sort = sort;
  }
  @ViewChild(MatPaginator) set paginator(paginator: MatPaginator) {
    this.dataSource.paginator = paginator;
  }
  
  isFormVisible = signal(false);
  selectedUser = signal<User | null>(null);
  isLoading = signal(true);

  constructor() {
    this.dataSource.sortingDataAccessor = (item: User, property: string) => {
      const value = item[property as keyof User];
      if (typeof value === 'string') {
        return value.toLowerCase();
      }
      return value as string | number;
    };

    this.dataSource.filterPredicate = (data: User, filter: string) => {
      const searchTerm = filter.toLowerCase();
      return data.name.toLowerCase().includes(searchTerm) ||
             data.email.toLowerCase().includes(searchTerm) ||
             data.id.toLowerCase().includes(searchTerm) ||
             data.role.toLowerCase().includes(searchTerm) ||
             data.status.toLowerCase().includes(searchTerm);
    };
  }

  ngOnInit() {
    this.loadUsers();

    this.searchControl.valueChanges.pipe(debounceTime(200)).subscribe(value => {
      this.dataSource.filter = value?.trim().toLowerCase() || '';
    });
  }

  loadUsers() {
    this.isLoading.set(true);
    this.userService.getUsers().subscribe(users => {
      this.dataSource.data = users;
      this.isLoading.set(false);
    });
  }

  showCreateForm() {
    this.selectedUser.set(null);
    this.isFormVisible.set(true);
  }

  showEditForm(user: User) {
    this.selectedUser.set(user);
    this.isFormVisible.set(true);
  }

  hideForm() {
    this.isFormVisible.set(false);
    this.selectedUser.set(null);
  }

  handleSave(user: User | (Omit<User, 'id'> & { password?: string })) {
    const isUpdate = 'id' in user;
    const operation$ = isUpdate
      ? this.userService.updateUser(user as User)
      : this.userService.createUser(user as Omit<User, 'id'> & { password?: string });

    operation$.subscribe({
      next: (savedUser) => {
        const message = isUpdate 
          ? `User '${savedUser.name}' updated successfully.` 
          : `User '${savedUser.name}' created successfully.`;
        this.notificationService.show(message);
        this.hideForm();
        this.loadUsers();
      },
      error: (err) => {
        const message = err.error?.error?.message || 'Failed to save user. Please try again.';
        this.notificationService.show(message, 'error');
      }
    });
  }

  toggleStatus(user: User) {
    const newStatus = user.status === 'Active' ? 'Inactive' : 'Active';
    const updatedUser = { ...user, status: newStatus as 'Active' | 'Inactive' };
    this.userService.updateUser(updatedUser).subscribe({
      next: () => {
        this.notificationService.show(`User '${user.name}' is now ${newStatus}.`);
        this.loadUsers();
      },
      error: (err) => {
        const message = err.error?.error?.message || 'Failed to update user status. Please try again.';
        this.notificationService.show(message, 'error');
      }
    });
  }

  deleteUser(user: User) {
    this.dialog.open(ConfirmationDialogComponent).afterClosed().subscribe(result => {
      if (result) {
        this.userService.deleteUser(user.id).subscribe({
          next: () => {
            this.notificationService.show(`User '${user.name}' deleted successfully.`);
            this.loadUsers();
          },
          error: (err) => {
            const message = err.error?.error?.message || 'Failed to delete user. Please try again.';
            this.notificationService.show(message, 'error');
          }
        });
      }
    });
  }
}
