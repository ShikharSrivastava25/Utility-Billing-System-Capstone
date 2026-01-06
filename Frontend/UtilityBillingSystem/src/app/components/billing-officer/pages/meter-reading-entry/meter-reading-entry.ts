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
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MeterReadingService } from '../../../../services/billing-officer/meterReadingService';
import { ConnectionForReading } from '../../../../models/billing-officer/connection-for-reading.dto';
import { debounceTime, startWith } from 'rxjs';
import { MeterReadingForm } from './meter-reading-form/meter-reading-form';

@Component({
  selector: 'app-meter-reading-entry',
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
    ReactiveFormsModule,
    MeterReadingForm
  ],
  templateUrl: './meter-reading-entry.html',
  styleUrl: './meter-reading-entry.css',
})
export class MeterReadingEntry implements OnInit {
  private meterReadingService = inject(MeterReadingService);

  isFormVisible = signal(false);
  selectedConnection = signal<ConnectionForReading | null>(null);
  searchControl = new FormControl('');
  isLoading = signal(false);

  dataSource = new MatTableDataSource<ConnectionForReading>([]);
  displayedColumns: string[] = ['consumerName', 'userId', 'utilityName', 'meterNumber', 'previousReading', 'actions'];

  @ViewChild(MatSort) set sort(sort: MatSort) {
    this.dataSource.sort = sort;
  }
  @ViewChild(MatPaginator) set paginator(paginator: MatPaginator) {
    this.dataSource.paginator = paginator;
  }

  constructor() {
    this.dataSource.filterPredicate = (data: ConnectionForReading, filter: string) => {
      const searchStr = filter.toLowerCase();
      return data.consumerName.toLowerCase().includes(searchStr) ||
             data.meterNumber.toLowerCase().includes(searchStr) ||
             data.utilityName.toLowerCase().includes(searchStr) ||
             data.userId.toLowerCase().includes(searchStr) ||
             (data.previousReading !== null ? data.previousReading.toString().includes(searchStr) : false);
    };

    this.dataSource.sortingDataAccessor = (item: ConnectionForReading, property: string) => {
      const value = item[property as keyof ConnectionForReading];
      return typeof value === 'string' ? value.toLowerCase() : (value as string | number);
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
    this.meterReadingService.getConnectionsNeedingReadings().subscribe({
      next: (connections) => {
        this.dataSource.data = connections;
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  openReadingForm(connection: ConnectionForReading) {
    this.selectedConnection.set(connection);
    this.isFormVisible.set(true);
  }

  hideForm() {
    this.isFormVisible.set(false);
    this.selectedConnection.set(null);
  }

  onReadingSaved() {
    this.hideForm();
    this.loadConnections();
  }
}
