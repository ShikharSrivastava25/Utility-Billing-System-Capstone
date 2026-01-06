import { Component, signal, inject, OnInit, ViewChild, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatButtonModule } from '@angular/material/button';
import { MatRadioModule } from '@angular/material/radio';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ReactiveFormsModule, FormBuilder, FormGroup } from '@angular/forms';
import { SummaryCard } from '../../../admin/widgets/summary-card/summary-card';
import { ConsumptionChart } from '../../../admin/widgets/consumption-chart/consumption-chart';
import { MonthlyRevenueTable } from '../../../shared/monthly-revenue-table/monthly-revenue-table';
import { AccountOfficerService } from '../../../../services/account-officer/accountOfficerService';
import { MonthlyRevenueDto } from '../../../../models/account-officer/monthly-revenue.dto';
import { RecentPaymentDto } from '../../../../models/account-officer/recent-payment.dto';
import { OutstandingByUtilityDto } from '../../../../models/account-officer/outstanding-by-utility.dto';
import { AverageConsumption, ConsumptionData } from '../../../../models/report';
import { MONTHS } from '../../../../config/date.config';
import { forkJoin } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';

@Component({
  selector: 'app-account-officer-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTableModule,
    MatPaginatorModule,
    MatSortModule,
    MatFormFieldModule,
    MatSelectModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatButtonModule,
    MatRadioModule,
    MatTooltipModule,
    ReactiveFormsModule,
    SummaryCard,
    ConsumptionChart,
    MonthlyRevenueTable
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class AccountOfficerDashboard implements OnInit {
  private accountOfficerService = inject(AccountOfficerService);
  private fb = inject(FormBuilder);
  isLoading = signal(true);
  
  totalRevenue = signal(0);
  unpaidBillsCount = signal(0);
  outstandingDues = signal(0);
  totalConsumption = signal(0);

  monthlyRevenue = signal<MonthlyRevenueDto[]>([]);
  outstandingDataSource = new MatTableDataSource<OutstandingByUtilityDto>([]);
  recentPaymentsDataSource = new MatTableDataSource<RecentPaymentDto>([]);
  avgConsumptionDataSource = new MatTableDataSource<AverageConsumption>([]);
  isAvgConsumptionLoading = signal(true);
  consumptionByUtility = signal<ConsumptionData[]>([]);
  isRevenueLoading = signal(false);

  filterForm: FormGroup = this.fb.group({
    filterType: ['monthYear'],
    selectedMonth: [new Date().getMonth() + 1],
    selectedYear: [new Date().getFullYear()],
    startDate: [null],
    endDate: [null]
  });

  months = MONTHS;
  years: number[] = [];

  @ViewChild('outstandingPaginator') set outstandingPaginator(op: MatPaginator) {
    this.outstandingDataSource.paginator = op;
  }

  @ViewChild('recentPaginator') set recentPaginator(rp: MatPaginator) {
    this.recentPaymentsDataSource.paginator = rp;
  }

  @ViewChild('avgConsumptionPaginator') set avgConsumptionPaginator(ap: MatPaginator) {
    this.avgConsumptionDataSource.paginator = ap;
  }


  @ViewChild('outstandingSort') set outstandingSort(ms: MatSort) {
    this.outstandingDataSource.sort = ms;
    this.outstandingDataSource.sortingDataAccessor = (item: OutstandingByUtilityDto, property: string) => {
      switch (property) {
        case 'utilityName': return item.utilityName.toLowerCase();
        case 'outstandingAmount': return item.outstandingAmount;
        default: {
          const value = item[property as keyof OutstandingByUtilityDto];
          return typeof value === 'string' ? value.toLowerCase() : (value as string | number);
        }
      }
    };
  }

  @ViewChild('recentSort') set recentSort(ms: MatSort) {
    this.recentPaymentsDataSource.sort = ms;
    this.recentPaymentsDataSource.sortingDataAccessor = (item: RecentPaymentDto, property: string) => {
      switch (property) {
        case 'consumerName': return item.consumerName.toLowerCase();
        case 'utilityName': return item.utilityName.toLowerCase();
        case 'method': return item.method.toLowerCase();
        case 'amount': return item.amount;
        case 'date': return new Date(item.date).getTime();
        default: {
          const value = item[property as keyof RecentPaymentDto];
          return typeof value === 'string' ? value.toLowerCase() : (value as string | number);
        }
      }
    };
  }

  @ViewChild('avgConsumptionSort') set avgConsumptionSort(ms: MatSort) {
    this.avgConsumptionDataSource.sort = ms;
    this.avgConsumptionDataSource.sortingDataAccessor = (item: AverageConsumption, property: string) => {
      switch (property) {
        case 'utilityName': return item.utilityName.toLowerCase();
        case 'averageConsumption': return item.averageConsumption;
        default: {
          const value = item[property as keyof AverageConsumption];
          return typeof value === 'string' ? value.toLowerCase() : (value as string | number);
        }
      }
    };
  }

  recentPaymentsColumns = ['date', 'consumerName', 'utilityName', 'amount', 'method'];
  outstandingColumns = ['utilityName', 'outstandingAmount'];
  avgConsumptionColumns = ['utilityName', 'averageConsumption'];

  ngOnInit() {
    const currentYear = new Date().getFullYear();
    for (let i = 0; i < 6; i++) {
      this.years.push(currentYear - i);
    }

    this.loadDashboardData();
    
    this.filterForm.valueChanges.pipe(
      debounceTime(300),
      distinctUntilChanged((prev, curr) => {
        if (prev.filterType !== curr.filterType) return false;
        if (prev.filterType === 'monthYear') {
          return prev.selectedMonth === curr.selectedMonth && prev.selectedYear === curr.selectedYear;
        } else if (prev.filterType === 'dateRange') {
          const prevStart = prev.startDate?.getTime();
          const prevEnd = prev.endDate?.getTime();
          const currStart = curr.startDate?.getTime();
          const currEnd = curr.endDate?.getTime();
          return prevStart === currStart && prevEnd === currEnd;
        }
        return false;
      })
    ).subscribe(() => {
      const filters = this.filterForm.value;
      const hasValidFilters = 
        (filters.filterType === 'dateRange' && filters.startDate && filters.endDate) ||
        (filters.filterType === 'monthYear' && filters.selectedMonth && filters.selectedYear);
      
      if (hasValidFilters) {
        this.loadMonthlyRevenue();
      }
    });
  }

  loadDashboardData() {
    this.isLoading.set(true);
    
    forkJoin({
      summary: this.accountOfficerService.getDashboardSummary(),
      recent: this.accountOfficerService.getRecentPayments(),
      outstanding: this.accountOfficerService.getOutstandingByUtility(),
      avgConsumption: this.accountOfficerService.getAverageConsumption(),
      consumption: this.accountOfficerService.getConsumptionByUtility()
    }).subscribe({
      next: (data) => {
        this.totalRevenue.set(data.summary.totalRevenue);
        this.unpaidBillsCount.set(data.summary.unpaidBillsCount);
        this.outstandingDues.set(data.summary.outstandingDues);
        this.totalConsumption.set(data.summary.totalConsumption);
        this.recentPaymentsDataSource.data = data.recent;
        this.outstandingDataSource.data = data.outstanding;
        this.avgConsumptionDataSource.data = data.avgConsumption;
        this.consumptionByUtility.set(data.consumption);
        this.isAvgConsumptionLoading.set(false);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Error loading dashboard data', err);
        this.isAvgConsumptionLoading.set(false);
        this.isLoading.set(false);
      }
    });

    // Load monthly revenue separately with filters
    this.loadMonthlyRevenue();
  }

  loadMonthlyRevenue() {
    this.isRevenueLoading.set(true);
    const filters = this.filterForm.value;
    
    let params: {
      startDate?: Date;
      endDate?: Date;
      month?: number;
      year?: number;
    } | undefined = undefined;

    if (filters.filterType === 'dateRange' && filters.startDate && filters.endDate) {
      params = {
        startDate: filters.startDate,
        endDate: filters.endDate
      };
    } else if (filters.filterType === 'monthYear' && filters.selectedMonth && filters.selectedYear) {
      params = {
        month: filters.selectedMonth,
        year: filters.selectedYear
      };
    }

    if (params) {
      const serviceCall = this.accountOfficerService.getMonthlyRevenue(params);
      serviceCall.subscribe({
        next: (data) => {
          this.monthlyRevenue.set(data);
          this.isRevenueLoading.set(false);
        },
        error: (err) => {
          console.error('Error loading monthly revenue', err);
          this.monthlyRevenue.set([]);
          this.isRevenueLoading.set(false);
        }
      });
    } else {
      // No valid filters, clear the revenue data
      this.monthlyRevenue.set([]);
      this.isRevenueLoading.set(false);
    }
  }

  resetRevenueFilters() {
    this.filterForm.patchValue({
      filterType: 'monthYear',
      selectedMonth: new Date().getMonth() + 1,
      selectedYear: new Date().getFullYear(),
      startDate: null,
      endDate: null
    });
  }
}
