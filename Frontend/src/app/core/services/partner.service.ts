import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, switchMap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Restaurant, MenuItem } from '../models/restaurant.models';

export interface RestaurantApplicationRequest { restaurantName: string; address: string; city: string; pincode: string; cuisineType: string; gst: string; fssai: string; }
export interface UpdateRestaurantRequest { name: string; address: string; city: string; pincode: string; cuisineType: string; prepTimeMinutes: number; minOrderAmount: number; }
export interface MenuItemRequest { name: string; description: string; category: string; price: number; isVeg: boolean; isAvailable: boolean; }
export interface RestaurantApplication { id: string; userId: string; restaurantName: string; address: string; city: string; cuisineType: string; status: string; appliedAt: string; reviewedAt?: string; rejectionReason?: string; restaurantId?: string; }
export interface DeliveryAgentApplicationRequest { location: string; aadhaarNumber: string; vehicleType: string; vehicleNumber: string; licenseNumber: string; }
export interface DeliveryAgentApplication { id: string; userId: string; location: string; vehicleType: string; vehicleNumber: string; status: string; appliedAt: string; }

@Injectable({ providedIn: 'root' })
export class PartnerService {
  private readonly base = `${environment.apiUrl}/gateway/catalog`;

  // BehaviorSubject to trigger restaurant list refresh
  private refreshRestaurantsSubject = new BehaviorSubject<void>(undefined);

  // Auto-refreshing observable for partner's restaurant list
  myRestaurants$: Observable<Restaurant[]> = this.refreshRestaurantsSubject.pipe(
    switchMap(() => this.getMyRestaurants())
  );

  constructor(private http: HttpClient) {}

  // ── Restaurants ────────────────────────────────────────────────────────────

  getMyRestaurants(): Observable<Restaurant[]> {
    return this.http.get<Restaurant[]>(`${this.base}/partner/restaurants`);
  }

  refreshMyRestaurants(): void {
    this.refreshRestaurantsSubject.next();
  }

  updateRestaurant(id: string, req: UpdateRestaurantRequest): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.base}/partner/restaurants/${id}`, req);
  }

  deleteRestaurant(id: string): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.base}/partner/restaurants/${id}`);
  }

  setStatus(id: string, isOpen: boolean): Observable<{ message: string }> {
    return this.http.patch<{ message: string }>(`${this.base}/partner/restaurants/${id}/status`, null, { params: { isOpen } });
  }

  uploadRestaurantImage(id: string, file: File): Observable<{ message: string; imageUrl: string }> {
    const fd = new FormData();
    fd.append('file', file);
    return this.http.post<{ message: string; imageUrl: string }>(`${this.base}/partner/restaurants/${id}/image`, fd);
  }

  // ── Menu ───────────────────────────────────────────────────────────────────

  addMenuItem(restaurantId: string, req: MenuItemRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.base}/partner/restaurants/${restaurantId}/menu`, req);
  }

  updateMenuItem(itemId: string, req: MenuItemRequest): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.base}/partner/menu/${itemId}`, req);
  }

  deleteMenuItem(itemId: string): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.base}/partner/menu/${itemId}`);
  }

  uploadMenuItemImage(itemId: string, file: File): Observable<{ message: string; imageUrl: string }> {
    const fd = new FormData();
    fd.append('file', file);
    return this.http.post<{ message: string; imageUrl: string }>(`${this.base}/partner/menu/${itemId}/image`, fd);
  }

  // ── Applications ───────────────────────────────────────────────────────────

  applyForRestaurant(req: RestaurantApplicationRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.base}/partner/applications`, req);
  }

  getMyApplication(): Observable<{ data: RestaurantApplication }> {
    return this.http.get<{ data: RestaurantApplication }>(`${this.base}/partner/applications/mine`);
  }

  applyAsDeliveryAgent(req: DeliveryAgentApplicationRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.base}/delivery-agent/applications`, req);
  }

  getMyAgentApplication(): Observable<{ data: DeliveryAgentApplication }> {
    return this.http.get<{ data: DeliveryAgentApplication }>(`${this.base}/delivery-agent/applications/mine`);
  }
}
