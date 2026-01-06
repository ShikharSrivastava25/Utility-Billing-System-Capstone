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
import { ConsumerBillingSummaryDto } from '../../../../models/account-officer/consumer-billing-summary.dto';

@Component({
  selector: 'app-consumer-billing-summary',
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
  templateUrl: './billing-summary.html',
  styleUrl: './billing-summary.css'
})
export class ConsumerBillingSummary implements OnInit {
  private accountOfficerService = inject(AccountOfficerService);

  isLoading = signal(true);
  dataSource = new MatTableDataSource<ConsumerBillingSummaryDto>([]);
  displayedColumns: string[] = ['consumerId', 'consumerName', 'totalBilled', 'totalPaid', 'outstandingBalance', 'overdueCount'];
  searchControl = new FormControl('');

  @ViewChild(MatSort) set sort(sort: MatSort) {
    this.dataSource.sort = sort;
  }

  @ViewChild(MatPaginator) set paginator(paginator: MatPaginator) {
    this.dataSource.paginator = paginator;
  }

  ngOnInit() {
    this.loadBillingSummary();
    this.setupFilter();
  }

  loadBillingSummary() {
    this.isLoading.set(true);
    this.accountOfficerService.getConsumerBillingSummary().subscribe({
      next: (summaries) => {
        this.dataSource.data = summaries;
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Error loading billing summary', err);
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

    this.dataSource.filterPredicate = (data: ConsumerBillingSummaryDto, filter: string) => {
      const searchStr = filter.toLowerCase();
      return data.consumerName.toLowerCase().includes(searchStr) || 
             data.consumerId.toLowerCase().includes(searchStr) ||
             data.totalBilled.toString().includes(searchStr) ||
             data.totalPaid.toString().includes(searchStr) ||
             data.outstandingBalance.toString().includes(searchStr) ||
             data.overdueCount.toString().includes(searchStr);
    };
  }
}
