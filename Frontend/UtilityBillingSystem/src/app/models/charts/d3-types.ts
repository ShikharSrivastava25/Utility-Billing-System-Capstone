import * as d3 from 'd3';

export type D3Selection = d3.Selection<SVGSVGElement, unknown, null, undefined>;

export type D3GSelection = d3.Selection<SVGGElement, unknown, null, undefined>;

export type D3ColorScale = d3.ScaleOrdinal<string, string, never>;

export interface D3PieArcDatum<T> {
  data: T;
  value: number;
  index: number;
  startAngle: number;
  endAngle: number;
  padAngle: number;
}

export interface ConsumptionTrendPoint {
  month: string;
  value: number;
}

