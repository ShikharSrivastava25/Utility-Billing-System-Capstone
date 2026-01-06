import { CommonModule } from '@angular/common';
import { Component, Input, OnChanges, OnInit, ViewChild } from '@angular/core';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatSortModule, MatSort } from '@angular/material/sort';
import { MatPaginatorModule, MatPaginator } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MonthlyRevenue } from '../../../models/report';
import { MonthlyRevenueDto } from '../../../models/account-officer/monthly-revenue.dto';

@Component({
  selector: 'app-monthly-revenue-table',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatSortModule,
    MatPaginatorModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './monthly-revenue-table.html',
  styleUrl: './monthly-revenue-table.css',
})
export class MonthlyRevenueTable implements OnInit, OnChanges {
  @Input() data: (MonthlyRevenue | MonthlyRevenueDto)[] = [];
  @Input() isLoading: boolean = false;

  displayedColumns: string[] = ['month', 'revenue'];
  dataSource = new MatTableDataSource<MonthlyRevenue | MonthlyRevenueDto>([]);

  @ViewChild('revenueSort') set revenueSort(sort: MatSort) {
    if (sort) {
      this.dataSource.sort = sort;
    }
  }

  @ViewChild('revenuePaginator') set revenuePaginator(paginator: MatPaginator) {
    if (paginator) {
      this.dataSource.paginator = paginator;
    }
  }

  private monthOrder: { [key: string]: number } = {
    'January': 1, 'February': 2, 'March': 3, 'April': 4, 'May': 5, 'June': 6,
    'July': 7, 'August': 8, 'September': 9, 'October': 10, 'November': 11, 'December': 12
  };

  constructor() {
    this.dataSource.sortingDataAccessor = (item: MonthlyRevenue | MonthlyRevenueDto, property: string) => {
      if (property === 'month') {
        const monthName = item.month.split(' ')[0];
        const monthNum = this.monthOrder[monthName] || 0;

        const yearMatch = item.month.match(/\d{4}/);
        const year = yearMatch ? parseInt(yearMatch[0]) : 0;

        return year * 100 + monthNum;
      }
      if (property === 'revenue') {
        return 'revenue' in item ? item.revenue : item.totalRevenue;
      }
      const value = item[property as keyof (MonthlyRevenue | MonthlyRevenueDto)];
      return typeof value === 'string' ? value.toLowerCase() : (value as string | number);
    };
  }

  ngOnInit() {
    this.dataSource.data = this.data;
  }

  ngOnChanges() {
    if (this.data) {
      this.dataSource.data = this.data;
    } else {
      this.dataSource.data = [];
    }
  }
}

