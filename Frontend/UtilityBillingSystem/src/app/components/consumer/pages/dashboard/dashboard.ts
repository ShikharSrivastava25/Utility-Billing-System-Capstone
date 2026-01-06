import { Component, signal, ElementRef, viewChild, AfterViewInit, effect, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import * as d3 from 'd3';
import { DashboardService } from '../../../../services/report/dashboardService';

@Component({
  selector: 'app-consumer-dashboard',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatIconModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class ConsumerDashboard implements AfterViewInit {
  private dashboardService = inject(DashboardService);

  totalOutstanding = signal(0);
  monthlySpending = signal(0);
  activeConnections = signal(0); 
  dueBills = signal(0);

  trendData = signal<{ month: string; value: number }[]>([]);

  private chartContainer = viewChild<ElementRef>('chartContainer');

  constructor() {
    // Load dashboard data
    this.dashboardService.getDashboard().subscribe({
      next: (res) => {
        this.totalOutstanding.set(res.outstandingBalance);
        this.monthlySpending.set(res.monthlySpending);
        this.activeConnections.set(res.activeConnections);
        this.dueBills.set(res.dueBillsCount);
        this.trendData.set(
          res.consumptionTrend.map(point => ({
            month: point.month,
            value: point.totalConsumption,
          }))
        );
      },
      error: () => {
      },
    });

    effect(() => {
      if (this.trendData() && this.chartContainer()) {
        this.createChart();
      }
    });
  }

  ngAfterViewInit() {
    this.createChart();
  }

  private createChart() {
    const element = this.chartContainer()?.nativeElement;
    if (!element) return;

    d3.select(element).selectAll('*').remove();

    const margin = { top: 20, right: 30, bottom: 40, left: 50 };
    const width = element.clientWidth - margin.left - margin.right;
    const height = element.clientHeight - margin.top - margin.bottom;

    const svg = d3.select(element)
      .append('svg')
      .attr('width', width + margin.left + margin.right)
      .attr('height', height + margin.top + margin.bottom)
      .append('g')
      .attr('transform', `translate(${margin.left},${margin.top})`);

    const x = d3.scalePoint()
      .domain(this.trendData().map(d => d.month))
      .range([0, width]);

    const y = d3.scaleLinear()
      .domain([0, d3.max(this.trendData(), d => d.value) || 0])
      .nice()
      .range([height, 0]);

    // Add grid lines
    svg.append('g')
      .attr('class', 'grid')
      .call(d3.axisLeft(y)
        .tickSize(-width)
        .tickFormat(() => '')
      );

    // Add X axis
    svg.append('g')
      .attr('transform', `translate(0,${height})`)
      .call(d3.axisBottom(x));

    // Add Y axis
    svg.append('g')
      .call(d3.axisLeft(y));

    // Add the line
    const line = d3.line<{ month: string, value: number }>()
      .x(d => x(d.month)!)
      .y(d => y(d.value));

    svg.append('path')
      .datum(this.trendData())
      .attr('class', 'line')
      .attr('d', line);

    // Add dots
    svg.selectAll('.dot')
      .data(this.trendData())
      .enter()
      .append('circle')
      .attr('class', 'dot')
      .attr('cx', d => x(d.month)!)
      .attr('cy', d => y(d.value))
      .attr('r', 4)
      .attr('fill', '#4caf50');
  }
}
