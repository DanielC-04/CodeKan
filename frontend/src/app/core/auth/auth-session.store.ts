import { computed, Injectable, signal } from '@angular/core';
import { AuthUser } from './models/auth.models';
import { LocalStoreService } from '../storage/local-store.service';

interface PersistedSession {
  accessToken: string;
  user: AuthUser;
}

@Injectable({ providedIn: 'root' })
export class AuthSessionStore {
  private readonly accessTokenState = signal<string | null>(null);
  private readonly userState = signal<AuthUser | null>(null);

  readonly accessToken = this.accessTokenState.asReadonly();
  readonly user = this.userState.asReadonly();
  readonly isAuthenticated = computed(() => !!this.accessTokenState());

  constructor(private readonly localStore: LocalStoreService) {
    const persisted = this.localStore.get<PersistedSession>('devboard.session');
    if (persisted?.accessToken && persisted?.user) {
      this.accessTokenState.set(persisted.accessToken);
      this.userState.set(persisted.user);
    }
  }

  setSession(accessToken: string, user: AuthUser): void {
    this.accessTokenState.set(accessToken);
    this.userState.set(user);
    this.localStore.set('devboard.session', { accessToken, user });
  }

  clearSession(): void {
    this.accessTokenState.set(null);
    this.userState.set(null);
    this.localStore.remove('devboard.session');
  }
}
