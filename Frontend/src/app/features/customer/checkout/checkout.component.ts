import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CartService } from '../../../core/services/cart.service';
import { OrderService } from '../../../core/services/order.service';
import { PaymentService } from '../../../core/services/payment.service';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../shared/services/toast.service';
import { Order } from '../../../core/models/restaurant.models';

@Component({
  selector: 'app-checkout',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './checkout.component.html',
  styleUrl: './checkout.component.scss'
})
export class CheckoutComponent implements OnInit {
  cart = inject(CartService);
  orderService = inject(OrderService);
  paymentService = inject(PaymentService);
  auth = inject(AuthService);
  toast = inject(ToastService);
  router = inject(Router);

  step = signal(1);
  placing = signal(false);
  placedOrder = signal<Order | null>(null);

  form = {
    customerName: '',
    customerMobile: '',
    deliveryAddress: '',
    deliveryInstructions: '',
    paymentMethod: 'COD'
  };

  steps = [
    { n: 1, label: 'Details' },
    { n: 2, label: 'Payment' },
    { n: 3, label: 'Confirm' }
  ];

  paymentMethods = [
    { value: 'COD', label: 'Cash on Delivery', desc: 'Pay when your order arrives' },
    { value: 'UPI', label: 'UPI', desc: 'Pay via UPI (simulated)' },
    { value: 'Card', label: 'Credit / Debit Card', desc: 'Pay via card (simulated)' },
  ];

  get step1Valid(): boolean {
    return !!(this.form.customerName.trim() &&
              this.form.customerMobile.trim() &&
              this.form.deliveryAddress.trim());
  }

  totalAmount(): number {
    return this.cart.total() + this.cart.total() * 0.05 + 30;
  }

  ngOnInit(): void {
    if (!this.cart.cart() || this.cart.cart()!.items.length === 0) {
      this.router.navigate(['/customer/cart']);
    }
  }

  placeOrder(): void {
    this.placing.set(true);
    this.orderService.checkout({
      customerName: this.form.customerName,
      customerMobile: this.form.customerMobile,
      deliveryAddress: this.form.deliveryAddress,
      deliveryInstructions: this.form.deliveryInstructions || undefined,
      paymentMethod: this.form.paymentMethod
    }).subscribe({
      next: res => {
        const order = res.data;
        this.paymentService.simulate({
          orderId: order.id,
          customerId: this.auth.user()!.userId,
          amount: order.totalAmount,
          method: this.form.paymentMethod
        }).subscribe({
          next: () => {
            this.placing.set(false);
            this.placedOrder.set(order);
            this.step.set(3);
          },
          error: () => {
            this.placing.set(false);
            this.placedOrder.set(order);
            this.step.set(3);
            this.toast.info('Order placed. Payment pending.');
          }
        });
      },
      error: (err: { error?: { message?: string } }) => {
        this.placing.set(false);
        this.toast.error(err.error?.message ?? 'Order failed.');
      }
    });
  }
}
