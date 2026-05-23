import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { CatalogService } from '../../../core/services/catalog.service';
import { CartService } from '../../../core/services/cart.service';
import { AuthService } from '../../../core/services/auth.service';
import { Restaurant, MenuItem } from '../../../core/models/restaurant.models';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-restaurant-detail',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './restaurant-detail.component.html',
  styleUrl: './restaurant-detail.component.scss'
})
export class RestaurantDetailComponent implements OnInit {
  route = inject(ActivatedRoute);
  router = inject(Router);
  catalogService = inject(CatalogService);
  cartService = inject(CartService);
  auth = inject(AuthService);
  catalogUrl = environment.catalogUrl;

  restaurant = signal<Restaurant | null>(null);
  menu = signal<MenuItem[]>([]);
  loading = signal(true);

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.catalogService.getRestaurantDetail(id).subscribe({
      next: res => { this.restaurant.set(res.restaurant); this.menu.set(res.menu); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
    if (this.auth.isLoggedIn()) this.cartService.loadCart().subscribe();
  }

  getQty(menuItemId: string) {
    return this.cartService.cart()?.items.find(i => i.menuItemId === menuItemId)?.quantity ?? 0;
  }

  addToCart(item: MenuItem) {
    if (!this.auth.isLoggedIn()) { this.router.navigate(['/auth/login']); return; }
    this.cartService.addItem({
      restaurantId: this.restaurant()!.id,
      restaurantName: this.restaurant()!.name,
      menuItemId: item.id,
      itemName: item.name,
      unitPrice: item.price,
      quantity: 1
    }).subscribe();
  }

  updateQty(item: MenuItem, delta: number) {
    const current = this.getQty(item.id);
    const newQty = current + delta;
    if (newQty <= 0) this.cartService.updateItem(item.id, 0).subscribe();
    else this.cartService.updateItem(item.id, newQty).subscribe();
  }
}
