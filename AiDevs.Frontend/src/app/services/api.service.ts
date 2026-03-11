import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ApiConfig {
  baseUrl: string;
}

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly config: ApiConfig = {
    baseUrl: 'http://localhost:5000/api' // Update with your .NET backend URL
  };

  get<T>(endpoint: string): Observable<T> {
    return this.http.get<T>(`${this.config.baseUrl}/${endpoint}`);
  }

  post<T>(endpoint: string, data: unknown): Observable<T> {
    return this.http.post<T>(`${this.config.baseUrl}/${endpoint}`, data);
  }

  put<T>(endpoint: string, data: unknown): Observable<T> {
    return this.http.put<T>(`${this.config.baseUrl}/${endpoint}`, data);
  }

  delete<T>(endpoint: string): Observable<T> {
    return this.http.delete<T>(`${this.config.baseUrl}/${endpoint}`);
  }

  getStreamUrl(endpoint: string): string {
    return `${this.config.baseUrl}/${endpoint}`;
  }
}
