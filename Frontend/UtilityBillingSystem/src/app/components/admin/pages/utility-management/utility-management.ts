import { CommonModule } from '@angular/common';
import { Component, inject, signal, ViewChild, OnInit } from '@angular/core';
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
import { UtilityForm } from '../../forms/utility-form/utility-form';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { UtilityService } from '../../../../services/core/utilityService';
import { BillingCycleService } from '../../../../services/billing/billingCycleService';
import { NotificationService } from '../../../../services/notification/notificationService';
import { UtilityType } from '../../../../models/utility';
import { combineLatest, debounceTime, map, Observable } from 'rxjs';
import { BillingCycle } from '../../../../models/billing/billing-cycle';
import { DisplayUtilityType } from '../../../../models/display/display-utility-type';
import { ConfirmationDialogComponent } from '../../../shared/confirmation-dialog/confirmation-dialog';


@Component({
  selector: 'app-utility-management',
  imports: [CommonModule, MatProgressSpinnerModule, MatTableModule, MatSortModule, MatPaginatorModule, MatButtonModule, MatChipsModule, MatFormFieldModule, MatInputModule, MatIconModule, MatTooltipModule, UtilityForm, ReactiveFormsModule, MatDialogModule],
  templateUrl: './utility-management.html',
  styleUrl: './utility-management.css',
})
export class UtilityManagement implements OnInit {
  private utilityService = inject(UtilityService);
  private billingCycleService = inject(BillingCycleService);
  private notificationService = inject(NotificationService);
  private dialog = inject(MatDialog);

  displayedColumns: string[] = ['name', 'description', 'billingCycleName', 'status', 'actions'];
  dataSource = new MatTableDataSource<DisplayUtilityType>([]);
  searchControl = new FormControl('');
  
  @ViewChild(MatSort) set sort(sort: MatSort) {
    this.dataSource.sort = sort;
  }
  @ViewChild(MatPaginator) set paginator(paginator: MatPaginator) {
    this.dataSource.paginator = paginator;
  }
  
  isFormVisible = signal(false);
  selectedUtility = signal<UtilityType | null>(null);
  isLoading = signal(false);

  activeBillingCycles$: Observable<BillingCycle[]> = this.billingCycleService.getBillingCycles().pipe(
    map(cycles => cycles.filter(c => c.isActive))
  );

  constructor() {
    this.dataSource.sortingDataAccessor = (item: DisplayUtilityType, property: string) => {
      const value = item[property as keyof DisplayUtilityType];
      if (typeof value === 'string') {
        return value.toLowerCase();
      }
      return (value ?? '') as string | number;
    };

    this.dataSource.filterPredicate = (data: DisplayUtilityType, filter: string) => {
      const searchTerm = filter.toLowerCase();
      return data.name.toLowerCase().includes(searchTerm) ||
             data.description.toLowerCase().includes(searchTerm) ||
             data.billingCycleName.toLowerCase().includes(searchTerm) ||
             data.status.toLowerCase().includes(searchTerm);
    };
  }

  ngOnInit() {
    this.loadUtilities();

    this.searchControl.valueChanges.pipe(debounceTime(200)).subscribe(value => {
      this.dataSource.filter = value?.trim().toLowerCase() || '';
    });
  }

  loadUtilities() {
    this.isLoading.set(true);
    combineLatest([
      this.utilityService.getUtilities(),
      this.billingCycleService.getBillingCycles()
    ]).pipe(
      map(([utilities, cycles]) => {
        const cycleMap = new Map(cycles.map(c => [c.id, c.name]));
        return utilities.map(utility => ({
          ...utility,
          billingCycleName: utility.billingCycleId ? cycleMap.get(utility.billingCycleId) ?? 'N/A' : 'Not Assigned',
        }));
      })
    ).subscribe(utilities => {
      this.dataSource.data = utilities;
      this.isLoading.set(false);
    });
  }

  showCreateForm() {
    this.selectedUtility.set(null);
    this.isFormVisible.set(true);
  }

  showEditForm(utility: DisplayUtilityType) {
    this.selectedUtility.set(utility);
    this.isFormVisible.set(true);
  }

  hideForm() {
    this.isFormVisible.set(false);
    this.selectedUtility.set(null);
  }

  handleSave(utility: UtilityType | Omit<UtilityType, 'id'>) {
    const isUpdate = 'id' in utility;
    const operation$ = isUpdate
      ? this.utilityService.updateUtility(utility)
      : this.utilityService.createUtility(utility);
    
    operation$.subscribe({
      next: (savedUtility) => {
        const message = isUpdate 
          ? `Utility '${savedUtility.name}' updated successfully.` 
          : `Utility '${savedUtility.name}' created successfully.`;
        this.notificationService.show(message);
        this.hideForm();
        this.loadUtilities();
      },
      error: (err) => {
        const message = err.error?.error?.message || `Failed to ${isUpdate ? 'update' : 'create'} utility. Please try again.`;
        this.notificationService.show(message, 'error');
      }
    });
  }

  toggleStatus(utility: UtilityType) {
    const newStatus = utility.status === 'Enabled' ? 'Disabled' : 'Enabled';
    const updatedUtility = { ...utility, status: newStatus as 'Enabled' | 'Disabled' };
    this.utilityService.updateUtility(updatedUtility).subscribe({
      next: () => {
        this.notificationService.show(`Utility '${utility.name}' is now ${newStatus}.`);
        this.loadUtilities();
      },
      error: (err) => {
        const message = err.error?.error?.message || 'Failed to update utility status. Please try again.';
        this.notificationService.show(message, 'error');
      }
    });
  }

  deleteUtility(utility: UtilityType) {
    this.dialog.open(ConfirmationDialogComponent).afterClosed().subscribe(result => {
      if (result) {
        this.utilityService.deleteUtility(utility.id).subscribe({
          next: () => {
            this.notificationService.show(`Utility '${utility.name}' deleted successfully.`);
            this.loadUtilities();
          },
          error: (err) => {
            const message = err.error?.error?.message || 'Failed to delete utility. Please try again.';
            this.notificationService.show(message, 'error');
          }
        });
      }
    });
  }
}
