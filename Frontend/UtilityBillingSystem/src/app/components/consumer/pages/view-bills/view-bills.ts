import { Component, signal, inject, TemplateRef, viewChild, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatSortModule, MatSort } from '@angular/material/sort';
import { MatPaginatorModule, MatPaginator } from '@angular/material/paginator';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators, FormControl } from '@angular/forms';
import { debounceTime, startWith } from 'rxjs';
import { NotificationService } from '../../../../services/notification/notificationService';
import { BillService } from '../../../../services/billing/billService';
import { BillDisplayItem } from '../../../../models/billing/bill-display';

@Component({
  selector: 'app-view-bills',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatSortModule,
    MatPaginatorModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    ReactiveFormsModule
  ],
  templateUrl: './view-bills.html',
  styleUrl: './view-bills.css',
})
export class ViewBills {
  private dialog = inject(MatDialog);
  private fb = inject(FormBuilder);
  private notificationService = inject(NotificationService);
  private billService = inject(BillService);

  paymentDialog = viewChild<TemplateRef<unknown>>('paymentDialog');
  sort = viewChild(MatSort);
  paginator = viewChild(MatPaginator);
  
  displayedColumns: string[] = ['month', 'utility', 'units', 'amount', 'status', 'dueDate', 'actions'];
  
  dataSource = new MatTableDataSource<BillDisplayItem>();
  selectedBill = signal<BillDisplayItem | null>(null);
  paymentForm: FormGroup;
  searchControl = new FormControl('');

  bills = signal<BillDisplayItem[]>([]);

  constructor() {
    this.paymentForm = this.fb.group({
      paymentMethod: ['', Validators.required],
      receiptNumber: [''],
      upiId: ['']
    });

    this.loadBills();
    this.setupFilter();

    effect(() => {
      this.dataSource.data = this.bills();
      if (this.sort()) {
        this.dataSource.sort = this.sort()!;
      }
      if (this.paginator()) {
        this.dataSource.paginator = this.paginator()!;
      }
    });

    this.paymentForm.get('paymentMethod')?.valueChanges.subscribe(method => {
      const receiptControl = this.paymentForm.get('receiptNumber');
      const upiControl = this.paymentForm.get('upiId');

      if (method === 'Cash') {
        receiptControl?.setValidators([Validators.required]);
        upiControl?.clearValidators();
      } else if (method === 'Online') {
        upiControl?.setValidators([Validators.required]);
        receiptControl?.clearValidators();
      }
      receiptControl?.updateValueAndValidity();
      upiControl?.updateValueAndValidity();
    });
  }

  ngAfterViewInit() {
    this.dataSource.sort = this.sort()!;
    this.dataSource.paginator = this.paginator()!;
    
    this.dataSource.sortingDataAccessor = (item, property) => {
      let value: unknown;
      if (property === 'utility') {
        value = item.utilityType;
      } else {
        value = item[property as keyof BillDisplayItem];
      }
      if (typeof value === 'string') {
        return value.toLowerCase();
      }
      if (value instanceof Date) {
        return value.getTime();
      }
      return value as string | number;
    };
  }

  setupFilter() {
    this.dataSource.filterPredicate = (data: BillDisplayItem, filter: string) => {
      const searchStr = filter.toLowerCase();
      return data.month.toLowerCase().includes(searchStr) ||
             data.utilityType.toLowerCase().includes(searchStr) ||
             data.status.toLowerCase().includes(searchStr) ||
             data.units.toString().includes(searchStr) ||
             data.amount.toString().includes(searchStr) ||
             new Date(data.dueDate).toDateString().toLowerCase().includes(searchStr) ||
             new Date(data.dueDate).toLocaleDateString().toLowerCase().includes(searchStr);
    };

    this.searchControl.valueChanges.pipe(
      debounceTime(300),
      startWith('')
    ).subscribe(value => {
      this.dataSource.filter = value?.trim().toLowerCase() || '';
    });
  }

  openPaymentDialog(bill: BillDisplayItem) {
    this.selectedBill.set(bill);
    this.paymentForm.reset();
    this.dialog.open(this.paymentDialog()!);
  }

  processPayment() {
    const selectedBill = this.selectedBill();
    if (this.paymentForm.valid && selectedBill) {
      const billId = selectedBill.id;

      const method = this.paymentForm.get('paymentMethod')?.value;
      const receiptNumber = this.paymentForm.get('receiptNumber')?.value || undefined;
      const upiId = this.paymentForm.get('upiId')?.value || undefined;

      this.billService
        .payBill(billId, { paymentMethod: method, receiptNumber, upiId })
        .subscribe({
          next: () => {
            this.notificationService.show('Payment successful! Bill marked as Paid.');
            this.dialog.closeAll();
            this.loadBills();
          },
          error: (err) => {
            const message = err?.error?.error?.message || 'Failed to process payment';
            this.notificationService.show(message, 'error');
          },
        });
    }
  }

  private loadBills() {
    this.billService.getConsumerBills().subscribe({
      next: (bills) => {
        this.bills.set(
          bills.map(b => ({
            id: b.id,
            month: b.billingPeriod,
            utilityType: b.utilityName,
            units: b.consumption,
            amount: b.totalAmount,
            dueDate: new Date(b.dueDate),
            status: b.status as 'Due' | 'Paid' | 'Overdue',
          }))
        );
      },
      error: () => {
        this.bills.set([]);
      },
    });
  }
}
