import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { AccountType, SecurityType } from './models';

export interface Summary {
  date: string;
  totalNetWorth: number;
  monthVariation: { amount: number; percent: number | null; referenceDate: string } | null;
  unrealizedGain: { amount: number; percent: number | null };
  byAccountType: { type: AccountType; amount: number; percent: number }[];
  missingPriceCount: number;
}

export type HistoryPeriod = '1M' | '1A' | 'ALL';

export interface History {
  period: HistoryPeriod;
  points: { date: string; totalNetWorth: number }[];
}

export interface DashboardFilter {
  accountIds: string[];
  securityTypes: SecurityType[];
  zones: string[];
  sectors: string[];
}

export interface AllocationShare {
  key: string;
  label: string;
  amount: number;
  percent: number;
}

export interface DashboardPosition {
  accountId: string;
  accountName: string;
  securityId: string;
  securityName: string;
  securityCode: string;
  securityType: SecurityType;
  zone: string;
  zoneLabel: string;
  sector: string;
  sectorLabel: string;
  quantity: number;
  averageCost: number;
  price: number;
  priceDate: string | null;
  missingPrice: boolean;
  marketValue: number;
  unrealizedGain: number;
  unrealizedGainPercent: number | null;
}

export interface DashboardPositions {
  positions: DashboardPosition[];
  allocation: {
    byAccount: AllocationShare[];
    bySecurityType: AllocationShare[];
    byZone: AllocationShare[];
    bySector: AllocationShare[];
  };
  includesCash: boolean;
}

export interface DashboardAccounts {
  accounts: {
    accountId: string;
    name: string;
    type: AccountType;
    value: number;
    unrealizedGain: number | null;
    realizedGain: number | null;
  }[];
  total: { value: number; unrealizedGain: number; realizedGain: number };
}

@Injectable({ providedIn: 'root' })
export class DashboardApi {
  private readonly http = inject(HttpClient);

  summary(): Observable<Summary> {
    return this.http.get<Summary>('/api/dashboard/summary');
  }

  history(period: HistoryPeriod): Observable<History> {
    return this.http.get<History>('/api/dashboard/history', { params: { period } });
  }

  positions(filter: DashboardFilter): Observable<DashboardPositions> {
    return this.http.get<DashboardPositions>('/api/dashboard/positions', { params: toParams(filter) });
  }

  accounts(accountIds: string[]): Observable<DashboardAccounts> {
    return this.http.get<DashboardAccounts>('/api/dashboard/accounts', { params: toParams({ accountIds }) });
  }
}

/** Filtres multiples en valeurs séparées par des virgules (FS §4.2). */
function toParams(filter: Partial<DashboardFilter>): HttpParams {
  let params = new HttpParams();
  for (const [key, values] of Object.entries(filter)) {
    if (values && values.length > 0) {
      params = params.set(key, values.join(','));
    }
  }

  return params;
}
