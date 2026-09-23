import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ReferentialItem, ReferentialItemRequest, ReferentialKind } from './models';

@Injectable({ providedIn: 'root' })
export class ReferentialsApi {
  private readonly http = inject(HttpClient);

  list(kind: ReferentialKind, includeArchived = false): Observable<ReferentialItem[]> {
    return this.http.get<ReferentialItem[]>(`/api/referentials/${kind}`, {
      params: new HttpParams().set('includeArchived', includeArchived),
    });
  }

  create(kind: ReferentialKind, request: ReferentialItemRequest): Observable<ReferentialItem> {
    return this.http.post<ReferentialItem>(`/api/referentials/${kind}`, request);
  }

  update(kind: ReferentialKind, id: string, request: ReferentialItemRequest): Observable<ReferentialItem> {
    return this.http.put<ReferentialItem>(`/api/referentials/${kind}/${id}`, request);
  }

  setArchived(kind: ReferentialKind, id: string, archived: boolean): Observable<ReferentialItem> {
    return this.http.post<ReferentialItem>(`/api/referentials/${kind}/${id}/${archived ? 'archive' : 'unarchive'}`, {});
  }

  delete(kind: ReferentialKind, id: string): Observable<void> {
    return this.http.delete<void>(`/api/referentials/${kind}/${id}`);
  }
}
