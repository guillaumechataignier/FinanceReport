import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { Account, AccountRequest, Balance } from './models';

@Injectable({ providedIn: 'root' })
export class AccountsApi {
  private readonly http = inject(HttpClient);

  list(includeArchived = false): Observable<Account[]> {
    return this.http.get<Account[]>('/api/accounts', { params: new HttpParams().set('includeArchived', includeArchived) });
  }

  create(request: AccountRequest): Observable<Account> {
    return this.http.post<Account>('/api/accounts', request);
  }

  update(id: string, request: AccountRequest): Observable<Account> {
    return this.http.put<Account>(`/api/accounts/${id}`, request);
  }

  setArchived(id: string, archived: boolean): Observable<Account> {
    return this.http.post<Account>(`/api/accounts/${id}/${archived ? 'archive' : 'unarchive'}`, {});
  }

  balances(id: string): Observable<Balance[]> {
    return this.http.get<Balance[]>(`/api/accounts/${id}/balances`);
  }

  saveBalance(id: string, date: string, amount: number): Observable<Balance> {
    return this.http.put<Balance>(`/api/accounts/${id}/balances/${date}`, { amount });
  }

  deleteBalance(id: string, date: string): Observable<void> {
    return this.http.delete<void>(`/api/accounts/${id}/balances/${date}`);
  }
}
