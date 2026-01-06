import { CommonModule } from '@angular/common';
import { Component, inject, signal, OnInit, computed, ViewChild } from '@angular/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatSortModule, MatSort } from '@angular/material/sort';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { SelectionModel } from '@angular/cdk/collections';
import { BillGenerationService } from '../../../../services/billing/billGenerationService';
import { BillingCycleService } from '../../../../services/billing/billingCycleService';
import { NotificationService } from '../../../../services/notification/notificationService';
import { PendingBill, BillGenerationRequest } from '../../../../models/billing/bill-generation';
import { ConfirmationDialogComponent } from '../../../shared/confirmation-dialog/confirmation-dialog';
import { BillingCycle } from '../../../../models/billing/billing-cycle';

@Component({
  selector: 'app-bill-generation',
  standalone: true,
  imports: [
    CommonModule,
    MatProgressSpinnerModule,
    MatTableModule,
    MatSortModule,
    MatButtonModule,
    MatIconModule,
    MatCheckboxModule,
    MatTooltipModule,
    MatDialogModule
  ],
  templateUrl: './bill-generation.html',
  styleUrl: './bill-generation.css',
})
export class BillGeneration implements OnInit {
  private billGenService = inject(BillGenerationService);
  private cycleService = inject(BillingCycleService);
  private notificationService = inject(NotificationService);
  private dialog = inject(MatDialog);

  isLoading = signal(false);
  isGenerating = signal(false);
  activeCycles = signal<BillingCycle[]>([]);
  
  dataSource = new MatTableDataSource<PendingBill>([]);
  selection = new SelectionModel<PendingBill>(true, []);
  displayedColumns: string[] = ['select', 'consumerName', 'utilityName', 'meterNumber', 'units', 'expectedAmount', 'status'];

  @ViewChild(MatSort) set sort(sort: MatSort) {
    this.dataSource.sort = sort;
  }

  totalExpectedAmount = computed(() => {
    return this.selection.selected.reduce((sum, bill) => sum + bill.expectedAmount, 0);
  });

  constructor() {
    this.dataSource.sortingDataAccessor = (item: PendingBill, property: string) => {
      const value = item[property as keyof PendingBill];
      return typeof value === 'string' ? value.toLowerCase() : (value as string | number);
    };
  }

  ngOnInit() {
    this.loadData();
  }

  loadData() {
    this.isLoading.set(true);
    this.cycleService.getBillingCycles().subscribe(cycles => {
      this.activeCycles.set(cycles.filter(c => c.isActive));
    });

    this.billGenService.getPendingBillsForDisplay().subscribe({
      next: (pendingBills) => {
        console.log('Pending bills received from service:', pendingBills);
        console.log('Number of pending bills:', pendingBills.length);
        
        this.dataSource.data = pendingBills;
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Error loading pending bills:', err);
        console.error('Error details:', err.error);
        const errorMessage = err?.error?.error?.message || 'Failed to load pending bills. Please check that connections have active tariffs assigned.';
        this.notificationService.show(errorMessage, 'error');
        this.isLoading.set(false);
      }
    });
  }

  isAllSelected() {
    const numSelected = this.selection.selected.length;
    const numRows = this.dataSource.data.length;
    return numSelected === numRows;
  }

  toggleAllRows() {
    if (this.isAllSelected()) {
      this.selection.clear();
      return;
    }
    this.selection.select(...this.dataSource.data);
  }

  generateSelected() {
    if (this.selection.isEmpty()) return;

    const count = this.selection.selected.length;
    const amount = this.totalExpectedAmount();

    this.dialog.open(ConfirmationDialogComponent, {
      data: {
        title: 'Confirm Bill Generation',
        message: `Are you sure you want to generate ${count} ${count === 1 ? 'bill' : 'bills'} totaling ₹${amount.toFixed(2)}?`,
        confirmButtonText: 'Generate',
        cancelButtonText: 'Cancel'
      }
    }).afterClosed().subscribe(result => {
      if (result) {
        this.isGenerating.set(true);
        const readingIds = this.selection.selected.map(b => b.id!).filter(id => id !== undefined);
        
        const billGenerationRequest: BillGenerationRequest = { readingIds };
        this.billGenService.generateBills(billGenerationRequest).subscribe({
          next: () => {
            this.isGenerating.set(false);
            this.selection.clear();
            this.loadData();
          },
          error: () => this.isGenerating.set(false)
        });
      }
    });
  }

  generateAll() {
    if (this.dataSource.data.length === 0) return;
    this.selection.select(...this.dataSource.data);
    this.generateSelected();
  }

  formatOrdinal(day: number | null | undefined): string {
    const n = Math.abs(Math.trunc(Number(day)));
    if (!Number.isFinite(n)) return '';

    const mod100 = n % 100;
    if (mod100 >= 11 && mod100 <= 13) return `${n}th`;

    switch (n % 10) {
      case 1: return `${n}st`;
      case 2: return `${n}nd`;
      case 3: return `${n}rd`;
      default: return `${n}th`;
    }
  }
}
