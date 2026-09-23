import { ChangeDetectionStrategy, Component, computed, effect, inject, OnInit, signal } from '@angular/core';
import { MatTooltipModule } from '@angular/material/tooltip';
import { forkJoin } from 'rxjs';

import { AccountsApi } from '../../core/api/accounts.api';
import { DashboardAccounts, DashboardApi, DashboardPosition, DashboardPositions } from '../../core/api/dashboard.api';
import { priceDecimals, SECURITY_TYPE_LABELS, SECURITY_TYPES, SecurityType } from '../../core/api/models';
import { ReferentialsApi } from '../../core/api/referentials.api';
import { DonutChart, DonutShare } from '../../shared/charts/donut-chart';
import { FilterMenu, FilterOption } from '../../shared/filter-menu/filter-menu';
import { Icon } from '../../shared/icon/icon';
import { Notify } from '../../shared/notify/notify';
import { PageHeader } from '../../shared/page-header/page-header';
import { gainClass } from '../../shared/pipes/format';
import { AmountFrPipe, DateFrPipe, PercentFrPipe, PriceFrPipe, QuantityFrPipe } from '../../shared/pipes/format.pipes';
import { todayIso } from '../../shared/pipes/decimal-input';

type SortKey = keyof Pick<
  DashboardPosition,
  'accountName' | 'securityName' | 'quantity' | 'averageCost' | 'price' | 'priceDate' | 'marketValue' | 'unrealizedGain' | 'unrealizedGainPercent'
>;

const COLUMNS: { key: SortKey; label: string; numeric: boolean }[] = [
  { key: 'accountName', label: 'Compte', numeric: false },
  { key: 'securityName', label: 'Support', numeric: false },
  { key: 'quantity', label: 'Quantité', numeric: true },
  { key: 'averageCost', label: 'PRU', numeric: true },
  { key: 'price', label: 'Cours', numeric: true },
  { key: 'priceDate', label: 'Date du cours', numeric: true },
  { key: 'marketValue', label: 'Valorisation', numeric: true },
  { key: 'unrealizedGain', label: 'PV latente', numeric: true },
  { key: 'unrealizedGainPercent', label: 'PV latente %', numeric: true },
];

/** Tableau de bord (UC-13) : filtres, 4 répartitions, positions triables et tableau des comptes. */
@Component({
  selector: 'app-dashboard-page',
  imports: [MatTooltipModule, PageHeader, FilterMenu, DonutChart, Icon, AmountFrPipe, DateFrPipe, PercentFrPipe, PriceFrPipe, QuantityFrPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './dashboard-page.html',
  styleUrl: './dashboard-page.scss',
})
export class DashboardPage implements OnInit {
  private readonly dashboardApi = inject(DashboardApi);
  private readonly accountsApi = inject(AccountsApi);
  private readonly referentialsApi = inject(ReferentialsApi);
  private readonly notify = inject(Notify);

  protected readonly columns = COLUMNS;
  protected readonly gainClass = gainClass;
  protected readonly priceDecimals = priceDecimals;
  protected readonly today = todayIso();

  protected readonly accountOptions = signal<FilterOption[]>([]);
  protected readonly typeOptions: FilterOption[] = SECURITY_TYPES.map((t) => ({ value: t, label: SECURITY_TYPE_LABELS[t] }));
  protected readonly zoneOptions = signal<FilterOption[]>([]);
  protected readonly sectorOptions = signal<FilterOption[]>([]);

  protected readonly accountIds = signal<string[]>([]);
  protected readonly securityTypes = signal<string[]>([]);
  protected readonly zones = signal<string[]>([]);
  protected readonly sectors = signal<string[]>([]);

  protected readonly data = signal<DashboardPositions | null>(null);
  protected readonly accountsTable = signal<DashboardAccounts | null>(null);
  protected readonly sort = signal<{ key: SortKey; direction: 1 | -1 }>({ key: 'marketValue', direction: -1 });

  protected readonly allocations = computed(() => {
    const allocation = this.data()?.allocation;
    const shares = (items: DonutShare[] | undefined) => items ?? [];
    return [
      { title: 'Par compte', shares: shares(allocation?.byAccount) },
      { title: 'Par type de support', shares: shares(allocation?.bySecurityType) },
      { title: 'Par zone', shares: shares(allocation?.byZone) },
      { title: 'Par secteur', shares: shares(allocation?.bySector) },
    ];
  });

  protected readonly positions = computed(() => {
    const { key, direction } = this.sort();
    return [...(this.data()?.positions ?? [])].sort((a, b) => compare(a[key], b[key]) * direction);
  });

  constructor() {
    effect(() => {
      const filter = {
        accountIds: this.accountIds(),
        securityTypes: this.securityTypes() as SecurityType[],
        zones: this.zones(),
        sectors: this.sectors(),
      };
      forkJoin({ positions: this.dashboardApi.positions(filter), accounts: this.dashboardApi.accounts(filter.accountIds) }).subscribe({
        next: ({ positions, accounts }) => {
          this.data.set(positions);
          this.accountsTable.set(accounts);
        },
        error: (error: unknown) => this.notify.error(error),
      });
    });
  }

  ngOnInit(): void {
    // Zones et secteurs archivés compris, pour que les positions existantes restent filtrables (FS §3.10).
    forkJoin({
      accounts: this.accountsApi.list(),
      zones: this.referentialsApi.list('zones', true),
      sectors: this.referentialsApi.list('sectors', true),
    }).subscribe({
      next: ({ accounts, zones, sectors }) => {
        this.accountOptions.set(accounts.map((a) => ({ value: a.id, label: a.name })));
        this.zoneOptions.set(zones.map((z) => ({ value: z.code ?? '', label: z.archived ? `${z.label} (archivé)` : z.label })));
        this.sectorOptions.set(sectors.map((s) => ({ value: s.code ?? '', label: s.archived ? `${s.label} (archivé)` : s.label })));
      },
      error: (error: unknown) => this.notify.error(error),
    });
  }

  protected sortBy(key: SortKey): void {
    this.sort.update((current) => ({ key, direction: current.key === key ? ((current.direction * -1) as 1 | -1) : 1 }));
  }

  protected ariaSort(key: SortKey): 'ascending' | 'descending' | null {
    const sort = this.sort();
    return sort.key === key ? (sort.direction === 1 ? 'ascending' : 'descending') : null;
  }
}

function compare(a: string | number | null, b: string | number | null): number {
  if (a === b) {
    return 0;
  }

  if (a === null) {
    return 1;
  }

  if (b === null) {
    return -1;
  }

  return typeof a === 'string' && typeof b === 'string' ? a.localeCompare(b, 'fr') : a < b ? -1 : 1;
}
