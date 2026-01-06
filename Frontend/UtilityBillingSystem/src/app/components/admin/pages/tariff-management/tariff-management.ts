import { CommonModule } from '@angular/common';
import { Component, inject, signal, ViewChild, OnInit } from '@angular/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatSortModule, MatSort } from '@angular/material/sort';
import { MatPaginatorModule, MatPaginator } from '@angular/material/paginator';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { TariffForm } from '../../forms/tariff-form/tariff-form';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { TariffService } from '../../../../services/core/tariffService';
import { UtilityService } from '../../../../services/core/utilityService';
import { NotificationService } from '../../../../services/notification/notificationService';
import { TariffPlan } from '../../../../models/tariff';
import { combineLatest, debounceTime, map, Observable } from 'rxjs';
import { UtilityType } from '../../../../models/utility';
import { DisplayTariff } from '../../../../models/display/display-tariff';
import { ConfirmationDialogComponent } from '../../../shared/confirmation-dialog/confirmation-dialog';

@Component({
  selector: 'app-tariff-management',
  imports: [CommonModule, MatProgressSpinnerModule, MatTableModule, MatSortModule, MatPaginatorModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatIconModule, MatTooltipModule, TariffForm, ReactiveFormsModule, MatDialogModule],
  templateUrl: './tariff-management.html',
  styleUrl: './tariff-management.css',
})
export class TariffManagement implements OnInit {
  private tariffService = inject(TariffService);
  private utilityService = inject(UtilityService);
  private notificationService = inject(NotificationService);
  private dialog = inject(MatDialog);

  displayedColumns: string[] = ['name', 'utilityName', 'baseRate', 'fixedCharge', 'taxPercentage', 'actions'];
  historicalColumns: string[] = ['name', 'utilityName', 'baseRate', 'fixedCharge', 'taxPercentage', 'createdAt'];
  
  activeDataSource = new MatTableDataSource<DisplayTariff>([]);
  historicalDataSource = new MatTableDataSource<DisplayTariff>([]);
  searchControl = new FormControl('');
  
  @ViewChild('activeSort') set activeSort(sort: MatSort) {
    this.activeDataSource.sort = sort;
  }
  @ViewChild('activePaginator') set activePaginator(paginator: MatPaginator) {
    this.activeDataSource.paginator = paginator;
  }
  @ViewChild('historicalSort') set historicalSort(sort: MatSort) {
    this.historicalDataSource.sort = sort;
  }
  @ViewChild('historicalPaginator') set historicalPaginator(paginator: MatPaginator) {
    this.historicalDataSource.paginator = paginator;
  }
  
  isFormVisible = signal(false);
  selectedTariff = signal<TariffPlan | null>(null);
  isLoading = signal(false);

  utilities$: Observable<UtilityType[]> = this.utilityService.getUtilities();

  constructor() {
    const sortingAccessor = (item: DisplayTariff, property: string) => {
      const value = item[property as keyof DisplayTariff];
      if (typeof value === 'string') {
        const dateValue = new Date(value);
        if (!isNaN(dateValue.getTime())) {
          return dateValue.getTime();
        }
        return value.toLowerCase();
      }
      if (value instanceof Date) {
        return value.getTime();
      }
      return (value ?? '') as string | number;
    };
    
    this.activeDataSource.sortingDataAccessor = sortingAccessor;
    this.historicalDataSource.sortingDataAccessor = sortingAccessor;

    const filterPredicate = (data: DisplayTariff, filter: string) => {
      const searchTerm = filter.toLowerCase();
      return data.name.toLowerCase().includes(searchTerm) ||
             data.utilityName.toLowerCase().includes(searchTerm) ||
             data.baseRate.toString().includes(searchTerm) ||
             data.fixedCharge.toString().includes(searchTerm) ||
             data.taxPercentage.toString().includes(searchTerm) ||
             (data.createdAt && new Date(data.createdAt).toDateString().toLowerCase().includes(searchTerm));
    };
    
    this.activeDataSource.filterPredicate = filterPredicate;
    this.historicalDataSource.filterPredicate = filterPredicate;
  }

  ngOnInit() {
    this.loadTariffs();

    this.searchControl.valueChanges.pipe(debounceTime(200)).subscribe(value => {
      const filterValue = value?.trim().toLowerCase() || '';
      this.activeDataSource.filter = filterValue;
      this.historicalDataSource.filter = filterValue;
    });
  }

  loadTariffs() {
    this.isLoading.set(true);
    combineLatest([
      this.tariffService.getTariffs(),
      this.utilities$
    ]).pipe(
      map(([tariffs, utilities]) => {
        const utilityMap = new Map(utilities.map(u => [u.id, u.name]));
        return tariffs
          .map(tariff => ({
            ...tariff,
            createdAt: new Date(tariff.createdAt),
            utilityName: utilityMap.get(tariff.utilityTypeId) ?? 'Unknown Utility',
          }));
      })
    ).subscribe(tariffs => {
      this.activeDataSource.data = tariffs.filter(t => t.isActive);
      this.historicalDataSource.data = tariffs.filter(t => !t.isActive);
      this.isLoading.set(false);
    });
  }

  showCreateForm() {
    this.selectedTariff.set(null);
    this.isFormVisible.set(true);
  }

  showEditForm(tariff: TariffPlan) {
    this.selectedTariff.set(tariff);
    this.isFormVisible.set(true);
  }

  hideForm() {
    this.isFormVisible.set(false);
    this.selectedTariff.set(null);
  }

  handleSave(tariffData: Omit<TariffPlan, 'id' | 'createdAt' | 'isActive'> | TariffPlan) {
    const isUpdate = 'id' in tariffData;
    const operation$ = isUpdate
      ? this.tariffService.updateTariff(tariffData)
      : this.tariffService.createTariff(tariffData);

    operation$.subscribe({
      next: (savedTariff) => {
        const message = isUpdate 
          ? `Tariff '${savedTariff.name}' updated successfully.` 
          : `Tariff '${savedTariff.name}' created successfully.`;
        this.notificationService.show(message);
        this.hideForm();
        this.loadTariffs();
      },
      error: (err) => {
        const message = err.error?.error?.message || `Failed to ${isUpdate ? 'update' : 'create'} tariff. Please try again.`;
        this.notificationService.show(message, 'error');
      }
    });
  }

  deleteTariff(tariff: DisplayTariff) {
    this.dialog.open(ConfirmationDialogComponent).afterClosed().subscribe(result => {
      if (result) {
        this.tariffService.deleteTariff(tariff.id).subscribe({
          next: () => {
            this.notificationService.show(`Tariff '${tariff.name}' deleted successfully.`);
            this.loadTariffs();
          },
          error: (err) => {
            const message = err.error?.error?.message || 'Failed to delete tariff. Please try again.';
            this.notificationService.show(message, 'error');
          }
        });
      }
    });
  }
}
