import { inject, Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../../environments/environment';
import { AuthSessionStore } from '../auth/auth-session.store';

@Injectable({ providedIn: 'root' })
export class SignalrService {
  private readonly authSessionStore = inject(AuthSessionStore);
  private hubConnection: signalR.HubConnection | null = null;

  startConnection(): Promise<void> {
    const token = this.authSessionStore.accessToken();
    if (!token || this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      return Promise.resolve();
    }

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/hubs/devboard`, {
        accessTokenFactory: () => token
      })
      .withAutomaticReconnect()
      .build();

    return this.hubConnection.start();
  }

  onTaskUpdated(handler: (eventData: unknown) => void): void {
    this.hubConnection?.on('TaskUpdated', handler);
  }

  offTaskUpdated(handler: (eventData: unknown) => void): void {
    this.hubConnection?.off('TaskUpdated', handler);
  }

  async stopConnection(): Promise<void> {
    if (!this.hubConnection) {
      return;
    }

    await this.hubConnection.stop();
    this.hubConnection = null;
  }
}
