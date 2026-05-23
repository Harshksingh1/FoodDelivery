import { Component, OnInit, inject, signal, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { interval } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { OrderService } from '../../../core/services/order.service';
import { PartnerService } from '../../../core/services/partner.service';
import { AdminService } from '../../../core/services/admin.service';
import { ToastService } from '../../../shared/services/toast.service';
import { Order } from '../../../core/models/restaurant.models';
import { Restaurant } from '../../../core/models/restaurant.models';

@Component({
  selector: 'app-partner-orders',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './partner-orders.component.html',
  styleUrl: './partner-orders.component.scss'
})
export class PartnerOrdersComponent implements OnInit {
  orderService = inject(OrderService);
  partnerService = inject(PartnerService);
  adminService = inject(AdminService);
  toast = inject(ToastService);
  #destroyRef = inject(DestroyRef);

  restaurants = signal<Restaurant[]>([]);
  orders = signal<Order[]>([]);
  deliveryAgents = signal<{ id: string; fullName: string; mobile: string }[]>([]);
  loading = signal(true);
  selectedRestaurantId = signal('');
  filterStatus = signal('');
  expandedId = signal('');
  assigningOrderId = signal('');
  selectedAgentId = '';

  statusFilters = [
    { value: '', label: 'All' },
    { value: 'Paid', label: '💰 New (Paid)' },
    { value: 'RestaurantAccepted', label: '✓ Accepted' },
    { value: 'Preparing', label: '🍳 Preparing' },
    { value: 'ReadyForPickup', label: '✅ Ready' },
    { value: 'Delivered', label: '🎉 Delivered' },
    { value: 'Cancelled', label: '❌ Cancelled' },
  ];

  filteredOrders = () => {
    const status = this.filterStatus();
    if (!status) return this.orders();
    return this.orders().filter(o => o.status === status);
  };

  ngOnInit() {
    this.adminService.getApprovedDeliveryAgents().subscribe({
      next: agents => this.deliveryAgents.set(agents),
      error: () => {}
    });

    this.partnerService.getMyRestaurants().subscribe({
      next: r => {
        this.restaurants.set(r);
        if (r.length > 0) {
          this.selectedRestaurantId.set(r[0].id);
          this.loadOrders(r[0].id);
          // Auto-refresh every 30 seconds
          interval(30_000).pipe(takeUntilDestroyed(this.#destroyRef)).subscribe(
            () => this.loadOrders(this.selectedRestaurantId())
          );
        } else {
          this.loading.set(false);
        }
      },
      error: () => this.loading.set(false)
    });
  }

  selectRestaurant(id: string) {
    this.selectedRestaurantId.set(id);
    this.loadOrders(id);
  }

  loadOrders(restaurantId: string) {
    this.loading.set(true);
    this.orderService.getRestaurantOrders(restaurantId).subscribe({
      next: o => { this.orders.set(o); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  toggleExpand(id: string) { this.expandedId.set(this.expandedId() === id ? '' : id); }

  openAssignAgent(id: string) {
    this.assigningOrderId.set(id);
    this.selectedAgentId = '';
  }

  updateStatus(orderId: string, status: string) {
    this.orderService.updateStatus(orderId, status).subscribe({
      next: res => { this.toast.success(res.message); this.loadOrders(this.selectedRestaurantId()); },
      error: err => this.toast.error(err.error?.message ?? 'Failed.')
    });
  }

  assignAgent(orderId: string) {
    if (!this.selectedAgentId) return;
    this.orderService.assignAgent(orderId, this.selectedAgentId).subscribe({
      next: res => { this.toast.success(res.message); this.assigningOrderId.set(''); this.selectedAgentId = ''; this.loadOrders(this.selectedRestaurantId()); },
      error: err => this.toast.error(err.error?.message ?? 'Failed.')
    });
  }

  statusClass(status: string) {
    const map: Record<string, string> = {
      Paid: 'bg-green-100 text-green-700',
      RestaurantAccepted: 'bg-blue-100 text-blue-700',
      Preparing: 'bg-orange-100 text-orange-700',
      ReadyForPickup: 'bg-purple-100 text-purple-700',
      PickedUp: 'bg-indigo-100 text-indigo-700',
      OutForDelivery: 'bg-cyan-100 text-cyan-700',
      Delivered: 'bg-emerald-100 text-emerald-700',
      Cancelled: 'bg-red-100 text-red-700',
      RestaurantRejected: 'bg-red-100 text-red-700',
      PaymentPending: 'bg-yellow-100 text-yellow-700',
    };
    return map[status] ?? 'bg-gray-100 text-gray-700';
  }
}
