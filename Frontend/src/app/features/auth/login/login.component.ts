import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink, Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  auth = inject(AuthService);
  router = inject(Router);

  email = '';
  password = '';
  loading = signal(false);
  error = signal('');
  showPwd = signal(false);

  onSubmit() {
    this.loading.set(true);
    this.error.set('');
    this.auth.login({ email: this.email, password: this.password }).subscribe({
      next: res => {
        this.loading.set(false);
        if (res.data.requiresOtp) {
          this.router.navigate(['/auth/verify-otp'], { state: { token: res.data.otpSessionToken } });
        } else if (res.data.authData) {
          this.auth.setSession(res.data.authData);
          this.auth.navigateByRole(res.data.authData.role);
        }
      },
      error: err => {
        this.loading.set(false);
        this.error.set(err.error?.message ?? 'Login failed. Please try again.');
      }
    });
  }
}
