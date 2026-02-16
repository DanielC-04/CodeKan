import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { AuthTokenResponse, LoginRequest, RegisterRequest } from './models/auth.models';
import { AuthSessionStore } from './auth-session.store';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly sessionStore = inject(AuthSessionStore);
  private readonly authUrl = `${environment.apiUrl}/api/auth`;

  register(request: RegisterRequest): Observable<ApiResponse<AuthTokenResponse>> {
    return this.http
      .post<ApiResponse<AuthTokenResponse>>(`${this.authUrl}/register`, request, {
        withCredentials: true
      })
      .pipe(tap((response) => this.applySession(response)));
  }

  login(request: LoginRequest): Observable<ApiResponse<AuthTokenResponse>> {
    return this.http
      .post<ApiResponse<AuthTokenResponse>>(`${this.authUrl}/login`, request, {
        withCredentials: true
      })
      .pipe(tap((response) => this.applySession(response)));
  }

  refresh(): Observable<ApiResponse<AuthTokenResponse>> {
    return this.http
      .post<ApiResponse<AuthTokenResponse>>(
        `${this.authUrl}/refresh`,
        {},
        {
          withCredentials: true
        }
      )
      .pipe(tap((response) => this.applySession(response)));
  }

  revoke(): Observable<ApiResponse<object>> {
    return this.http
      .post<ApiResponse<object>>(
        `${this.authUrl}/revoke`,
        {},
        {
          withCredentials: true
        }
      )
      .pipe(tap(() => this.sessionStore.clearSession()));
  }

  private applySession(response: ApiResponse<AuthTokenResponse>): void {
    this.sessionStore.setSession(response.data.accessToken, response.data.user);
  }
}
