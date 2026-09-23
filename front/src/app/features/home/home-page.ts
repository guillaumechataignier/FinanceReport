import { ChangeDetectionStrategy, Component, computed, effect, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AreaChartModule, Color, ScaleType } from '@swimlane/ngx-charts';

import { AuthService } from '../../core/auth/auth.service';
import { DashboardApi, History, HistoryPeriod, Summary } from '../../core/api/dashboard.api';
import { DonutChart, DonutShare } from '../../shared/charts/donut-chart';
import { SERIES_COLORS } from '../../shared/charts/palette';
import { Icon } from '../../shared/icon/icon';
import { Notify } from '../../shared/notify/notify';
import { PageHeader } from '../../shared/page-header/page-header';
import { formatAmount, formatDate, gainClass } from '../../shared/pipes/format';
import { AmountFrPipe, DateFrPipe, PercentFrPipe } from '../../shared/pipes/format.pipes';

const PERIODS: { value: HistoryPeriod; label: string }[] = [
  { value: '1M', label: '1M' },
  { value: '1A', label: '1A' },
  { value: 'ALL', label: 'Tout' },
];

/** Page d'accueil (UC-11, UC-12) : indicateurs, courbe d'évolution, répartition par type de compte. */
@Component({
  selector: 'app-home-page',
  imports: [RouterLink, AreaChartModule, PageHeader, DonutChart, Icon, AmountFrPipe, DateFrPipe, PercentFrPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './home-page.html',
  styleUrl: './home-page.scss',
})
export class HomePage implements OnInit {
  private readonly api = inject(DashboardApi);
  private readonly notify = inject(Notify);
  private readonly auth = inject(AuthService);

  protected readonly periods = PERIODS;
  protected readonly gainClass = gainClass;
  protected readonly summary = signal<Summary | null>(null);
  protected readonly period = signal<HistoryPeriod>('1A');
  protected readonly history = signal<History | null>(null);
  protected readonly chartColors: Color = { name: 'patrimoine', selectable: false, group: ScaleType.Ordinal, domain: [SERIES_COLORS[0]] };

  protected readonly greeting = computed(() => {
    const name = this.auth.username() ?? '';
    return `Bonjour ${name.charAt(0).toUpperCase()}${name.slice(1)}`;
  });

  protected readonly hasData = computed(() => (this.summary()?.byAccountType.length ?? 0) > 0);

  protected readonly typeShares = computed<DonutShare[]>(() =>
    (this.summary()?.byAccountType ?? []).map((t) => ({ key: t.type, label: t.type, amount: t.amount, percent: t.percent })),
  );

  /** Moins de 2 points : « Historique insuffisant pour tracer une courbe » (UC-12). */
  protected readonly series = computed(() => {
    const points = this.history()?.points ?? [];
    return points.length < 2
      ? null
      : [{ name: 'Patrimoine total', series: points.map((p) => ({ name: new Date(`${p.date}T12:00:00`), value: p.totalNetWorth })) }];
  });

  protected readonly formatAxisAmount = (value: number) => formatAmount(value).replace(/,00/, '');
  protected readonly formatAxisDate = (value: Date) =>
    value.toLocaleDateString('fr-FR', this.period() === '1M' ? { day: '2-digit', month: '2-digit' } : { month: 'short', year: 'numeric' });
  protected readonly formatTooltipDate = (value: Date) => formatDate(value.toISOString().slice(0, 10));

  constructor() {
    effect(() => {
      this.api.history(this.period()).subscribe({
        next: (history) => this.history.set(history),
        error: (error: unknown) => this.notify.error(error),
      });
    });
  }

  ngOnInit(): void {
    this.api.summary().subscribe({
      next: (summary) => this.summary.set(summary),
      error: (error: unknown) => this.notify.error(error),
    });
  }
}
