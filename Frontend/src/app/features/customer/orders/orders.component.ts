import { Component, OnInit, inject, signal, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { interval } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { OrderService } from '../../../core/services/order.service';
import { CatalogService } from '../../../core/services/catalog.service';
import { ToastService } from '../../../shared/services/toast.service';
import { Order } from '../../../core/models/restaurant.models';

@Component({
  selector: 'app-orders',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './orders.component.html',
  styleUrl: './orders.component.scss'
})
export class OrdersComponent implements OnInit {
  orderService = inject(OrderService);
  catalogService = inject(CatalogService);
  toast = inject(ToastService);
  #destroyRef = inject(DestroyRef);

  orders = signal<Order[]>([]);
  loading = signal(true);
  selected = signal<Order | null>(null);
  ratedOrders = signal<Set<string>>(new Set());
  hoverRating = signal<Record<string, number>>({});

  ngOnInit() {
    this.loadOrders();
    // Auto-refresh every 30 seconds
    interval(30_000).pipe(takeUntilDestroyed(this.#destroyRef)).subscribe(() => this.loadOrders());
  }

  private loadOrders() {
    this.orderService.getMyOrders().subscribe({
      next: o => { this.orders.set(o); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  setHover(orderId: string, star: number) {
    this.hoverRating.update(h => ({ ...h, [orderId]: star }));
  }

  clearHover(orderId: string) {
    this.hoverRating.update(h => { const n = { ...h }; delete n[orderId]; return n; });
  }

  submitRating(order: Order, stars: number) {
    this.catalogService.rateRestaurant(order.restaurantId, stars).subscribe({
      next: () => {
        this.ratedOrders.update(s => new Set([...s, order.id]));
        this.toast.success(`Rated ${order.restaurantName} ${stars}⭐`);
      },
      error: () => this.toast.error('Rating failed.')
    });
  }

  statusClass(status: string) {
    const map: Record<string, string> = {
      Delivered: 'bg-green-100 text-green-700',
      Cancelled: 'bg-red-100 text-red-700',
      PaymentPending: 'bg-yellow-100 text-yellow-700',
      Paid: 'bg-blue-100 text-blue-700',
      Preparing: 'bg-orange-100 text-orange-700',
      OutForDelivery: 'bg-purple-100 text-purple-700',
    };
    return map[status] ?? 'bg-gray-100 text-gray-700';
  }
}
