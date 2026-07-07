import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { environment } from '../../environments/environment';
import {
  DashboardStats,
  EventResponse,
  EventSummary,
  EventType,
  GuestEventResponse,
  MagnetSize,
  OrderResponse,
  OrderStatus,
  PublicOrderView
} from './models';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class ApiService {
  constructor(private http: HttpClient, private auth: AuthService) {}

  private headers(): HttpHeaders {
    const token = this.auth.getToken();
    return token ? new HttpHeaders({ Authorization: `Bearer ${token}` }) : new HttpHeaders();
  }

  login(username: string, password: string) {
    return this.http.post<{ token: string; expiresAt: string }>(
      `${environment.apiUrl}/auth/login`,
      { username, password }
    );
  }

  createEvent(body: object) {
    return this.http.post<EventResponse>(`${environment.apiUrl}/events`, body, { headers: this.headers() });
  }

  getEvent(id: string) {
    return this.http.get<EventResponse>(`${environment.apiUrl}/events/${id}`, { headers: this.headers() });
  }

  getGuestEvent(slug: string) {
    return this.http.get<GuestEventResponse>(`${environment.apiUrl}/events/guest/${slug}`);
  }

  updateEventSettings(id: string, body: object) {
    return this.http.patch<EventResponse>(`${environment.apiUrl}/events/${id}/settings`, body, { headers: this.headers() });
  }

  endEvent(id: string) {
    return this.http.post<EventSummary>(`${environment.apiUrl}/events/${id}/end`, null, { headers: this.headers() });
  }

  getSummary(id: string) {
    return this.http.get<EventSummary>(`${environment.apiUrl}/events/${id}/summary`, { headers: this.headers() });
  }

  getOrders(eventId: string, all = false) {
    const q = all ? '?all=true' : '';
    return this.http.get<OrderResponse[]>(`${environment.apiUrl}/events/${eventId}/orders${q}`, { headers: this.headers() });
  }

  searchOrders(eventId: string, q: string) {
    return this.http.get<OrderResponse[]>(
      `${environment.apiUrl}/events/${eventId}/orders/search?q=${encodeURIComponent(q)}`,
      { headers: this.headers() }
    );
  }

  getStats(eventId: string) {
    return this.http.get<DashboardStats>(`${environment.apiUrl}/events/${eventId}/stats`, { headers: this.headers() });
  }

  updateOrderStatus(orderId: string, status: OrderStatus) {
    return this.http.patch<OrderResponse>(
      `${environment.apiUrl}/orders/${orderId}/status`,
      { status },
      { headers: this.headers() }
    );
  }

  getOrderImage(orderId: string) {
    return this.http.get(`${environment.apiUrl}/orders/${orderId}/image`, {
      headers: this.headers(),
      responseType: 'blob'
    });
  }

  createOrder(slug: string, formData: FormData) {
    return this.http.post<PublicOrderView>(`${environment.apiUrl}/events/${slug}/orders`, formData);
  }

  getOrderStatus(publicToken: string) {
    return this.http.get<PublicOrderView>(`${environment.apiUrl}/o/${publicToken}`);
  }

  cancelOrder(publicToken: string) {
    return this.http.post<PublicOrderView>(`${environment.apiUrl}/o/${publicToken}/cancel`, null);
  }

  reportClientIssue(payload: object) {
    return this.http.post(`${environment.apiUrl}/diagnostics/client-error`, payload);
  }
}
