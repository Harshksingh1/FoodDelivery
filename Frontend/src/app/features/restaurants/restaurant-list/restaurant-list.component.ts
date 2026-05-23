import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CatalogService } from '../../../core/services/catalog.service';
import { Restaurant, MenuItem } from '../../../core/models/restaurant.models';
import { RestaurantCardComponent } from '../../../shared/components/restaurant-card/restaurant-card.component';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-restaurant-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RestaurantCardComponent, RouterLink],
  templateUrl: './restaurant-list.component.html',
  styleUrl: './restaurant-list.component.scss'
})
export class RestaurantListComponent implements OnInit {
  route = inject(ActivatedRoute);
  catalogService = inject(CatalogService);
  catalogUrl = environment.catalogUrl;

  restaurants = signal<Restaurant[]>([]);
  menuItems = signal<MenuItem[]>([]);
  loading = signal(true);
  title = signal('All Restaurants');
  sortBy = '';
  filters = { veg: false, open: false };

  filtered = () => {
    let list = [...this.restaurants()];
    if (this.filters.open) list = list.filter(r => r.isOpen);
    if (this.sortBy === 'rating') list.sort((a, b) => b.rating - a.rating);
    if (this.sortBy === 'time') list.sort((a, b) => a.prepTimeMinutes - b.prepTimeMinutes);
    return list;
  };

  ngOnInit() {
    this.route.queryParams.subscribe(params => {
      const q = params['q'];
      const city = params['city'];
      if (q) {
        this.title.set(`Results for "${q}"`);
        this.loading.set(true);
        let done = 0;
        const finish = () => { if (++done === 2) this.loading.set(false); };
        this.catalogService.searchRestaurants(q).subscribe({ next: r => { this.restaurants.set(r); finish(); }, error: () => finish() });
        this.catalogService.searchMenu(q).subscribe({ next: m => { this.menuItems.set(m); finish(); }, error: () => finish() });
      } else {
        this.menuItems.set([]);
        this.catalogService.getRestaurants(city).subscribe({ next: r => { this.restaurants.set(r); this.loading.set(false); }, error: () => this.loading.set(false) });
      }
    });
  }

  toggleFilter(f: 'veg' | 'open') { this.filters[f] = !this.filters[f]; }
  applySort() {}
}
