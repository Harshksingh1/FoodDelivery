import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { UserService, ProfileResponse } from '../../../core/services/user.service';
import { ToastService } from '../../services/toast.service';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss'
})
export class ProfileComponent implements OnInit {
  userService = inject(UserService);
  toast = inject(ToastService);
  auth = inject(AuthService);

  profile = signal<ProfileResponse | null>(null);
  loading = signal(true);
  saving = signal(false);
  pwdLoading = signal(false);
  pwdError = signal('');
  form = { fullName: '', mobile: '' };
  pwd = { current: '', newPwd: '', confirm: '' };

  ngOnInit() {
    this.userService.getProfile().subscribe({
      next: res => {
        this.profile.set(res.data);
        this.form = { fullName: res.data.fullName, mobile: res.data.mobile };
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  onSave() {
    this.saving.set(true);
    this.userService.updateProfile(this.form).subscribe({
      next: () => { this.saving.set(false); this.toast.success('Profile updated!'); },
      error: err => { this.saving.set(false); this.toast.error(err.error?.message ?? 'Update failed.'); }
    });
  }

  onChangePassword() {
    this.pwdError.set('');
    if (this.pwd.newPwd !== this.pwd.confirm) { this.pwdError.set('Passwords do not match.'); return; }
    this.pwdLoading.set(true);
    this.userService.changePassword({ currentPassword: this.pwd.current, newPassword: this.pwd.newPwd }).subscribe({
      next: () => { this.pwdLoading.set(false); this.toast.success('Password changed!'); this.pwd = { current: '', newPwd: '', confirm: '' }; },
      error: err => { this.pwdLoading.set(false); this.pwdError.set(err.error?.message ?? 'Failed.'); }
    });
  }
}
