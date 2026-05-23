import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, switchMap } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface ProfileResponse { userId: string; fullName: string; email: string; mobile: string; role: string; createdAt: string; }
export interface UpdateProfileRequest { fullName: string; mobile: string; }
export interface ChangePasswordRequest { currentPassword: string; newPassword: string; }
export interface ForgotPasswordRequest { email: string; }
export interface ResetPasswordRequest { token: string; newPassword: string; }

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly base = `${environment.apiUrl}/gateway/auth`;

  // BehaviorSubject to trigger profile refresh
  private refreshProfileSubject = new BehaviorSubject<void>(undefined);

  // Auto-refreshing observable for profile data
  profile$: Observable<{ data: ProfileResponse }> = this.refreshProfileSubject.pipe(
    switchMap(() => this.getProfile())
  );

  constructor(private http: HttpClient) {}

  getProfile(): Observable<{ data: ProfileResponse }> {
    return this.http.get<{ data: ProfileResponse }>(`${this.base}/profile`);
  }

  refreshProfile(): void {
    this.refreshProfileSubject.next();
  }

  updateProfile(req: UpdateProfileRequest): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.base}/profile`, req);
  }

  changePassword(req: ChangePasswordRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.base}/password/change`, req);
  }

  forgotPassword(req: ForgotPasswordRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.base}/password/forgot`, req);
  }

  resetPassword(req: ResetPasswordRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.base}/password/reset`, req);
  }
}
