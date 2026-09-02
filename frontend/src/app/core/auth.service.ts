import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiAuthStatus } from './api-types';

/** Server {error} body when present, otherwise the fallback. */
export function apiError(err: unknown, fallback: string): string {
  if (err instanceof HttpErrorResponse && typeof err.error?.error === 'string')
    return err.error.error;
  return fallback;
}

/** Session state and the /api/auth calls. Status is fetched once; login, setup and logout keep it current. */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);

  private status: Promise<ApiAuthStatus> | null = null;

  /** Signed-in account email; empty while anonymous. */
  readonly email = signal('');

  /** Cached auth status; the first call also plants the XSRF cookie. */
  ensureStatus(): Promise<ApiAuthStatus> {
    return (this.status ??= firstValueFrom(this.http.get<ApiAuthStatus>('/api/auth/status')).then(
      (s) => {
        this.email.set(s.email ?? '');
        return s;
      },
    ));
  }

  async login(email: string, password: string): Promise<void> {
    await firstValueFrom(this.http.post('/api/auth/login', { email, password }));
    this.setAuthenticated(email);
  }

  async setup(code: string, email: string, password: string): Promise<void> {
    await firstValueFrom(this.http.post('/api/auth/setup', { code, email, password }));
    this.setAuthenticated(email);
  }

  async logout(): Promise<void> {
    await firstValueFrom(this.http.post('/api/auth/logout', null));
    this.reset();
  }

  /** Forget the session locally, e.g. after a 401 or logout. */
  reset(): void {
    this.status = null;
    this.email.set('');
  }

  private setAuthenticated(email: string): void {
    this.status = Promise.resolve({ needsSetup: false, authenticated: true, email, isAdmin: null });
    this.email.set(email);
  }
}
