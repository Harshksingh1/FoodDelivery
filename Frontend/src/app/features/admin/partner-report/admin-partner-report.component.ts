import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../core/services/admin.service';
import { ToastService } from '../../../shared/services/toast.service';

@Component({
  selector: 'app-admin-partner-report',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-partner-report.component.html',
  styleUrl: './admin-partner-report.component.scss'
})
export class AdminPartnerReportComponent implements OnInit {
  adminService = inject(AdminService);
  toast = inject(ToastService);

  restaurants = signal<any[]>([]);
  filtered = signal<any[]>([]);
  loading = signal(true);
  search = '';
  filterOpen = '';
  now = new Date();

  openCount = () => this.restaurants().filter(r => r.isOpen).length;
  avgRating = () => {
    const rated = this.restaurants().filter(r => r.rating > 0);
    if (!rated.length) return '0.0';
    return (rated.reduce((s, r) => s + r.rating, 0) / rated.length).toFixed(1);
  };
  uniqueCities = () => new Set(this.restaurants().map(r => r.city)).size;

  ngOnInit() {
    this.adminService.getPartnerReport().subscribe({
      next: data => {
        this.restaurants.set(data);
        this.filtered.set(data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  applyFilter() {
    let list = [...this.restaurants()];
    if (this.search.trim()) {
      const q = this.search.toLowerCase();
      list = list.filter(r =>
        r.restaurantName?.toLowerCase().includes(q) ||
        r.city?.toLowerCase().includes(q) ||
        r.cuisineType?.toLowerCase().includes(q)
      );
    }
    if (this.filterOpen !== '') {
      const open = this.filterOpen === 'true';
      list = list.filter(r => r.isOpen === open);
    }
    this.filtered.set(list);
  }

  deleteRestaurant(id: string) {
    if (!id || !confirm('Delete this restaurant permanently?')) return;
    this.adminService.deleteRestaurant(id).subscribe({
      next: () => {
        this.toast.success('Restaurant deleted.');
        this.restaurants.update(r => r.filter(x => x.restaurantId !== id));
        this.applyFilter();
      },
      error: err => this.toast.error(err.error?.message ?? 'Delete failed.')
    });
  }
}
