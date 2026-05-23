import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, switchMap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Order } from '../models/restaurant.models';

export interface CheckoutRequest {
  customerName: string;
  customerMobile: string;
  deliveryAddress: string;
  deliveryInstructions?: string;
  promoCode?: string;
  paymentMethod: string;
}

@Injectable({ providedIn: 'root' })
export class OrderService {
  private readonly base = `${environment.apiUrl}/gateway/orders`;

  // BehaviorSubject to trigger my-orders refresh
  private refreshMyOrdersSubject = new BehaviorSubject<void>(undefined);

  // Auto-refreshing observable for customer order list
  myOrders$: Observable<Order[]> = this.refreshMyOrdersSubject.pipe(
    switchMap(() => this.getMyOrders())
  );

  // BehaviorSubject to trigger restaurant orders refresh
  private refreshRestaurantOrdersSubject = new BehaviorSubject<string>('');

  // Auto-refreshing observable for partner order list
  restaurantOrders$: Observable<Order[]> = this.refreshRestaurantOrdersSubject.pipe(
    switchMap(restaurantId => restaurantId ? this.getRestaurantOrders(restaurantId) : new Observable<Order[]>())
  );

  constructor(private http: HttpClient) {}

  checkout(req: CheckoutRequest): Observable<{ message: string; data: Order }> {
    return this.http.post<{ message: string; data: Order }>(`${this.base}/checkout`, req);
  }

  getOrder(id: string): Observable<Order> {
    return this.http.get<Order>(`${this.base}/${id}`);
  }

  getMyOrders(): Observable<Order[]> {
    return this.http.get<Order[]>(`${this.base}/my`);
  }

  refreshMyOrders(): void {
    this.refreshMyOrdersSubject.next();
  }

  getRestaurantOrders(restaurantId: string): Observable<Order[]> {
    return this.http.get<Order[]>(`${this.base}/restaurant/${restaurantId}`);
  }

  refreshRestaurantOrders(restaurantId: string): void {
    this.refreshRestaurantOrdersSubject.next(restaurantId);
  }

  getAllOrders(status?: string): Observable<Order[]> {
    return this.http.get<Order[]>(`${this.base}`, { params: status ? { status } : {} });
  }

  getMyDeliveries(): Observable<Order[]> {
    return this.http.get<Order[]>(`${this.base}/my-deliveries`);
  }

  updateStatus(id: string, newStatus: string, note?: string): Observable<{ message: string }> {
    return this.http.patch<{ message: string }>(`${this.base}/${id}/status`, { newStatus, note });
  }

  assignAgent(id: string, agentId: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.base}/${id}/assign-agent`, { agentId });
  }
}
