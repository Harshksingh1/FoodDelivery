import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, LoginRequest, LoginResponse, RegisterRequest, User, VerifyOtpRequest } from '../models/auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly base = `${environment.apiUrl}/gateway/auth`;

  private _user  = signal<User | null>(this.loadUser());
  private _token = signal<string | null>(sessionStorage.getItem('accessToken'));

  readonly user      = this._user.asReadonly();
  readonly token     = this._token.asReadonly();
  readonly isLoggedIn = computed(() => !!this._token());
  readonly role       = computed(() => this._user()?.role ?? null);

  constructor(private http: HttpClient, private router: Router) {}

  register(req: RegisterRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.base}/register`, req);
  }

  login(req: LoginRequest): Observable<{ message: string; data: LoginResponse }> {
    return this.http.post<{ message: string; data: LoginResponse }>(`${this.base}/login`, req);
  }

  verifyOtp(req: VerifyOtpRequest): Observable<{ message: string; data: AuthResponse }> {
    return this.http.post<{ message: string; data: AuthResponse }>(`${this.base}/login/verify-otp`, req).pipe(
      tap(res => this.setSession(res.data))
    );
  }

  logout(refreshToken: string): Observable<object> {
    return this.http.post(`${this.base}/logout`, { refreshToken }).pipe(
      tap(() => this.clearSession())
    );
  }

  refreshToken(refreshToken: string): Observable<{ data: AuthResponse }> {
    return this.http.post<{ data: AuthResponse }>(`${this.base}/token/refresh`, { refreshToken }).pipe(
      tap(res => this.setSession(res.data))
    );
  }

  setSession(auth: AuthResponse): void {
    sessionStorage.setItem('accessToken', auth.accessToken);
    sessionStorage.setItem('refreshToken', auth.refreshToken);
    const user: User = { userId: auth.userId, fullName: auth.fullName, email: auth.email, role: auth.role };
    sessionStorage.setItem('user', JSON.stringify(user));
    this._token.set(auth.accessToken);
    this._user.set(user);
  }

  clearSession(): void {
    sessionStorage.clear();
    this._token.set(null);
    this._user.set(null);
    this.router.navigate(['/auth/login']);
  }

  navigateByRole(role: string): void {
    const routes: Record<string, string> = {
      Admin: '/admin/dashboard',
      RestaurantPartner: '/partner/dashboard',
      DeliveryAgent: '/delivery/dashboard',
      Customer: '/'
    };
    this.router.navigate([routes[role] ?? '/']);
  }

  private loadUser(): User | null {
    const u = sessionStorage.getItem('user');
    return u ? JSON.parse(u) : null;
  }
}
