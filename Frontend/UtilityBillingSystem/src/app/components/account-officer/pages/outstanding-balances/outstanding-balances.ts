import { Component, OnInit, ViewChild, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatSortModule, MatSort } from '@angular/material/sort';
import { MatPaginatorModule, MatPaginator } from '@angular/material/paginator';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { debounceTime, startWith } from 'rxjs';
import { AccountOfficerService } from '../../../../services/account-officer/accountOfficerService';
import { OutstandingBillDto } from '../../../../models/account-officer/outstanding-bill.dto';
import { BillDetailDialogComponent } from '../../bill-detail-dialog/bill-detail-dialog';

@Component({
  selector: 'app-outstanding-balances',
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
    MatButtonToggleModule,
    MatDialogModule,
    MatButtonModule,
    ReactiveFormsModule
  ],
  templateUrl: './outstanding-balances.html',
  styleUrl: './outstanding-balances.css'
})
export class OutstandingBalances implements OnInit {
  private accountOfficerService = inject(AccountOfficerService);
  private dialog = inject(MatDialog);

  isLoading = signal(true);
  dataSource = new MatTableDataSource<OutstandingBillDto>([]);
  displayedColumns: string[] = ['consumerId', 'consumerName', 'utilityName', 'billMonth', 'amount', 'status', 'dueDate', 'actions'];
  
  searchControl = new FormControl('');
  statusFilter = new FormControl('all');

  @ViewChild(MatSort) set sort(sort: MatSort) {
    this.dataSource.sort = sort;
    if (sort) {
      // Set up custom sorting for billMonth and other columns
      this.dataSource.sortingDataAccessor = (item, property) => {
        switch (property) {
          case 'billMonth':
            try {
              const date = new Date(item.billMonth + ' 1');
              return isNaN(date.getTime()) ? 0 : date.getTime();
            } catch {
              return 0;
            }
          case 'dueDate':
            return new Date(item.dueDate).getTime();
          case 'consumerId':
            return item.consumerId.toLowerCase();
          case 'consumerName':
            return item.consumerName.toLowerCase();
          case 'utilityName':
            return item.utilityName.toLowerCase();
          case 'amount':
            return item.amount;
          case 'status':
            return item.status.toLowerCase();
          default: {
            const value = item[property as keyof OutstandingBillDto];
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
    this.loadOutstandingBills();
    this.setupFilters();
  }

  loadOutstandingBills() {
    this.isLoading.set(true);
    this.accountOfficerService.getOutstandingBills().subscribe({
      next: (bills) => {
        this.dataSource.data = bills;
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Error loading outstanding bills', err);
        this.isLoading.set(false);
      }
    });
  }

  setupFilters() {
    // Combine search and status filter
    this.searchControl.valueChanges.pipe(
      debounceTime(300),
      startWith('')
    ).subscribe(() => this.applyFilters());

    this.statusFilter.valueChanges.subscribe(() => this.applyFilters());

    this.dataSource.filterPredicate = (data: OutstandingBillDto, filter: string) => {
      const [searchStr, status] = filter.split('|');
      
      const matchesSearch = data.consumerName.toLowerCase().includes(searchStr) ||
                           data.consumerId.toLowerCase().includes(searchStr) ||
                           data.utilityName.toLowerCase().includes(searchStr) ||
                           data.billMonth.toLowerCase().includes(searchStr) ||
                           data.amount.toString().includes(searchStr) ||
                           data.status.toLowerCase().includes(searchStr) ||
                           new Date(data.dueDate).toDateString().toLowerCase().includes(searchStr) ||
                           new Date(data.dueDate).toLocaleDateString().toLowerCase().includes(searchStr);
      
      const matchesStatus = status === 'all' || data.status.toLowerCase() === status.toLowerCase();
      
      return matchesSearch && matchesStatus;
    };
  }

  applyFilters() {
    const search = this.searchControl.value?.trim().toLowerCase() || '';
    const status = this.statusFilter.value || 'all';
    this.dataSource.filter = `${search}|${status}`;
  }

  viewBillDetails(bill: OutstandingBillDto) {
    this.isLoading.set(true);
    this.accountOfficerService.getBillById(bill.billId).subscribe({
      next: (billDetail) => {
        this.isLoading.set(false);
        this.dialog.open(BillDetailDialogComponent, {
          width: '600px',
          data: billDetail
        });
      },
      error: (err) => {
        console.error('Error loading bill details', err);
        this.isLoading.set(false);
      }
    });
  }
}
