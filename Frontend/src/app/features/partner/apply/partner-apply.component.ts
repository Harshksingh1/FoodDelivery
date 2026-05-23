import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PartnerService, RestaurantApplication } from '../../../core/services/partner.service';
import { ToastService } from '../../../shared/services/toast.service';

@Component({
  selector: 'app-partner-apply',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './partner-apply.component.html',
  styleUrl: './partner-apply.component.scss'
})
export class PartnerApplyComponent implements OnInit {
  partnerService = inject(PartnerService);
  toast = inject(ToastService);

  existing = signal<RestaurantApplication | null>(null);
  loading = signal(false); error = signal(''); success = signal('');
  form = { restaurantName: '', address: '', city: '', pincode: '', cuisineType: 'Indian', gst: '', fssai: '' };
  cuisines = ['Indian', 'Chinese', 'Italian', 'Mexican', 'Continental', 'South Indian', 'North Indian', 'Fast Food', 'Desserts','American'];

  ngOnInit() {
    this.partnerService.getMyApplication().subscribe({ next: res => this.existing.set(res.data), error: () => {} });
  }

  onSubmit() {
    this.loading.set(true); this.error.set('');
    this.partnerService.applyForRestaurant(this.form).subscribe({
      next: res => { this.loading.set(false); this.success.set(res.message); this.ngOnInit(); },
      error: err => { this.loading.set(false); this.error.set(err.error?.message ?? 'Submission failed.'); }
    });
  }
}
