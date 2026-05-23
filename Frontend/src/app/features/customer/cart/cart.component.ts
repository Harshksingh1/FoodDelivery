import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, Router } from '@angular/router';
import { CartService } from '../../../core/services/cart.service';
import { ToastService } from '../../../shared/services/toast.service';

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './cart.component.html',
  styleUrl: './cart.component.scss'
})
export class CartComponent implements OnInit {
  cart = inject(CartService);
  toast = inject(ToastService);
  router = inject(Router);
  loading = signal(true);

  ngOnInit() {
    this.cart.loadCart().subscribe({ next: () => this.loading.set(false), error: () => this.loading.set(false) });
  }

  updateQty(menuItemId: string, qty: number) {
    this.cart.updateItem(menuItemId, qty).subscribe({
      error: () => this.toast.error('Failed to update cart.')
    });
  }

  clearCart() {
    this.cart.clearCart().subscribe({ next: () => this.toast.success('Cart cleared.'), error: () => this.toast.error('Failed.') });
  }
}
