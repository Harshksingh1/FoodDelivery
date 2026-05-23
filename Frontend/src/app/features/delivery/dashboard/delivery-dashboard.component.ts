import { Component, OnInit, inject, signal, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { interval } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { OrderService } from '../../../core/services/order.service';
import { PartnerService, DeliveryAgentApplication } from '../../../core/services/partner.service';
import { ToastService } from '../../../shared/services/toast.service';
import { Order } from '../../../core/models/restaurant.models';

@Component({
  selector: 'app-delivery-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './delivery-dashboard.component.html',
  styleUrl: './delivery-dashboard.component.scss'
})
export class DeliveryDashboardComponent implements OnInit {
  orderService = inject(OrderService);
  partnerService = inject(PartnerService);
  toast = inject(ToastService);
  #destroyRef = inject(DestroyRef);

  orders = signal<Order[]>([]);
  application = signal<DeliveryAgentApplication | null>(null);
  applying = signal(false);
  agentForm = { location: '', aadhaarNumber: '', vehicleType: 'Bike', vehicleNumber: '', licenseNumber: '' };

  ngOnInit() {
    this.loadDeliveries();
    this.partnerService.getMyAgentApplication().subscribe({ next: res => this.application.set(res.data), error: () => {} });
    // Auto-refresh deliveries every 30 seconds
    interval(30_000).pipe(takeUntilDestroyed(this.#destroyRef)).subscribe(() => this.loadDeliveries());
  }

  private loadDeliveries() {
    this.orderService.getMyDeliveries().subscribe({ next: o => this.orders.set(o), error: () => {} });
  }

  applyAgent() {
    this.applying.set(true);
    this.partnerService.applyAsDeliveryAgent(this.agentForm).subscribe({
      next: res => { this.applying.set(false); this.toast.success(res.message); this.loadDeliveries(); },
      error: err => { this.applying.set(false); this.toast.error(err.error?.message ?? 'Failed.'); }
    });
  }

  updateStatus(orderId: string, status: string) {
    this.orderService.updateStatus(orderId, status).subscribe({
      next: () => { this.toast.success(`Order marked as ${status}`); this.loadDeliveries(); },
      error: err => this.toast.error(err.error?.message ?? 'Failed.')
    });
  }
}
