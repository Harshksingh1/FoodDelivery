import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { UserService } from '../../../core/services/user.service';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './forgot-password.component.html',
  styleUrl: './forgot-password.component.scss'
})
export class ForgotPasswordComponent {
  userService = inject(UserService);
  email = ''; loading = signal(false); error = signal(''); sent = signal(false);

  onSubmit() {
    this.loading.set(true); this.error.set('');
    this.userService.forgotPassword({ email: this.email }).subscribe({
      next: () => { this.loading.set(false); this.sent.set(true); },
      error: err => { this.loading.set(false); this.error.set(err.error?.message ?? 'Failed to send reset link.'); }
    });
  }
}
