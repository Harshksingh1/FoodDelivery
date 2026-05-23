import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink, Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss'
})
export class RegisterComponent {
  auth = inject(AuthService);
  router = inject(Router);

  form = { fullName: '', email: '', mobile: '', password: '', role: 'Customer' };
  loading = signal(false);
  error = signal('');
  success = signal('');

  // Field-level validation errors
  errors = signal<Record<string, string>>({});

  // Getter for template use — avoids language service squiggles
  get fieldErrors(): Record<string, string> {
    return this.errors();
  }

  roles = [
    { value: 'Customer', label: 'Customer', emoji: '🛒' },
    { value: 'RestaurantPartner', label: 'Partner', emoji: '🍴' },
    { value: 'DeliveryAgent', label: 'Delivery', emoji: '🛵' },
  ];

  private validate(): boolean {
    const e: Record<string, string> = {};

    if (!this.form.fullName.trim())
      e['fullName'] = 'Full name is required.';
    else if (this.form.fullName.trim().length < 2)
      e['fullName'] = 'Name must be at least 2 characters.';

    if (!this.form.email.trim())
      e['email'] = 'Email is required.';
    else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(this.form.email))
      e['email'] = 'Enter a valid email address.';

    if (!this.form.mobile.trim())
      e['mobile'] = 'Mobile number is required.';
    else if (!/^\d{10}$/.test(this.form.mobile.trim()))
      e['mobile'] = 'Mobile must be exactly 10 digits.';

    if (!this.form.password)
      e['password'] = 'Password is required.';
    else if (this.form.password.length < 8)
      e['password'] = 'Password must be at least 8 characters.';
    else if (!/[A-Z]/.test(this.form.password))
      e['password'] = 'Password must contain at least one uppercase letter.';
    else if (!/\d/.test(this.form.password))
      e['password'] = 'Password must contain at least one number.';

    this.errors.set(e);
    return Object.keys(e).length === 0;
  }

  // Clear field error on input
  clearError(field: string): void {
    this.errors.update(e => { const n = { ...e }; delete n[field]; return n; });
  }

  onSubmit() {
    this.error.set('');
    this.success.set('');
    if (!this.validate()) return;

    this.loading.set(true);
    this.auth.register(this.form).subscribe({
      next: res => {
        this.loading.set(false);
        this.success.set(res.message);
        setTimeout(() => this.router.navigate(['/auth/login']), 1500);
      },
      error: err => {
        this.loading.set(false);
        this.error.set(err.error?.message ?? 'Registration failed. Please try again.');
      }
    });
  }


}
