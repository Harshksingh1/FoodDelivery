import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AdminService, AdminUser } from '../../../core/services/admin.service';
import { ToastService } from '../../../shared/services/toast.service';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './admin-users.component.html',
  styleUrl: './admin-users.component.scss'
})
export class AdminUsersComponent implements OnInit {
  adminService = inject(AdminService);
  toast = inject(ToastService);

  users = signal<AdminUser[]>([]);
  loading = signal(true);
  activeTab = signal('customers');

  tabs = [
    { key: 'customers', label: 'Customers', emoji: '🛒' },
    { key: 'partners', label: 'Partners', emoji: '🍴' },
    { key: 'agents', label: 'Delivery Agents', emoji: '🛵' },
  ];

  ngOnInit() { this.loadUsers(); }

  loadUsers() {
    this.loading.set(true);
    const obs = this.activeTab() === 'customers' ? this.adminService.getCustomers()
      : this.activeTab() === 'partners' ? this.adminService.getRestaurantPartners()
      : this.adminService.getDeliveryAgents();
    obs.subscribe({ next: u => { this.users.set(u); this.loading.set(false); }, error: () => this.loading.set(false) });
  }

  toggleActive(u: AdminUser) {
    this.adminService.toggleActive(u.id).subscribe({
      next: res => { u.isActive = res.isActive; this.toast.success(`User ${res.isActive ? 'activated' : 'deactivated'}.`); },
      error: err => this.toast.error(err.error?.message ?? 'Failed.')
    });
  }

  deleteUser(id: string) {
    if (!confirm('Delete this user permanently?')) return;
    this.adminService.deleteUser(id).subscribe({
      next: () => { this.users.update(u => u.filter(x => x.id !== id)); this.toast.success('User deleted.'); },
      error: err => this.toast.error(err.error?.message ?? 'Failed.')
    });
  }
}
