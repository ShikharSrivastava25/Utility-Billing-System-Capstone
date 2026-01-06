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
import { MatSelectModule } from '@angular/material/select';
import { FormControl, ReactiveFormsModule, FormGroup } from '@angular/forms';
import { MeterReadingService } from '../../../../services/billing-officer/meterReadingService';
import { ConnectionService } from '../../../../services/core/connectionService';
import { UserService } from '../../../../services/core/userService';
import { UtilityService } from '../../../../services/core/utilityService';
import { DisplayMeterReading } from '../../../../models/display/display-meter-reading';
import { combineLatest, debounceTime, map, startWith } from 'rxjs';
import { ReadingDetailsDialog } from './reading-details-dialog/reading-details-dialog';
import { MeterReadingFilters } from '../../../../models/billing-officer/meter-reading-filters';

@Component({
  selector: 'app-meter-reading-history',
  standalone: true,
  imports: [
    CommonModule,
    MatProgressSpinnerModule,
    MatTableModule,
    MatSortModule,
    MatPaginatorModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatTooltipModule,
    MatSelectModule,
    ReactiveFormsModule,
    ReadingDetailsDialog
  ],
  templateUrl: './meter-reading-history.html',
  styleUrl: './meter-reading-history.css',
})
export class MeterReadingHistory implements OnInit {
  private meterReadingService = inject(MeterReadingService);
  private connectionService = inject(ConnectionService);
  private userService = inject(UserService);
  private utilityService = inject(UtilityService);

  isLoading = signal(false);
  isDialogVisible = signal(false);
  selectedReading = signal<DisplayMeterReading | null>(null);

  filterForm = new FormGroup({
    search: new FormControl(''),
    utility: new FormControl('all'),
    status: new FormControl('all')
  });

  dataSource = new MatTableDataSource<DisplayMeterReading>([]);
  displayedColumns: string[] = ['consumerName', 'userId', 'utilityName', 'meterNumber', 'currentReading', 'month', 'recordedBy', 'status', 'actions'];

  @ViewChild(MatSort) set sort(sort: MatSort) {
    this.dataSource.sort = sort;
  }
  @ViewChild(MatPaginator) set paginator(paginator: MatPaginator) {
    this.dataSource.paginator = paginator;
  }

  utilities$ = this.utilityService.getUtilities();

  constructor() {
    this.dataSource.filterPredicate = (data: DisplayMeterReading, filter: string) => {
      const filterObj = JSON.parse(filter);
      const searchStr = filterObj.search.toLowerCase();
      
      const matchesSearch = data.consumerName.toLowerCase().includes(searchStr) ||
                           data.meterNumber.toLowerCase().includes(searchStr) ||
                           data.utilityName.toLowerCase().includes(searchStr) ||
                           data.month.toLowerCase().includes(searchStr) ||
                           (data.recordedBy ? data.recordedBy.toLowerCase().includes(searchStr) : false) ||
                           (data.userId ? data.userId.toLowerCase().includes(searchStr) : false);
      
      const matchesStatus = filterObj.status === 'all' || data.status === filterObj.status;

      return matchesSearch && matchesStatus;
    };

    this.dataSource.sortingDataAccessor = (item: DisplayMeterReading, property: string) => {
      if (property === 'status') {
        return item.status === 'ReadyForBilling' ? 0 : 1;
      }
      const value = item[property as keyof DisplayMeterReading];
      return typeof value === 'string' ? value.toLowerCase() : (value as string | number);
    };
  }

  ngOnInit() {
    this.filterForm.valueChanges.pipe(
      debounceTime(400),
      startWith(this.filterForm.value)
    ).subscribe(value => {
      this.loadReadings();
      this.dataSource.filter = JSON.stringify(value);
    });
  }

  loadReadings() {
    this.isLoading.set(true);
    
    const filters: MeterReadingFilters = {};
    const formValue = this.filterForm.value;
    
    if (formValue.utility && formValue.utility !== 'all') {
      filters.utilityTypeId = formValue.utility;
    }
    
    if (formValue.status && formValue.status !== 'all') {
      filters.status = formValue.status as 'ReadyForBilling' | 'Billed';
    }

    if (formValue.search) {
      filters.consumerName = formValue.search;
    }

    this.meterReadingService.getReadings(filters).subscribe({
      next: (readings) => {
        const mappedReadings: DisplayMeterReading[] = readings.map(r => ({
          id: r.id,
          connectionId: r.connectionId,
          userId: r.userId,
          previousReading: r.previousReading,
          currentReading: r.currentReading,
          readingDate: r.readingDate,
          consumption: r.consumption,
          status: r.status as 'ReadyForBilling' | 'Billed',
          recordedBy: r.recordedBy,
          billingCycleId: r.billingCycleId,
          consumerName: r.consumerName,
          utilityName: r.utilityName,
          meterNumber: r.meterNumber,
          month: r.month
        }));
        
        this.dataSource.data = mappedReadings;
        
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  viewDetails(reading: DisplayMeterReading) {
    this.selectedReading.set(reading);
    this.isDialogVisible.set(true);
  }

  hideDialog() {
    this.isDialogVisible.set(false);
    this.selectedReading.set(null);
  }

  onReadingUpdated() {
    this.hideDialog();
    this.loadReadings();
  }
}
