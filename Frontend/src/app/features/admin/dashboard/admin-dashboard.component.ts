import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AdminService } from '../../../core/services/admin.service';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './admin-dashboard.component.html',
  styleUrl: './admin-dashboard.component.scss'
})
export class AdminDashboardComponent implements OnInit {
  adminService = inject(AdminService);
  dashboard = signal<any>(null);
  salesReport = signal<any[]>([]);
  fromDate = new Date(Date.now() - 30 * 86400000).toISOString().split('T')[0];
  toDate = new Date().toISOString().split('T')[0];

  quickLinks = [
    { label: 'Manage Users', desc: 'View and manage all users', emoji: '👥', route: '/admin/users' },
    { label: 'Approvals', desc: 'Review pending applications', emoji: '✅', route: '/admin/approvals' },
    { label: 'Partner Report', desc: 'All restaurants with details', emoji: '📊', route: '/admin/partner-report' },
  ];

  ngOnInit() {
    this.adminService.getDashboard().subscribe({ next: d => this.dashboard.set(d) });
  }

  loadSales() {
    this.adminService.getSalesReport(this.fromDate, this.toDate).subscribe({ next: r => this.salesReport.set(r) });
  }
}
