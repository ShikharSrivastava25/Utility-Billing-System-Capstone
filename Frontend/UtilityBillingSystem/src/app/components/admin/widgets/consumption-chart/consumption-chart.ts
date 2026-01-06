import { Component, computed, effect, ElementRef, input, viewChild } from '@angular/core';
import { ConsumptionData } from '../../../../models/report';
import * as d3 from 'd3';
import { D3GSelection, D3ColorScale, D3PieArcDatum } from '../../../../models/charts/d3-types';

@Component({
  selector: 'app-consumption-chart',
  imports: [],
  templateUrl: './consumption-chart.html',
  styleUrl: './consumption-chart.css',
})
export class ConsumptionChart {
  data = input.required<ConsumptionData[]>();
  
  private chartContainer = viewChild<ElementRef>('chart');
  private svg: D3GSelection | null = null;
  private colors: D3ColorScale;
  private width = 250;
  private height = 250;
  private radius = Math.min(this.width, this.height) / 2;
  
  totalConsumption = computed(() => {
    if (!this.data()) return 0;
    return this.data().reduce((acc, curr) => acc + curr.consumption, 0);
  });

  constructor() {
    this.colors = d3.scaleOrdinal<string, string>()
      .range(["#ab47bc", "#00bcd4", "#fb8c00", "#66bb6a", "#ef5350"]) as D3ColorScale;

    effect(() => {
      // Re-create the chart whenever the data changes.
      if (this.data() && this.svg) {
        this.createChart();
      }
    });
  }
  
  ngAfterViewInit(): void {
    if (this.chartContainer()) {
      this.initChart();
      if (this.data()) {
        this.createChart();
      }
    }
  }
  
  private initChart(): void {
    this.svg = d3.select(this.chartContainer()!.nativeElement)
      .append('svg')
      .attr('width', this.width)
      .attr('height', this.height)
      .append('g')
      .attr('transform', `translate(${this.width / 2}, ${this.height / 2})`);
  }
  
  private createChart(): void {
    if (!this.svg) return;
    this.svg.selectAll('*').remove(); // Clear previous chart
    
    // Do not render chart if there is no data
    if (!this.data() || this.data().length === 0 || this.totalConsumption() === 0) {
      return;
    }
    
    const pie = d3.pie<ConsumptionData>().value(d => d.consumption).sort(null);
    const data_ready = pie(this.data());
    
    const arc = d3.arc<D3PieArcDatum<ConsumptionData>>()
      .innerRadius(this.radius * 0.6)
      .outerRadius(this.radius * 0.9);
      
    this.svg
      .selectAll('path')
      .data(data_ready)
      .enter()
      .append('path')
      .attr('d', (d) => arc(d))
      .attr('fill', (d: D3PieArcDatum<ConsumptionData>) => this.colors(d.data.utilityName))
      .attr('stroke', '#2c2c34')
      .style('stroke-width', '3px');

    this.svg.append('text')
      .attr('text-anchor', 'middle')
      .attr('dy', '-0.3em')
      .style('font-size', '1.8rem')
      .style('font-weight', '600')
      .style('fill', '#fff')
      .text(this.totalConsumption().toLocaleString());
      
    this.svg.append('text')
      .attr('text-anchor', 'middle')
      .attr('dy', '1.2em')
      .style('font-size', '0.9rem')
      .style('fill', '#aaa')
      .text('Total Units');
  }

  getPercentage(value: number): string {
    if (this.totalConsumption() === 0) return '0%';
    return ((value / this.totalConsumption()) * 100).toFixed(1) + '%';
  }

  getColor(utilityName: string): string {
    return this.colors(utilityName);
  }
}
