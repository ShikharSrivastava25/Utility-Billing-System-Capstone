import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatDividerModule } from '@angular/material/divider';
import { MatChipsModule } from '@angular/material/chips';
import { BillDetailData } from '../../../models/billing/bill-display';

@Component({
  selector: 'app-bill-detail-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatListModule,
    MatDividerModule,
    MatChipsModule
  ],
  templateUrl: './bill-detail-dialog.html',
  styleUrl: './bill-detail-dialog.css'
})
export class BillDetailDialogComponent {
  constructor(
    public dialogRef: MatDialogRef<BillDetailDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: BillDetailData
  ) {
    console.log('Bill Detail Data:', data);
    console.log('Penalty Amount:', data?.penaltyAmount, data?.PenaltyAmount);
  }

  getPenaltyAmount(): number {
    const penaltyFromBackend = this.data?.penaltyAmount ?? this.data?.PenaltyAmount;
    
    if (penaltyFromBackend !== undefined && penaltyFromBackend !== null) {
      return penaltyFromBackend;
    }
    
    // TotalAmount = BaseAmount + TaxAmount + PenaltyAmount
    if (this.data?.totalAmount && this.data?.baseAmount && this.data?.taxAmount) {
      const calculatedPenalty = this.data.totalAmount - (this.data.baseAmount + this.data.taxAmount);
      return calculatedPenalty > 0 ? calculatedPenalty : 0;
    }
    
    return 0;
  }

  close(): void {
    this.dialogRef.close();
  }
}

