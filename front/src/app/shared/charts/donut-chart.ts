import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { PieChartModule } from '@swimlane/ngx-charts';

import { AmountFrPipe, PercentFrPipe } from '../pipes/format.pipes';
import { colorAt } from './palette';

export interface DonutShare {
  key: string;
  label: string;
  amount: number;
  percent: number;
}

/** Graphique en anneau (ngx-charts) avec sa légende : libellé, montant et pourcentage de chaque part. */
@Component({
  selector: 'app-donut-chart',
  imports: [PieChartModule, AmountFrPipe, PercentFrPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (shares().length === 0) {
      <p class="empty">{{ emptyMessage() }}</p>
    } @else {
      <div class="donut" [class.vertical]="vertical()">
        <div class="chart" [style.width.px]="size()" [style.height.px]="size()">
          <ngx-charts-pie-chart
            [view]="[size(), size()]"
            [results]="results()"
            [customColors]="colors()"
            [doughnut]="true"
            [arcWidth]="0.22"
            [animations]="false"
            [tooltipDisabled]="true"
          />
          @if (centerLabel()) {
            <div class="center">
              <span class="muted">{{ centerLabel() }}</span>
              <span class="strong num">{{ centerValue() | amountFr }}</span>
            </div>
          }
        </div>
        <ul class="legend" [attr.aria-label]="ariaLabel()">
          @for (share of shares(); track share.key; let i = $index) {
            <li>
              <span class="swatch" [style.background]="colors()[i].value"></span>
              <span class="label">{{ share.label }}</span>
              <span class="values">
                <span class="num strong">{{ share.amount | amountFr }}</span>
                <span class="num muted">{{ share.percent | percentFr }}</span>
              </span>
            </li>
          }
        </ul>
      </div>
    }
  `,
  styles: `
    .donut { display: flex; align-items: center; gap: 24px; }
    .donut.vertical { flex-direction: column; }
    .chart { position: relative; flex-shrink: 0; }
    .center {
      position: absolute; inset: 0; display: flex; flex-direction: column; align-items: center; justify-content: center;
      gap: 2px; font-size: 14px; pointer-events: none;
    }
    .center .strong { font-size: 19px; }
    .legend { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 10px; flex-grow: 1; min-width: 0; width: 100%; }
    li { display: grid; grid-template-columns: 10px minmax(0, 1fr) auto; align-items: center; gap: 10px; font-size: 14px; }
    .swatch { width: 10px; height: 10px; border-radius: 3px; }
    .label { overflow-wrap: anywhere; }
    .values { display: flex; flex-direction: column; align-items: flex-end; line-height: 1.3; }
    .values .muted { font-size: 12.5px; }
    .empty { margin: 0; padding: 24px 0; text-align: center; color: var(--fr-muted); font-size: 14px; }
  `,
})
export class DonutChart {
  readonly shares = input.required<DonutShare[]>();
  readonly size = input(116);
  readonly vertical = input(false);
  readonly centerLabel = input<string>();
  readonly centerValue = input<number | null>(null);
  readonly ariaLabel = input('Répartition');
  readonly emptyMessage = input('Aucune donnée pour ces filtres.');

  protected readonly results = computed(() => this.shares().map((s) => ({ name: s.label, value: Math.max(0, s.amount) })));
  protected readonly colors = computed(() => this.shares().map((s, i) => ({ name: s.label, value: colorAt(i, s.key) })));
}
