import { Component, effect, ElementRef, input, viewChild, AfterViewInit } from '@angular/core';
import { ConnectionsByUtility } from '../../../../models/report';
import * as d3 from 'd3';
import { D3GSelection } from '../../../../models/charts/d3-types';

@Component({
  selector: 'app-connections-chart',
  imports: [],
  templateUrl: './connections-chart.html',
  styleUrl: './connections-chart.css',
})
export class ConnectionsChart implements AfterViewInit {
  data = input.required<ConnectionsByUtility[]>();
  
  private chartContainer = viewChild<ElementRef>('chart');
  private svg: D3GSelection | null = null;

  constructor() {
    effect(() => {
      if (this.data() && this.svg) {
        this.createChart();
      }
    });
  }
  
  ngAfterViewInit(): void {
    if (this.chartContainer()) {
      this.createChart();
    }
  }

  private createChart(): void {
    const container = this.chartContainer()?.nativeElement;
    if (!container) return;

    d3.select(container).selectAll('*').remove();
    
    if (!this.data() || this.data().length === 0) {
      return;
    }

    const margin = { top: 20, right: 30, bottom: 80, left: 60 };
    const containerWidth = container.offsetWidth || 800;
    const width = containerWidth - margin.left - margin.right;
    const height = 400 - margin.top - margin.bottom;

    this.svg = d3.select(container)
      .append('svg')
      .attr('width', width + margin.left + margin.right)
      .attr('height', height + margin.top + margin.bottom)
      .append('g')
      .attr('transform', `translate(${margin.left},${margin.top})`);

    const x = d3.scaleBand()
      .domain(this.data().map(d => d.utilityName))
      .range([0, width])
      .padding(0.15);

    const maxValue = d3.max(this.data(), d => d.connectionCount) || 0;
    const maxTick = Math.ceil(maxValue);
    
    const y = d3.scaleLinear()
      .domain([0, maxTick])
      .range([height, 0]);

    // Color scale
    const colors = d3.scaleOrdinal()
      .domain(this.data().map(d => d.utilityName))
      .range(["#ab47bc", "#00bcd4", "#fb8c00", "#66bb6a", "#ef5350", "#42a5f5", "#ff7043"]);

    // Add grid lines
    this.svg.append('g')
      .attr('class', 'grid')
      .attr('transform', `translate(0,${height})`)
      .call(d3.axisBottom(x).tickSize(-height).tickFormat(() => ''))
      .style('stroke-dasharray', '3,3')
      .style('opacity', 0.3);

    this.svg.append('g')
      .attr('class', 'grid')
      .call(d3.axisLeft(y).tickValues(d3.range(0, maxTick + 1)).tickSize(-width).tickFormat(() => ''))
      .style('stroke-dasharray', '3,3')
      .style('opacity', 0.3);

    // Add X axis
    this.svg.append('g')
      .attr('transform', `translate(0,${height})`)
      .attr('class', 'axis x-axis')
      .call(d3.axisBottom(x))
      .selectAll('text')
      .style('text-anchor', 'middle')
      .style('fill', '#ffffff')
      .style('font-size', '13px')
      .style('font-weight', '400');

    // Add Y axis with integer ticks only
    const yAxis = d3.axisLeft(y)
      .tickValues(d3.range(0, maxTick + 1))
      .tickFormat(d3.format('d'));
    
    this.svg.append('g')
      .attr('class', 'axis y-axis')
      .call(yAxis)
      .selectAll('text')
      .style('fill', '#ffffff')
      .style('font-size', '13px')
      .style('font-weight', '400');

    // Add bars
    this.svg.selectAll('.bar')
      .data(this.data())
      .enter()
      .append('rect')
      .attr('class', 'bar')
      .attr('x', (d: ConnectionsByUtility) => x(d.utilityName) || 0)
      .attr('y', (d: ConnectionsByUtility) => y(d.connectionCount))
      .attr('width', x.bandwidth())
      .attr('height', (d: ConnectionsByUtility) => height - y(d.connectionCount))
      .attr('fill', (d: ConnectionsByUtility) => colors(d.utilityName) as string)
      .attr('rx', 4)
      .attr('ry', 4);

    // Add value labels on bars
    this.svg.selectAll('.bar-label')
      .data(this.data())
      .enter()
      .append('text')
      .attr('class', 'bar-label')
      .attr('x', (d: ConnectionsByUtility) => (x(d.utilityName) || 0) + x.bandwidth() / 2)
      .attr('y', (d: ConnectionsByUtility) => y(d.connectionCount) - 5)
      .attr('text-anchor', 'middle')
      .style('fill', '#fff')
      .style('font-size', '12px')
      .style('font-weight', '500')
      .text((d: ConnectionsByUtility) => d.connectionCount);

    // Add X axis label
    this.svg.append('text')
      .attr('transform', `translate(${width / 2}, ${height + 65})`)
      .style('text-anchor', 'middle')
      .style('fill', '#ffffff')
      .style('font-size', '14px')
      .style('font-weight', '500')
      .text('Utility Type');

    // Add Y axis label
    this.svg.append('text')
      .attr('transform', 'rotate(-90)')
      .attr('y', 0 - margin.left)
      .attr('x', 0 - (height / 2))
      .attr('dy', '1em')
      .style('text-anchor', 'middle')
      .style('fill', '#ffffff')
      .style('font-size', '14px')
      .style('font-weight', '500')
      .text('Number of Connections');
  }
}

