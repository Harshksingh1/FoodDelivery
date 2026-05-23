import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, switchMap, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Cart } from '../models/restaurant.models';

export interface AddItemRequest {
  restaurantId: string;
  restaurantName: string;
  menuItemId: string;
  itemName: string;
  unitPrice: number;
  quantity: number;
}

@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly base = `${environment.apiUrl}/gateway/cart`;

  // Internal signal for reactive cart state
  private _cart = signal<Cart | null>(null);

  readonly cart     = this._cart.asReadonly();
  readonly itemCount = computed(() => this._cart()?.items.reduce((s, i) => s + i.quantity, 0) ?? 0);
  readonly total     = computed(() => this._cart()?.items.reduce((s, i) => s + i.unitPrice * i.quantity, 0) ?? 0);

  // BehaviorSubject to trigger cart refresh
  private refreshCartSubject = new BehaviorSubject<void>(undefined);

  // Auto-refreshing observable — emits latest cart whenever refresh is triggered
  cart$: Observable<Cart> = this.refreshCartSubject.pipe(
    switchMap(() => this.fetchCart())
  );

  constructor(private http: HttpClient) {}

  private fetchCart(): Observable<Cart> {
    return this.http.get<Cart>(`${this.base}`).pipe(
      tap(c => this._cart.set(c))
    );
  }

  loadCart(): Observable<Cart> {
    return this.fetchCart();
  }

  refreshCart(): void {
    this.refreshCartSubject.next();
  }

  addItem(req: AddItemRequest): Observable<Cart> {
    return this.http.post<Cart>(`${this.base}/items`, req).pipe(
      tap(c => this._cart.set(c))
    );
  }

  updateItem(menuItemId: string, quantity: number): Observable<Cart> {
    return this.http.put<Cart>(`${this.base}/items/${menuItemId}`, { quantity }).pipe(
      tap(c => this._cart.set(c))
    );
  }

  clearCart(): Observable<void> {
    return this.http.delete<void>(`${this.base}`).pipe(
      tap(() => this._cart.set(null))
    );
  }
}
