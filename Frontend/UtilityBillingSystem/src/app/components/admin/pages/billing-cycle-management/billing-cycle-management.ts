import { CommonModule } from '@angular/common';
import { Component, inject, signal, ViewChild, OnInit } from '@angular/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatSortModule, MatSort } from '@angular/material/sort';
import { MatPaginatorModule, MatPaginator } from '@angular/material/paginator';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { debounceTime, startWith } from 'rxjs';
import { BillingCycleForm } from '../../forms/billing-cycle-form/billing-cycle-form';
import { BillingCycleService } from '../../../../services/billing/billingCycleService';
import { NotificationService } from '../../../../services/notification/notificationService';
import { BillingCycle } from '../../../../models/billing/billing-cycle';
import { ConfirmationDialogComponent } from '../../../shared/confirmation-dialog/confirmation-dialog';

@Component({
  selector: 'app-billing-cycle-management',
  imports: [
    CommonModule, 
    MatProgressSpinnerModule, 
    MatTableModule, 
    MatSortModule, 
    MatPaginatorModule, 
    MatButtonModule, 
    MatChipsModule, 
    MatIconModule, 
    MatTooltipModule, 
    BillingCycleForm, 
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    ReactiveFormsModule
  ],
  templateUrl: './billing-cycle-management.html',
  styleUrl: './billing-cycle-management.css',
})
export class BillingCycleManagement implements OnInit {
  private cycleService = inject(BillingCycleService);
  private notificationService = inject(NotificationService);
  private dialog = inject(MatDialog);

  displayedColumns: string[] = ['name', 'generationDay', 'dueDateOffset', 'gracePeriod', 'status', 'actions'];
  dataSource = new MatTableDataSource<BillingCycle>([]);
  searchControl = new FormControl('');
  
  @ViewChild(MatSort) set sort(sort: MatSort) {
    this.dataSource.sort = sort;
  }
  @ViewChild(MatPaginator) set paginator(paginator: MatPaginator) {
    this.dataSource.paginator = paginator;
  }
  
  isFormVisible = signal(false);
  selectedCycle = signal<BillingCycle | null>(null);
  isLoading = signal(true);

  constructor() {
    // Case-insensitive sorting
    this.dataSource.sortingDataAccessor = (item: BillingCycle, property: string) => {
      const value = item[property as keyof BillingCycle];
      if (typeof value === 'string') {
        return value.toLowerCase();
      }
      if (typeof value === 'boolean') {
        return value ? 1 : 0;
      }
      return value as string | number;
    };

    this.dataSource.filterPredicate = (data: BillingCycle, filter: string) => {
      const searchTerm = filter.toLowerCase();
      const status = data.isActive ? 'active' : 'inactive';
      return data.name.toLowerCase().includes(searchTerm) ||
             data.generationDay.toString().includes(searchTerm) ||
             data.dueDateOffset.toString().includes(searchTerm) ||
             data.gracePeriod.toString().includes(searchTerm) ||
             status.includes(searchTerm);
    };
  }

  ngOnInit() {
    this.loadCycles();

    this.searchControl.valueChanges.pipe(
      debounceTime(200),
      startWith('')
    ).subscribe(value => {
      this.dataSource.filter = value?.trim().toLowerCase() || '';
    });
  }

  loadCycles() {
    this.isLoading.set(true);
    this.cycleService.getBillingCycles().subscribe(cycles => {
      this.dataSource.data = cycles;
      this.isLoading.set(false);
    });
  }

  showCreateForm() {
    this.selectedCycle.set(null);
    this.isFormVisible.set(true);
  }

  showEditForm(cycle: BillingCycle) {
    this.selectedCycle.set(cycle);
    this.isFormVisible.set(true);
  }

  hideForm() {
    this.isFormVisible.set(false);
    this.selectedCycle.set(null);
  }

  handleSave(cycle: BillingCycle | Omit<BillingCycle, 'id'>) {
    const isUpdate = 'id' in cycle;
    const operation$ = isUpdate
      ? this.cycleService.updateBillingCycle(cycle)
      : this.cycleService.createBillingCycle(cycle);

    operation$.subscribe({
      next: (savedCycle) => {
        const message = isUpdate
          ? `Billing cycle '${savedCycle.name}' updated successfully.`
          : `Billing cycle '${savedCycle.name}' created successfully.`;
        this.notificationService.show(message);
        this.hideForm();
        this.loadCycles();
      },
      error: (err) => {
        const message = err.error?.error?.message || `Failed to ${isUpdate ? 'update' : 'create'} billing cycle. Please try again.`;
        this.notificationService.show(message, 'error');
      }
    });
  }

  toggleStatus(cycle: BillingCycle) {
    const updatedCycle = { ...cycle, isActive: !cycle.isActive };
    this.cycleService.updateBillingCycle(updatedCycle).subscribe({
      next: () => {
        const newStatus = updatedCycle.isActive ? 'Active' : 'Inactive';
        this.notificationService.show(`Billing cycle '${cycle.name}' is now ${newStatus}.`);
        this.loadCycles();
      },
      error: (err) => {
        const message = err.error?.error?.message || 'Failed to update billing cycle status. Please try again.';
        this.notificationService.show(message, 'error');
        this.loadCycles(); // Reload to show correct status
      }
    });
  }

  deleteCycle(cycle: BillingCycle) {
    this.dialog.open(ConfirmationDialogComponent).afterClosed().subscribe(result => {
      if (result) {
        this.cycleService.deleteBillingCycle(cycle.id).subscribe({
          next: () => {
            this.notificationService.show(`Billing cycle '${cycle.name}' deleted successfully.`);
            this.loadCycles();
          },
          error: (err) => {
            const message = err.error?.error?.message || 'Failed to delete billing cycle. Please try again.';
            this.notificationService.show(message, 'error');
          }
        });
      }
    });
  }

  formatOrdinal(day: number | null | undefined): string {
    const n = Math.abs(Math.trunc(Number(day)));
    if (!Number.isFinite(n)) return '';

    const mod100 = n % 100;
    if (mod100 >= 11 && mod100 <= 13) return `${n}th`;

    switch (n % 10) {
      case 1: return `${n}st`;
      case 2: return `${n}nd`;
      case 3: return `${n}rd`;
      default: return `${n}th`;
    }
  }
}
