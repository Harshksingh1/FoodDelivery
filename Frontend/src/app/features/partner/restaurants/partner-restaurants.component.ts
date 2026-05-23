import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PartnerService, MenuItemRequest } from '../../../core/services/partner.service';
import { ToastService } from '../../../shared/services/toast.service';
import { Restaurant, MenuItem } from '../../../core/models/restaurant.models';
import { CatalogService } from '../../../core/services/catalog.service';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-partner-restaurants',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './partner-restaurants.component.html',
  styleUrl: './partner-restaurants.component.scss'
})
export class PartnerRestaurantsComponent implements OnInit {
  partnerService = inject(PartnerService);
  catalogService = inject(CatalogService);
  toast = inject(ToastService);
  apiUrl = environment.catalogUrl;

  restaurants = signal<Restaurant[]>([]);
  menuMap = signal<Record<string, MenuItem[]>>({});
  loading = signal(true);
  editingId = signal('');
  addingMenuFor = signal('');
  editForm = { name: '', address: '', city: '', pincode: '', cuisineType: '', prepTimeMinutes: 30, minOrderAmount: 0 };
  menuForm: MenuItemRequest = { name: '', description: '', category: '', price: 0, isVeg: true, isAvailable: true };

  ngOnInit() {
    this.partnerService.getMyRestaurants().subscribe({
      next: r => {
        this.restaurants.set(r);
        this.loading.set(false);
        r.forEach(rest => this.loadMenu(rest.id));
      },
      error: () => this.loading.set(false)
    });
  }

  loadMenu(restaurantId: string) {
    this.catalogService.getMenu(restaurantId).subscribe({
      next: items => this.menuMap.update(m => ({ ...m, [restaurantId]: items }))
    });
  }

  getMenu(restaurantId: string) { return this.menuMap()[restaurantId] ?? []; }

  startEdit(r: Restaurant) {
    this.editingId.set(r.id);
    this.editForm = { name: r.name, address: r.address, city: r.city, pincode: '', cuisineType: r.cuisineType, prepTimeMinutes: r.prepTimeMinutes, minOrderAmount: r.minOrderAmount };
  }

  saveEdit(id: string) {
    this.partnerService.updateRestaurant(id, this.editForm).subscribe({
      next: () => { this.toast.success('Restaurant updated!'); this.editingId.set(''); this.ngOnInit(); },
      error: err => this.toast.error(err.error?.message ?? 'Update failed.')
    });
  }

  toggleStatus(r: Restaurant) {
    this.partnerService.setStatus(r.id, !r.isOpen).subscribe({
      next: () => { this.toast.success('Status updated!'); this.ngOnInit(); },
      error: err => this.toast.error(err.error?.message ?? 'Failed.')
    });
  }

  deleteRestaurant(id: string) {
    if (!confirm('Delete this restaurant?')) return;
    this.partnerService.deleteRestaurant(id).subscribe({
      next: () => { this.toast.success('Restaurant deleted.'); this.restaurants.update(r => r.filter(x => x.id !== id)); },
      error: err => this.toast.error(err.error?.message ?? 'Failed.')
    });
  }

  addMenuItem(restaurantId: string) {
    this.partnerService.addMenuItem(restaurantId, this.menuForm).subscribe({
      next: () => { this.toast.success('Menu item added!'); this.addingMenuFor.set(''); this.loadMenu(restaurantId); this.menuForm = { name: '', description: '', category: '', price: 0, isVeg: true, isAvailable: true }; },
      error: err => this.toast.error(err.error?.message ?? 'Failed.')
    });
  }

  deleteMenuItem(itemId: string, restaurantId: string) {
    this.partnerService.deleteMenuItem(itemId).subscribe({
      next: () => { this.toast.success('Deleted.'); this.loadMenu(restaurantId); },
      error: err => this.toast.error(err.error?.message ?? 'Failed.')
    });
  }

  onRestaurantImageChange(event: Event, restaurantId: string) {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.partnerService.uploadRestaurantImage(restaurantId, file).subscribe({
      next: res => {
        this.restaurants.update(list => list.map(r => r.id === restaurantId ? { ...r, imageUrl: res.imageUrl } : r));
        this.toast.success('Restaurant image updated!');
      },
      error: () => this.toast.error('Image upload failed.')
    });
  }

  onMenuItemImageChange(event: Event, itemId: string, restaurantId: string) {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.partnerService.uploadMenuItemImage(itemId, file).subscribe({
      next: res => {
        this.menuMap.update(m => ({
          ...m,
          [restaurantId]: (m[restaurantId] ?? []).map(i => i.id === itemId ? { ...i, imageUrl: res.imageUrl } : i)
        }));
        this.toast.success('Menu item image updated!');
      },
      error: () => this.toast.error('Image upload failed.')
    });
  }
}
