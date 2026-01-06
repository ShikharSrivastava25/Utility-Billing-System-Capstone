import { CommonModule } from '@angular/common';
import { Component, computed, input } from '@angular/core';
import { MatCardModule } from '@angular/material/card';

@Component({
  selector: 'app-summary-card',
  imports: [CommonModule, MatCardModule],
  templateUrl: './summary-card.html',
  styleUrl: './summary-card.css',
})
export class SummaryCard {
  title = input.required<string>();
  value = input.required<number>();
  unit = input<string>();
  icon = input.required<'currency' | 'people' | 'bolt' | 'pending'>();
  theme = input<'purple' | 'orange' | 'cyan' | 'green' | 'pink' | 'blue' | 'yellow'>();

  cardClasses = computed(() => {
    return `summary-card ${this.theme() ?? ''}`;
  });
}
