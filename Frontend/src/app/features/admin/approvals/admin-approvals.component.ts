import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../core/services/admin.service';
import { ToastService } from '../../../shared/services/toast.service';

@Component({
  selector: 'app-admin-approvals',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-approvals.component.html',
  styleUrl: './admin-approvals.component.scss'
})
export class AdminApprovalsComponent implements OnInit {
  adminService = inject(AdminService);
  toast = inject(ToastService);

  applications = signal<any[]>([]);
  loading = signal(true);
  activeTab = signal('restaurants');
  filterStatus = signal('');
  rejectingId = signal('');
  expandedId = signal('');
  rejectReason = '';
  statuses = ['', 'Pending', 'Approved', 'Rejected'];

  ngOnInit() { this.loadData(); }

  loadData() {
    this.loading.set(true);
    this.expandedId.set('');
    const status = this.filterStatus() || undefined;
    const obs = this.activeTab() === 'restaurants'
      ? this.adminService.getRestaurantApplications(status)
      : this.adminService.getAgentApplications(status);
    obs.subscribe({
      next: a => { this.applications.set(a); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  toggleExpand(id: string) {
    this.expandedId.set(this.expandedId() === id ? '' : id);
  }

  openReject(id: string) { this.rejectingId.set(id); this.rejectReason = ''; }

  review(id: string, status: string) {
    const req = { status, rejectionReason: status === 'Rejected' ? this.rejectReason : undefined };
    const obs = this.activeTab() === 'restaurants'
      ? this.adminService.reviewRestaurantApplication(id, req)
      : this.adminService.reviewAgentApplication(id, req);
    obs.subscribe({
      next: res => { this.toast.success(res.message); this.rejectingId.set(''); this.loadData(); },
      error: err => this.toast.error(err.error?.message ?? 'Failed.')
    });
  }

  deleteRestaurant(id: string) {
    if (!id || !confirm('Delete this restaurant?')) return;
    this.adminService.deleteRestaurant(id).subscribe({
      next: () => { this.toast.success('Deleted.'); this.loadData(); },
      error: err => this.toast.error(err.error?.message ?? 'Failed.')
    });
  }
}
