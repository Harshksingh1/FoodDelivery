import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-verify-otp',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './verify-otp.component.html',
  styleUrl: './verify-otp.component.scss'
})
export class VerifyOtpComponent implements OnInit {
  auth = inject(AuthService);
  router = inject(Router);

  otpDigits = ['', '', '', '', '', ''];
  sessionToken = '';
  loading = signal(false);
  error = signal('');

  otp = () => this.otpDigits.join('');

  ngOnInit() {
    const state = history.state;
    this.sessionToken = state?.token ?? '';
    if (!this.sessionToken) this.router.navigate(['/auth/login']);
  }

  onInput(index: number, event: Event) {
    const val = (event.target as HTMLInputElement).value;
    this.otpDigits[index] = val.slice(-1);
    if (val && index < 5) {
      document.getElementById(`otp-${index + 1}`)?.focus();
    }
  }

  onBackspace(index: number) {
    if (!this.otpDigits[index] && index > 0) {
      document.getElementById(`otp-${index - 1}`)?.focus();
    }
  }

  onVerify() {
    this.loading.set(true);
    this.error.set('');
    this.auth.verifyOtp({ otpSessionToken: this.sessionToken, otp: this.otp() }).subscribe({
      next: res => {
        this.loading.set(false);
        this.auth.navigateByRole(res.data.role);
      },
      error: err => {
        this.loading.set(false);
        this.error.set(err.error?.message ?? 'Invalid OTP.');
        this.otpDigits = ['', '', '', '', '', ''];
      }
    });
  }
}
