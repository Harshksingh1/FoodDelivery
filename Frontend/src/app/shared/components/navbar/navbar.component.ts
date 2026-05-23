import { Component, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { CartService } from '../../../core/services/cart.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, FormsModule],
  templateUrl: './navbar.component.html',
  styleUrl: './navbar.component.scss'
})
export class NavbarComponent {
  auth = inject(AuthService);
  cart = inject(CartService);
  router = inject(Router);

  searchQuery = '';
  menuOpen = signal(false);

  logoLink() {
    if (this.auth.role() === 'Admin') return '/admin/dashboard';
    if (this.auth.role() === 'RestaurantPartner') return '/partner/dashboard';
    if (this.auth.role() === 'DeliveryAgent') return '/delivery/dashboard';
    return '/';
  }

  onSearch() {
    if (this.searchQuery.trim()) {
      this.router.navigate(['/restaurants'], { queryParams: { q: this.searchQuery } });
    }
  }

  onLogout() {
    const rt = localStorage.getItem('refreshToken') ?? '';
    this.auth.logout(rt).subscribe({ error: () => this.auth.clearSession() });
    this.menuOpen.set(false);
  }

  dashboardLink() {
    const map: Record<string, string> = {
      RestaurantPartner: '/partner/dashboard',
      DeliveryAgent: '/delivery/dashboard',
      Customer: '/customer/orders'
    };
    return map[this.auth.role() ?? ''] ?? '/';
  }

  roleBadgeClass() {
    const map: Record<string, string> = {
      Admin: 'bg-purple-100 text-purple-700',
      RestaurantPartner: 'bg-orange-100 text-orange-700',
      DeliveryAgent: 'bg-blue-100 text-blue-700',
      Customer: 'bg-green-100 text-green-700'
    };
    return map[this.auth.role() ?? ''] ?? 'bg-gray-100 text-gray-700';
  }
}
