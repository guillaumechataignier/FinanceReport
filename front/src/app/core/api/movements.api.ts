import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { Movement, MovementFilter, MovementRequest } from './models';

@Injectable({ providedIn: 'root' })
export class MovementsApi {
  private readonly http = inject(HttpClient);

  list(filter: MovementFilter = {}): Observable<Movement[]> {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(filter)) {
      if (value) {
        params = params.set(key, value);
      }
    }

    return this.http.get<Movement[]>('/api/movements', { params });
  }

  create(request: MovementRequest): Observable<Movement> {
    return this.http.post<Movement>('/api/movements', request);
  }

  update(id: string, request: MovementRequest): Observable<Movement> {
    return this.http.put<Movement>(`/api/movements/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`/api/movements/${id}`);
  }
}
