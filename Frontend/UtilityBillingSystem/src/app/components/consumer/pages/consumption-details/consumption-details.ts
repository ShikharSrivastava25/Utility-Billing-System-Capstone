import { Component, signal, computed, effect, ElementRef, AfterViewInit, OnDestroy, inject, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatSortModule, MatSort } from '@angular/material/sort';
import { MatPaginatorModule, MatPaginator } from '@angular/material/paginator';
import { ReactiveFormsModule, FormControl } from '@angular/forms';
import * as d3 from 'd3';
import { ConsumptionService } from '../../../../services/report/consumptionService';
import { ConsumptionTableRow, ConsumptionTrendPoint } from '../../../../models/report';
import { HttpClient } from '@angular/common/http';
import { API_BASE_URL } from '../../../../config/api.config';
import { UtilityTypeDto } from '../../../../models/consumer/utility-type-dto';
import { UtilityOptionDto } from '../../../../models/utility';
import { ConsumptionTrendPoint as ChartTrendPoint } from '../../../../models/charts/d3-types';

@Component({
  selector: 'app-consumption-details',
  standalone: true,
  imports: [
    CommonModule, 
    MatCardModule, 
    MatSelectModule, 
    MatFormFieldModule, 
    MatTableModule,
    MatSortModule,
    MatPaginatorModule,
    ReactiveFormsModule
  ],
  templateUrl: './consumption-details.html',
  styleUrls: ['./consumption-details.css']
})
export class ConsumptionDetailsComponent implements AfterViewInit, OnDestroy {
  chartContainer = viewChild<ElementRef>('chartContainer');
  sort = viewChild(MatSort);
  paginator = viewChild(MatPaginator);

  private consumptionService = inject(ConsumptionService);
  private http = inject(HttpClient);

  utilityControl = new FormControl('');
  utilities = signal<UtilityOptionDto[]>([]);
  selectedUtilityId = signal<string | null>(null);

  consumptionTrend = signal<ConsumptionTrendPoint[]>([]);

  tableData = signal<ConsumptionTableRow[]>([]);
  dataSource = new MatTableDataSource<ConsumptionTableRow>();

  displayedColumns: string[] = ['month', 'units', 'cost'];

  selectedUtilityName = computed(() => {
    const utilityId = this.selectedUtilityId();
    if (!utilityId) return '';
    const utility = this.utilities().find(u => u.id === utilityId);
    return utility?.name || '';
  });

  constructor() {
    this.http.get<UtilityTypeDto[]>(`${API_BASE_URL}/utilitytype/my-utilities`).subscribe({
      next: (res) => {
        const utilities: UtilityOptionDto[] = res.map(u => ({ id: u.id, name: u.name }));
        this.utilities.set(utilities);
      },
      error: () => this.utilities.set([]),
    });

    this.consumptionService.getCombinedConsumptionTrend().subscribe({
      next: (res) => this.consumptionTrend.set(res),
      error: () => this.consumptionTrend.set([]),
    });

    this.utilityControl.valueChanges.subscribe((utilityTypeId) => {
      this.selectedUtilityId.set(utilityTypeId || null);
      
      if (!utilityTypeId) {
        this.tableData.set([]);
        this.consumptionService.getCombinedConsumptionTrend().subscribe({
          next: (res) => this.consumptionTrend.set(res),
          error: () => this.consumptionTrend.set([]),
        });
      } else {
        this.consumptionService.getUtilityConsumptionTable(utilityTypeId).subscribe({
          next: (rows) => this.tableData.set(rows),
          error: () => this.tableData.set([]),
        });
      }
    });

    effect(() => {
      const trend = this.consumptionTrend();
      const utilitySelected = !!this.selectedUtilityId();

      if (!utilitySelected && this.chartContainer()) {
        setTimeout(() => this.createChart(), 0);
      }
    });

    effect(() => {
      this.dataSource.data = this.tableData();
      if (this.sort()) {
        this.dataSource.sort = this.sort()!;
      }
      if (this.paginator()) {
        this.dataSource.paginator = this.paginator()!;
      }
    });
  }

  ngAfterViewInit() {
    if (this.sort()) {
      this.dataSource.sort = this.sort()!;
    }
    if (this.paginator()) {
      this.dataSource.paginator = this.paginator()!;
    }
    if (!this.selectedUtilityId()) {
      this.createChart();
    }
  }

  ngOnDestroy() {
    const container = this.chartContainer();
    if (container) {
      d3.select(container.nativeElement).selectAll('*').remove();
    }
  }

  private createChart() {
    const container = this.chartContainer();
    if (!container) return;

    const element = container.nativeElement;
    d3.select(element).selectAll('*').remove();

    const trend = this.consumptionTrend() || [];
    const margin = { top: 20, right: 30, bottom: 40, left: 50 };
    const width = element.clientWidth - margin.left - margin.right;
    const height = 400 - margin.top - margin.bottom;

    if (width <= 0) return;

    const svg = d3.select(element)
      .append('svg')
      .attr('width', width + margin.left + margin.right)
      .attr('height', height + margin.top + margin.bottom)
      .append('g')
      .attr('transform', `translate(${margin.left},${margin.top})`);

    const data = trend.map(d => ({
      month: d.month,
      value: d.totalConsumption,
    }));

    const x = d3.scalePoint()
      .domain(data.map(d => d.month))
      .range([0, width]);

    const y = d3.scaleLinear()
      .domain([0, d3.max(data, d => d.value) || 0])
      .nice()
      .range([height, 0]);

    svg.append('g')
      .attr('class', 'grid')
      .attr('transform', `translate(0,${height})`)
      .call(d3.axisBottom(x).tickSize(-height).tickFormat(() => ''));

    svg.append('g')
      .attr('class', 'grid')
      .call(d3.axisLeft(y).tickSize(-width).tickFormat(() => ''));

    // X Axis
    svg.append('g')
      .attr('transform', `translate(0,${height})`)
      .call(d3.axisBottom(x));

    // Y Axis
    svg.append('g')
      .call(d3.axisLeft(y));

    // Line for combined consumption
    const line = d3.line<ChartTrendPoint>()
      .x((d: ChartTrendPoint) => x(d.month)!)
      .y((d: ChartTrendPoint) => y(d.value));

    svg.append('path')
      .datum(data)
      .attr('class', 'line')
      .attr('d', line)
      .style('stroke', '#4caf50');

    // Add dots
    svg.selectAll('.dot')
      .data(data)
      .enter().append('circle')
      .attr('cx', (d: ChartTrendPoint) => x(d.month)!)
      .attr('cy', (d: ChartTrendPoint) => y(d.value))
      .attr('r', 5)
      .style('fill', '#4caf50');
  }
}
