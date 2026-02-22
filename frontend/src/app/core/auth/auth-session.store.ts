import { computed, Injectable, signal } from '@angular/core';
import { AuthUser } from './models/auth.models';
import { LocalStoreService } from '../storage/local-store.service';

interface PersistedSession {
  accessToken: string;
  user: AuthUser;
}

const SESSION_KEY = 'devboard.session';
const ACTIVE_PROJECT_KEY_PREFIX = 'devboard.activeProjectId';
const KANBAN_SNAPSHOT_KEY_PREFIX = 'devboard.kanban';

@Injectable({ providedIn: 'root' })
export class AuthSessionStore {
  private readonly accessTokenState = signal<string | null>(null);
  private readonly userState = signal<AuthUser | null>(null);

  readonly accessToken = this.accessTokenState.asReadonly();
  readonly user = this.userState.asReadonly();
  readonly isAuthenticated = computed(() => !!this.accessTokenState());

  constructor(private readonly localStore: LocalStoreService) {
    const persisted = this.localStore.get<PersistedSession>(SESSION_KEY);
    if (persisted?.accessToken && persisted?.user) {
      this.accessTokenState.set(persisted.accessToken);
      this.userState.set(persisted.user);
    }
  }

  setSession(accessToken: string, user: AuthUser): void {
    const previousUserId = this.userState()?.id;
    if (previousUserId && previousUserId !== user.id) {
      this.clearKanbanCacheForUser(previousUserId);
    }

    this.accessTokenState.set(accessToken);
    this.userState.set(user);
    this.localStore.set(SESSION_KEY, { accessToken, user });
  }

  clearSession(): void {
    const currentUserId = this.userState()?.id;
    if (currentUserId) {
      this.clearKanbanCacheForUser(currentUserId);
    }

    this.accessTokenState.set(null);
    this.userState.set(null);
    this.localStore.remove(SESSION_KEY);
  }

  private clearKanbanCacheForUser(userId: string): void {
    this.localStore.remove(`${ACTIVE_PROJECT_KEY_PREFIX}.${userId}`);
    this.localStore.removeByPrefix(`${KANBAN_SNAPSHOT_KEY_PREFIX}.${userId}.`);
    this.localStore.remove(ACTIVE_PROJECT_KEY_PREFIX);

    for (const key of this.localStore.keys()) {
      if (/^devboard\.kanban\.[^.]+$/.test(key)) {
        this.localStore.remove(key);
      }
    }
  }
}
