import { Component, OnInit, ViewChild, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatSortModule, MatSort } from '@angular/material/sort';
import { MatPaginatorModule, MatPaginator } from '@angular/material/paginator';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { debounceTime, startWith } from 'rxjs';
import { AccountOfficerService } from '../../../../services/account-officer/accountOfficerService';
import { PaymentAuditDto } from '../../../../models/account-officer/payment-audit.dto';

@Component({
  selector: 'app-track-payments',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatSortModule,
    MatPaginatorModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatProgressSpinnerModule,
    ReactiveFormsModule
  ],
  templateUrl: './track-payments.html',
  styleUrl: './track-payments.css'
})
export class TrackPayments implements OnInit {
  private accountOfficerService = inject(AccountOfficerService);

  isLoading = signal(true);
  dataSource = new MatTableDataSource<PaymentAuditDto>([]);
  displayedColumns: string[] = ['date', 'consumerId', 'consumerName', 'utilityName', 'amount', 'method', 'reference'];
  searchControl = new FormControl('');

  @ViewChild(MatSort) set sort(sort: MatSort) {
    this.dataSource.sort = sort;
    if (sort) {
      this.dataSource.sortingDataAccessor = (item, property) => {
        switch (property) {
          case 'date':
            return new Date(item.date).getTime();
          case 'consumerId':
            return item.consumerId.toLowerCase();
          case 'consumerName':
            return item.consumerName.toLowerCase();
          case 'utilityName':
            return item.utilityName.toLowerCase();
          case 'method':
            return item.method.toLowerCase();
          case 'amount':
            return item.amount;
          default: {
            const value = item[property as keyof PaymentAuditDto];
            return typeof value === 'string' ? value.toLowerCase() : (value as string | number);
          }
        }
      };
    }
  }

  @ViewChild(MatPaginator) set paginator(paginator: MatPaginator) {
    this.dataSource.paginator = paginator;
  }

  ngOnInit() {
    this.loadPayments();
    this.setupFilter();
  }

  loadPayments() {
    this.isLoading.set(true);
    this.accountOfficerService.getAllPayments().subscribe({
      next: (payments) => {
        this.dataSource.data = payments;
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Error loading payments', err);
        this.isLoading.set(false);
      }
    });
  }

  setupFilter() {
    this.searchControl.valueChanges.pipe(
      debounceTime(300),
      startWith('')
    ).subscribe(value => {
      this.dataSource.filter = value?.trim().toLowerCase() || '';
    });

    this.dataSource.filterPredicate = (data: PaymentAuditDto, filter: string) => {
      const searchStr = filter.toLowerCase();
      return data.consumerName.toLowerCase().includes(searchStr) ||
             data.consumerId.toLowerCase().includes(searchStr) ||
             data.utilityName.toLowerCase().includes(searchStr) ||
             data.method.toLowerCase().includes(searchStr) ||
             data.reference.toLowerCase().includes(searchStr) ||
             data.amount.toString().includes(searchStr) ||
             new Date(data.date).toDateString().toLowerCase().includes(searchStr) ||
             new Date(data.date).toLocaleDateString().toLowerCase().includes(searchStr);
    };
  }
}
