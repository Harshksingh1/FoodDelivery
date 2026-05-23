import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { PartnerService } from '../../../core/services/partner.service';
import { OrderService } from '../../../core/services/order.service';
import { Restaurant, Order } from '../../../core/models/restaurant.models';

interface RestaurantRevenue {
  restaurant: Restaurant;
  orders: Order[];
  revenue: number;
  deliveredCount: number;
}

@Component({
  selector: 'app-partner-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './partner-dashboard.component.html',
  styleUrl: './partner-dashboard.component.scss'
})
export class PartnerDashboardComponent implements OnInit {
  partnerService = inject(PartnerService);
  orderService = inject(OrderService);
  router = inject(Router);

  restaurants = signal<Restaurant[]>([]);
  allRevenueByRestaurant = signal<RestaurantRevenue[]>([]);
  recentOrders = signal<Order[]>([]);
  showRevenueBreakdown = signal(false);

  // Date filter — default: last 30 days
  fromDate = this.toInputDate(new Date(Date.now() - 30 * 24 * 60 * 60 * 1000));
  toDate   = this.toInputDate(new Date());

  // Applied range — only updates on Apply click
  appliedFrom = signal(this.toInputDate(new Date(Date.now() - 30 * 24 * 60 * 60 * 1000)));
  appliedTo   = signal(this.toInputDate(new Date()));

  filteredRevenue = computed(() => {
    const from = new Date(this.appliedFrom());
    const to   = new Date(this.appliedTo());
    to.setHours(23, 59, 59, 999);
    return this.allRevenueByRestaurant().map(r => {
      const filtered = r.orders.filter(o =>
        o.status === 'Delivered' &&
        new Date(o.createdAt) >= from &&
        new Date(o.createdAt) <= to
      );
      return { ...r, revenue: filtered.reduce((s, o) => s + o.totalAmount, 0), deliveredCount: filtered.length };
    });
  });

  applyFilter() {
    this.appliedFrom.set(this.fromDate);
    this.appliedTo.set(this.toDate);
  }

  totalRevenue  = computed(() => this.filteredRevenue().reduce((s, r) => s + r.revenue, 0));
  totalOrders   = computed(() => this.allRevenueByRestaurant().reduce((s, r) => s + r.orders.length, 0));
  openCount     = computed(() => this.restaurants().filter(r => r.isOpen).length);

  ngOnInit() {
    this.partnerService.getMyRestaurants().subscribe({
      next: restaurants => {
        this.restaurants.set(restaurants);
        if (restaurants.length === 0) return;

        forkJoin(restaurants.map(r => this.orderService.getRestaurantOrders(r.id))).subscribe({
          next: allOrders => {
            const breakdown: RestaurantRevenue[] = restaurants.map((r, i) => {
              const orders = allOrders[i];
              const delivered = orders.filter(o => o.status === 'Delivered');
              return { restaurant: r, orders, revenue: delivered.reduce((s, o) => s + o.totalAmount, 0), deliveredCount: delivered.length };
            });
            this.allRevenueByRestaurant.set(breakdown);

            const all = allOrders.flat().sort(
              (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
            );
            this.recentOrders.set(all);
          }
        });
      }
    });
  }

  private toInputDate(d: Date): string {
    return d.toISOString().slice(0, 10);
  }

  statusClass(status: string) {
    const map: Record<string, string> = {
      Paid: 'bg-green-100 text-green-700', RestaurantAccepted: 'bg-blue-100 text-blue-700',
      Preparing: 'bg-orange-100 text-orange-700', ReadyForPickup: 'bg-purple-100 text-purple-700',
      Delivered: 'bg-emerald-100 text-emerald-700', Cancelled: 'bg-red-100 text-red-700',
    };
    return map[status] ?? 'bg-gray-100 text-gray-700';
  }
}
