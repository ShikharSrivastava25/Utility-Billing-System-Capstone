import { CommonModule } from '@angular/common';
import { Component, inject, signal, ViewChild, OnInit } from '@angular/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatSortModule, MatSort } from '@angular/material/sort';
import { MatPaginatorModule, MatPaginator } from '@angular/material/paginator';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { ConnectionForm } from '../../forms/connection-form/connection-form';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { ConnectionService } from '../../../../services/core/connectionService';
import { UserService } from '../../../../services/core/userService';
import { UtilityService } from '../../../../services/core/utilityService';
import { TariffService } from '../../../../services/core/tariffService';
import { NotificationService } from '../../../../services/notification/notificationService';
import { Connection } from '../../../../models/connection';
import { Role, User } from '../../../../models/user';
import { combineLatest, debounceTime, map, Observable, startWith } from 'rxjs';
import { UtilityType } from '../../../../models/utility';
import { TariffPlan } from '../../../../models/tariff';
import { DisplayConnection } from '../../../../models/display/display-connection';
import { ConfirmationDialogComponent } from '../../../shared/confirmation-dialog/confirmation-dialog';

@Component({
  selector: 'app-connection-management',
  imports: [
    CommonModule,
    MatProgressSpinnerModule,
    MatTableModule,
    MatSortModule,
    MatPaginatorModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatChipsModule,
    MatIconModule,
    MatTooltipModule,
    ConnectionForm,
    ReactiveFormsModule,
    MatDialogModule
  ],
  templateUrl: './connection-management.html',
  styleUrl: './connection-management.css',
})
export class ConnectionManagement implements OnInit {
  private connectionService = inject(ConnectionService);
  private userService = inject(UserService);
  private utilityService = inject(UtilityService);
  private tariffService = inject(TariffService);
  private notificationService = inject(NotificationService);
  private dialog = inject(MatDialog);

  isFormVisible = signal(false);
  selectedConnection = signal<Connection | null>(null);
  searchControl = new FormControl('');
  isLoading = signal(false);

  // MatTable setup
  dataSource = new MatTableDataSource<DisplayConnection>([]);
  displayedColumns: string[] = ['consumerName', 'utilityName', 'meterNumber', 'tariffName', 'status', 'actions'];

  @ViewChild(MatSort) set sort(sort: MatSort) {
    this.dataSource.sort = sort;
  }
  @ViewChild(MatPaginator) set paginator(paginator: MatPaginator) {
    this.dataSource.paginator = paginator;
  }

  consumers$: Observable<User[]> = this.userService.getUsers().pipe(
    map(users => users.filter(u => u.role === Role.Consumer))
  );
  utilities$: Observable<UtilityType[]> = this.utilityService.getUtilities().pipe(
    map(utilities => utilities.filter(u => u.status === 'Enabled'))
  );
  activeTariffs$: Observable<TariffPlan[]> = this.tariffService.getTariffs().pipe(
    map(tariffs => tariffs.filter(t => t.isActive))
  );

  formData$ = combineLatest({
    consumers: this.consumers$,
    utilities: this.utilities$,
    tariffs: this.activeTariffs$
  });

  constructor() {
    this.dataSource.sortingDataAccessor = (item: DisplayConnection, property: string) => {
      const value = item[property as keyof DisplayConnection];
      if (value === undefined || value === null) {
        return '';
      }
      return typeof value === 'string' ? value.toLowerCase() : (value as string | number);
    };

    this.dataSource.filterPredicate = (data: DisplayConnection, filter: string) => {
      const searchStr = filter.toLowerCase();
      return data.consumerName.toLowerCase().includes(searchStr) ||
             data.utilityName.toLowerCase().includes(searchStr) ||
             data.meterNumber.toLowerCase().includes(searchStr) ||
             data.tariffName.toLowerCase().includes(searchStr) ||
             data.status.toLowerCase().includes(searchStr);
    };
  }

  ngOnInit() {
    this.loadConnections();

    this.searchControl.valueChanges.pipe(
      debounceTime(200),
      startWith('')
    ).subscribe(value => {
      this.dataSource.filter = value?.trim() || '';
    });
  }

  loadConnections() {
    this.isLoading.set(true);
    this.connectionService.getDisplayConnections().subscribe({
      next: (connections) => {
        this.dataSource.data = connections;
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Error loading connections:', error);
        this.dataSource.data = [];
        this.isLoading.set(false);
      }
    });
  }
  
  showCreateForm() {
    this.selectedConnection.set(null);
    this.isFormVisible.set(true);
  }

  showEditForm(connection: Connection) {
    this.selectedConnection.set(connection);
    this.isFormVisible.set(true);
  }

  hideForm() {
    this.isFormVisible.set(false);
    this.selectedConnection.set(null);
  }

  handleSave(connection: Connection | Omit<Connection, 'id'>) {
    const isUpdate = 'id' in connection;
    const operation$ = isUpdate
      ? this.connectionService.updateConnection(connection)
      : this.connectionService.createConnection(connection);

    operation$.subscribe({
      next: (savedConnection) => {
        const message = isUpdate
          ? `Connection for meter ${savedConnection.meterNumber} updated.`
          : `Connection for meter ${savedConnection.meterNumber} created.`;
        this.notificationService.show(message);
        this.loadConnections();
        this.hideForm();
      },
      error: (err) => {
        const message = err.error?.error?.message || `Failed to ${isUpdate ? 'update' : 'create'} connection. Please try again.`;
        this.notificationService.show(message, 'error');
      }
    });
  }

  toggleStatus(connection: Connection) {
    const newStatus = connection.status === 'Active' ? 'Inactive' : 'Active';
    const updatedConnection = { ...connection, status: newStatus as 'Active' | 'Inactive' };
    this.connectionService.updateConnection(updatedConnection).subscribe({
      next: () => {
        this.notificationService.show(`Connection for meter ${connection.meterNumber} is now ${newStatus}.`);
        this.loadConnections();
      },
      error: (err) => {
        const message = err.error?.error?.message || `Failed to update status for meter ${connection.meterNumber}.`;
        this.notificationService.show(message, 'error');
      }
    });
  }

  deleteConnection(connection: Connection) {
    this.dialog.open(ConfirmationDialogComponent).afterClosed().subscribe(result => {
      if (result) {
        this.connectionService.deleteConnection(connection.id).subscribe({
          next: () => {
            this.notificationService.show(`Connection for meter '${connection.meterNumber}' deleted successfully.`);
            this.loadConnections();
          },
          error: (err) => {
            const message = err.error?.error?.message || 'Failed to delete connection. Please try again.';
            this.notificationService.show(message, 'error');
          }
        });
      }
    });
  }
}
