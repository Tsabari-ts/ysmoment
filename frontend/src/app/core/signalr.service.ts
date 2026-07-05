import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { environment } from '../../environments/environment';
import { DashboardStats, OrderResponse } from './models';
import { AuthService } from './auth.service';

export interface QueueUpdate {
  orders: OrderResponse[];
  stats: DashboardStats;
}

@Injectable({ providedIn: 'root' })
export class SignalrService {
  private connection?: signalR.HubConnection;
  readonly queueUpdated$ = new Subject<QueueUpdate>();

  constructor(private auth: AuthService) {}

  async connect(eventId: string): Promise<void> {
    await this.disconnect();

    const token = this.auth.getToken();
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(environment.hubUrl, { accessTokenFactory: () => token ?? '' })
      .withAutomaticReconnect()
      .build();

    this.connection.on('QueueUpdated', (payload: QueueUpdate) => {
      this.queueUpdated$.next(payload);
    });

    await this.connection.start();
    await this.connection.invoke('JoinEvent', eventId);
  }

  async disconnect(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = undefined;
    }
  }
}
