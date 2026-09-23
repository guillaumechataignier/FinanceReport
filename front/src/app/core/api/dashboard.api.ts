import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface Summary {
  date: string;
  totalNetWorth: number;
  monthVariation: { amount: number; percent: number | null; referenceDate: string } | null;
  unrealizedGain: { amount: number; percent: number | null };
  byAccountType: { type: string; amount: number; percent: number }[];
  missingPriceCount: number;
}

@Injectable({ providedIn: 'root' })
export class DashboardApi {
  private readonly http = inject(HttpClient);

  summary(): Observable<Summary> {
    return this.http.get<Summary>('/api/dashboard/summary');
  }
}
