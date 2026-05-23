import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CatalogService } from '../../core/services/catalog.service';
import { Restaurant } from '../../core/models/restaurant.models';
import { RestaurantCardComponent } from '../../shared/components/restaurant-card/restaurant-card.component';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, RestaurantCardComponent],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent implements OnInit {
  router = inject(Router);
  catalogService = inject(CatalogService);

  searchQuery = '';
  city = '';
  selectedCuisine = signal('');
  restaurants = signal<Restaurant[]>([]);
  loading = signal(true);

  visibleRestaurants = computed(() => this.restaurants().slice(0, 8));
  hasMore = computed(() => this.restaurants().length > 8);

  services = [
    { title: 'FOOD DELIVERY', subtitle: 'FROM RESTAURANTS', offer: 'UPTO 60% OFF', emoji: '🍱' },
    { title: 'QUICK BITES', subtitle: 'INSTANT DELIVERY', offer: 'UPTO 50% OFF', emoji: '⚡' },
    { title: 'DINEOUT', subtitle: 'EAT OUT & SAVE MORE', offer: 'UPTO 40% OFF', emoji: '🍽️' },
  ];

  cuisines = [
    { name: 'Pizza', emoji: '🍕' }, { name: 'North Indian', emoji: '🍛' },
    { name: 'Biryani', emoji: '🍚' }, { name: 'Burger', emoji: '🍔' },
    { name: 'Chinese', emoji: '🥡' }, { name: 'Noodles', emoji: '🍜' },
    { name: 'Momos', emoji: '🥟' }, { name: 'Desserts', emoji: '🍰' },
    { name: 'South Indian', emoji: '🥘' }, { name: 'Roll', emoji: '🌯' },
    { name: 'Sandwich', emoji: '🥪' }, { name: 'Pasta', emoji: '🍝' },
  ];

  ngOnInit() { this.loadRestaurants(); }

  loadRestaurants() {
    this.loading.set(true);
    this.catalogService.getRestaurants(this.city || undefined, this.selectedCuisine() || undefined).subscribe({
      next: r => { this.restaurants.set(r); this.loading.set(false); },
      error: () => { this.restaurants.set([]); this.loading.set(false); }
    });
  }

  filterByCuisine(name: string) {
    if (this.selectedCuisine() === name) {
      this.selectedCuisine.set('');
      this.loadRestaurants();
    } else {
      this.router.navigate(['/restaurants'], { queryParams: { q: name } });
    }
  }

  onSearch() {
    this.router.navigate(['/restaurants'], { queryParams: { q: this.searchQuery, city: this.city } });
  }

  scrollCuisines(dir: number) {}
}
