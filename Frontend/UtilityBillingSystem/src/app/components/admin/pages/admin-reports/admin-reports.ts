import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal, ViewChild } from '@angular/core';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatSortModule, MatSort } from '@angular/material/sort';
import { MatPaginatorModule, MatPaginator } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { SummaryCard } from '../../widgets/summary-card/summary-card';
import { ConnectionsChart } from '../../widgets/connections-chart/connections-chart';
import { ReportService } from '../../../../services/report/reportService';
import { BehaviorSubject, Observable, switchMap } from 'rxjs';
import { ConnectionsByUtility, ReportSummary } from '../../../../models/report';

@Component({
  selector: 'app-admin-reports',
  imports: [
    CommonModule, 
    MatTableModule,
    MatSortModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    SummaryCard,
    ConnectionsChart
  ],
  templateUrl: './admin-reports.html',
  styleUrl: './admin-reports.css',
})
export class AdminReports implements OnInit {
  private reportService = inject(ReportService);

  private refreshTrigger = new BehaviorSubject<void>(undefined);

  summary$: Observable<ReportSummary> = this.refreshTrigger.pipe(
    switchMap(() => this.reportService.getReportSummary())
  );

  connectionsByUtility$: Observable<ConnectionsByUtility[]> = this.refreshTrigger.pipe(
    switchMap(() => this.reportService.getConnectionsByUtility())
  );

  ngOnInit() {
    this.refreshTrigger.next();
  }
}
