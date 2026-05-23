import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../core/services/admin.service';

interface DayBreakdown { date: string; orderCount: number; revenue: number; }
interface RestaurantRevenue {
  restaurantId: string;
  restaurantName: string;
  totalOrders: number;
  totalRevenue: number;
  dailyBreakdown: DayBreakdown[];
  expanded: boolean;
}

@Component({
  selector: 'app-admin-revenue-report',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-revenue-report.component.html',
  styleUrl: './admin-revenue-report.component.scss'
})
export class AdminRevenueReportComponent implements OnInit {
  adminService = inject(AdminService);

  data = signal<RestaurantRevenue[]>([]);
  loading = signal(true);

  // Default: last 30 days
  fromDate = this.toInputDate(new Date(Date.now() - 30 * 24 * 60 * 60 * 1000));
  toDate   = this.toInputDate(new Date());

  totalRevenue  = computed(() => this.data().reduce((s, r) => s + r.totalRevenue, 0));
  totalOrders   = computed(() => this.data().reduce((s, r) => s + r.totalOrders, 0));

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.adminService.getRevenueReport(this.fromDate, this.toDate).subscribe({
      next: rows => {
        this.data.set(rows.map((r: any) => ({ ...r, expanded: false })));
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  toggle(r: RestaurantRevenue) { r.expanded = !r.expanded; }

  private toInputDate(d: Date): string {
    return d.toISOString().slice(0, 10);
  }
}
