import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, switchMap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { MenuItem, Restaurant } from '../models/restaurant.models';

@Injectable({ providedIn: 'root' })
export class CatalogService {
  private readonly base = `${environment.apiUrl}/gateway/catalog`;

  // BehaviorSubject to trigger restaurant list refresh
  private refreshRestaurantsSubject = new BehaviorSubject<{ city?: string; cuisine?: string; isOpen?: boolean }>({});

  // Auto-refreshing observable — components can subscribe to this directly
  restaurants$: Observable<Restaurant[]> = this.refreshRestaurantsSubject.pipe(
    switchMap(params => this.getRestaurants(params.city, params.cuisine, params.isOpen))
  );

  constructor(private http: HttpClient) {}

  getRestaurants(city?: string, cuisine?: string, isOpen?: boolean): Observable<Restaurant[]> {
    const params: Record<string, string | boolean> = {};
    if (city) params['city'] = city;
    if (cuisine) params['cuisine'] = cuisine;
    if (isOpen !== undefined) params['isOpen'] = isOpen;
    return this.http.get<Restaurant[]>(`${this.base}/restaurants`, { params });
  }

  refreshRestaurants(city?: string, cuisine?: string, isOpen?: boolean): void {
    this.refreshRestaurantsSubject.next({ city, cuisine, isOpen });
  }

  searchRestaurants(q: string): Observable<Restaurant[]> {
    return this.http.get<Restaurant[]>(`${this.base}/restaurants/search`, { params: { q } });
  }

  searchMenu(q: string): Observable<MenuItem[]> {
    return this.http.get<MenuItem[]>(`${this.base}/menu/search`, { params: { q } });
  }

  getRestaurantDetail(id: string): Observable<{ restaurant: Restaurant; menu: MenuItem[] }> {
    return this.http.get<{ restaurant: Restaurant; menu: MenuItem[] }>(`${this.base}/restaurants/${id}`);
  }

  getMenu(restaurantId: string): Observable<MenuItem[]> {
    return this.http.get<MenuItem[]>(`${this.base}/restaurants/${restaurantId}/menu`);
  }

  rateRestaurant(restaurantId: string, stars: number): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.base}/restaurants/${restaurantId}/rate`, { stars });
  }
}
