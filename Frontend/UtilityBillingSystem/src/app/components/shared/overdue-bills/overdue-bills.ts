import { Component, input } from '@angular/core';
import { OverdueBill } from '../../../models/report';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatSortModule } from '@angular/material/sort';
import { MatPaginatorModule } from '@angular/material/paginator';

@Component({
  selector: 'app-overdue-bills',
  imports: [CommonModule, MatTableModule, MatSortModule, MatPaginatorModule],
  templateUrl: './overdue-bills.html',
  styleUrl: './overdue-bills.css',
})
export class OverdueBills {
  bills = input.required<OverdueBill[]>();
  displayedColumns: string[] = ['consumerName', 'utilityName', 'amount', 'dueDate'];
}

