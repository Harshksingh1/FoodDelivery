import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink, ActivatedRoute, Router } from '@angular/router';
import { UserService } from '../../../core/services/user.service';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './reset-password.component.html',
  styleUrl: './reset-password.component.scss'
})
export class ResetPasswordComponent implements OnInit {
  userService = inject(UserService);
  route = inject(ActivatedRoute);
  router = inject(Router);

  token = ''; newPassword = ''; confirmPassword = '';
  loading = signal(false); error = signal(''); done = signal(false);

  ngOnInit() { this.token = this.route.snapshot.queryParamMap.get('token') ?? ''; }

  onSubmit() {
    if (this.newPassword !== this.confirmPassword) { this.error.set('Passwords do not match.'); return; }
    this.loading.set(true); this.error.set('');
    this.userService.resetPassword({ token: this.token, newPassword: this.newPassword }).subscribe({
      next: () => { this.loading.set(false); this.done.set(true); },
      error: err => { this.loading.set(false); this.error.set(err.error?.message ?? 'Reset failed.'); }
    });
  }
}
