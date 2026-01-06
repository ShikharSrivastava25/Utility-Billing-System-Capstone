import { CommonModule } from '@angular/common';
import { Component, inject, ViewChild, OnInit } from '@angular/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { debounceTime } from 'rxjs';
import { LogService } from '../../../../services/shared/logService';
import { AuditLog } from '../../../../models/audit-log';

@Component({
  selector: 'app-audit-logs',
  imports: [
    CommonModule, 
    MatProgressSpinnerModule, 
    MatTableModule, 
    MatSortModule, 
    MatPaginatorModule, 
    MatChipsModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    ReactiveFormsModule
  ],
  templateUrl: './audit-logs.html',
  styleUrl: './audit-logs.css',
})
export class AuditLogs implements OnInit {
  private logService = inject(LogService);
  displayedColumns: string[] = ['timestamp', 'action', 'details', 'performedBy'];
  dataSource = new MatTableDataSource<AuditLog>([]);
  searchControl = new FormControl('');
  isLoading = true;
  
  @ViewChild(MatSort) sort!: MatSort;
  @ViewChild(MatPaginator) paginator!: MatPaginator;

  constructor() {
    // Case-insensitive sorting
    this.dataSource.sortingDataAccessor = (item: AuditLog, property: string) => {
      switch (property) {
        case 'timestamp':
          return item.timestamp instanceof Date ? item.timestamp.getTime() : new Date(item.timestamp).getTime();
        case 'action':
          return item.action.toLowerCase();
        case 'details':
          return item.details.toLowerCase();
        case 'performedBy':
          return item.performedBy.toLowerCase();
        default: {
          const value = item[property as keyof AuditLog];
          if (value instanceof Date) {
            return value.getTime();
          }
          return typeof value === 'string' ? value.toLowerCase() : (value as string | number);
        }
      }
    };

    this.dataSource.filterPredicate = (data: AuditLog, filter: string) => {
      const searchTerm = filter.toLowerCase();
      return data.action.toLowerCase().includes(searchTerm) ||
             data.details.toLowerCase().includes(searchTerm) ||
             data.performedBy.toLowerCase().includes(searchTerm) ||
             new Date(data.timestamp).toDateString().toLowerCase().includes(searchTerm) ||
             new Date(data.timestamp).toLocaleDateString().toLowerCase().includes(searchTerm);
    };
  }

  ngOnInit() {
    this.logService.getLogs().subscribe(logs => {
      this.dataSource.data = logs;
      this.isLoading = false;
      setTimeout(() => {
        this.dataSource.sort = this.sort;
        this.dataSource.paginator = this.paginator;
      });
    });

    this.searchControl.valueChanges.pipe(debounceTime(200)).subscribe(value => {
      this.dataSource.filter = value?.trim().toLowerCase() || '';
    });
  }

  getActionClass(action: string): string {
    if (action.includes('CREATE') || action.includes('APPROVE')) return 'action-create';
    if (action.includes('UPDATE')) return 'action-update';
    if (action.includes('REJECT') || action.includes('DEACTIVATE')) return 'action-reject';
    return '';
  }
}
