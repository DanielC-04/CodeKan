import { TestBed } from '@angular/core/testing';
import { AuthSessionStore } from './auth-session.store';
import { LocalStoreService } from '../storage/local-store.service';

describe('AuthSessionStore', () => {
  let store: AuthSessionStore;
  let localStore: LocalStoreService;

  beforeEach(() => {
    localStorage.clear();

    TestBed.configureTestingModule({
      providers: [AuthSessionStore, LocalStoreService]
    });

    store = TestBed.inject(AuthSessionStore);
    localStore = TestBed.inject(LocalStoreService);
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('clears previous user kanban cache when switching accounts', () => {
    store.setSession('token-a', { id: 'user-a', email: 'a@test.local', role: 'Member' });
    localStore.set('devboard.activeProjectId.user-a', 'project-a');
    localStore.set('devboard.kanban.user-a.project-a', [{ id: 'task-a' }]);
    localStore.set('devboard.activeProjectId', 'legacy-project');
    localStore.set('devboard.kanban.legacyproject', [{ id: 'legacy-task' }]);

    store.setSession('token-b', { id: 'user-b', email: 'b@test.local', role: 'Member' });

    expect(localStore.get('devboard.activeProjectId.user-a')).toBeNull();
    expect(localStore.get('devboard.kanban.user-a.project-a')).toBeNull();
    expect(localStore.get('devboard.activeProjectId')).toBeNull();
    expect(localStore.get('devboard.kanban.legacyproject')).toBeNull();
  });
});
