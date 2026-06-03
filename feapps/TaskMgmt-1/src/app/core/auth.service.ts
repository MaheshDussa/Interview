import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { LoginRequest, LoginResponse } from './models';

const TOKEN_KEY = 'tm_token';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl.replace(/\/+$/, '');

  readonly isAuthenticated = signal<boolean>(this.hasToken());

  login(payload: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.base}/api/Auth/login`, payload).pipe(
      tap(res => {
        const token = res?.token ?? res?.accessToken ?? res?.jwt;
        if (token) {
          this.setToken(token);
        }
      })
    );
  }

  logout(): void {
    if (typeof localStorage !== 'undefined') {
      localStorage.removeItem(TOKEN_KEY);
    }
    this.isAuthenticated.set(false);
  }

  getToken(): string | null {
    if (typeof localStorage === 'undefined') return null;
    return localStorage.getItem(TOKEN_KEY);
  }

  private setToken(token: string): void {
    if (typeof localStorage !== 'undefined') {
      localStorage.setItem(TOKEN_KEY, token);
    }
    this.isAuthenticated.set(true);
  }

  private hasToken(): boolean {
    return !!this.getToken();
  }
}
