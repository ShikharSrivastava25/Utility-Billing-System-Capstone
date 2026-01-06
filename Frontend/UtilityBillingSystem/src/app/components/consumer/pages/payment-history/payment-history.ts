import { Component, signal, computed, inject, ViewChild, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatChipsModule } from '@angular/material/chips';
import { MatButtonModule } from '@angular/material/button';
import { ReactiveFormsModule, FormBuilder, FormGroup } from '@angular/forms';
import { PaymentService } from '../../../../services/core/paymentService';
import { PaymentHistoryItem } from '../../../../models/payment';

type PaymentDisplayItem = Omit<PaymentHistoryItem, 'paymentDate'> & { 
  billMonth: string; 
  utilityType: string; 
  paymentDate: Date; 
  method: string; 
};

@Component({
  selector: 'app-payment-history',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatSortModule,
    MatPaginatorModule,
    MatIconModule,
    MatCardModule,
    MatFormFieldModule,
    MatSelectModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatChipsModule,
    MatButtonModule,
    ReactiveFormsModule
  ],
  templateUrl: './payment-history.html',
  styleUrl: './payment-history.css',
})
export class PaymentHistory {
  private fb = inject(FormBuilder);
  private paymentService = inject(PaymentService);
  
  @ViewChild(MatSort) sort!: MatSort;
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  
  displayedColumns: string[] = ['date', 'billMonth', 'utility', 'amount', 'method', 'status'];
  
  dataSource = new MatTableDataSource<PaymentDisplayItem>([]);
  
  filterForm: FormGroup = this.fb.group({
    utilityType: [''],
    startDate: [null],
    endDate: [null]
  });

  allPayments = signal<PaymentHistoryItem[]>([]);
  
  payments = signal<PaymentHistoryItem[]>([]);

  availableUtilities = computed(() => {
    const names = this.allPayments().map(p => p.utilityName);
    return Array.from(new Set(names)).sort();
  });

  filteredPayments = computed((): PaymentDisplayItem[] => {
    return this.payments().map(p => {
      const { paymentDate: _, ...rest } = p;
      return {
        ...rest,
        billMonth: p.billingPeriod,
        utilityType: p.utilityName,
        paymentDate: new Date(p.paymentDate),
        method: p.paymentMethod,
      };
    });
  });

  constructor() {
    this.loadPayments();

    this.filterForm.valueChanges.subscribe(() => {
      this.loadPayments();
    });

    effect(() => {
      this.dataSource.data = this.filteredPayments();
      if (this.sort) {
        this.dataSource.sort = this.sort;
      }
      if (this.paginator) {
        this.dataSource.paginator = this.paginator;
      }
    });
  }

  ngAfterViewInit() {
    this.dataSource.sort = this.sort;
    this.dataSource.paginator = this.paginator;
    
    this.dataSource.sortingDataAccessor = (item: PaymentDisplayItem, property: string) => {
      let value: unknown;
      if (property === 'utility') {
        value = item.utilityType;
      } else if (property === 'date') {
        value = item.paymentDate;
      } else {
        value = item[property as keyof PaymentDisplayItem];
      }
      
      if (typeof value === 'string') {
        return value.toLowerCase();
      }
      if (value instanceof Date) {
        return value.getTime();
      }
      return (value as string | number) ?? 0;
    };
  }

  resetFilters() {
    this.filterForm.reset({
      utilityType: '',
      startDate: null,
      endDate: null
    });
  }

  private loadPayments() {
    const filters = this.filterForm.value;
    this.paymentService.getPaymentHistory({
      startDate: filters.startDate,
      endDate: filters.endDate,
      utilityTypeId: null,
    }).subscribe({
      next: (items) => {
        this.allPayments.set(items);
        
        const filtered = filters.utilityType 
          ? items.filter(p => p.utilityName === filters.utilityType)
          : items;
        this.payments.set(filtered);
      },
      error: () => {
        this.allPayments.set([]);
        this.payments.set([]);
      },
    });
  }
}
