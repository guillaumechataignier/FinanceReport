import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { Price, Security, SecurityRequest } from './models';

@Injectable({ providedIn: 'root' })
export class SecuritiesApi {
  private readonly http = inject(HttpClient);

  list(includeArchived = false): Observable<Security[]> {
    return this.http.get<Security[]>('/api/securities', { params: new HttpParams().set('includeArchived', includeArchived) });
  }

  create(request: SecurityRequest): Observable<Security> {
    return this.http.post<Security>('/api/securities', request);
  }

  update(id: string, request: SecurityRequest): Observable<Security> {
    return this.http.put<Security>(`/api/securities/${id}`, request);
  }

  setArchived(id: string, archived: boolean): Observable<Security> {
    return this.http.post<Security>(`/api/securities/${id}/${archived ? 'archive' : 'unarchive'}`, {});
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`/api/securities/${id}`);
  }

  prices(id: string): Observable<Price[]> {
    return this.http.get<Price[]>(`/api/securities/${id}/prices`);
  }

  savePrice(id: string, date: string, price: number): Observable<Price> {
    return this.http.put<Price>(`/api/securities/${id}/prices/${date}`, { price });
  }

  deletePrice(id: string, date: string): Observable<void> {
    return this.http.delete<void>(`/api/securities/${id}/prices/${date}`);
  }
}
