import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, switchMap } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface ReviewRequest { status: string; rejectionReason?: string; }
export interface AdminUser { id: string; fullName: string; email: string; mobile: string; isActive: boolean; createdAt: string; }

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly catalogBase = `${environment.apiUrl}/gateway/catalog/admin/approvals`;
  private readonly authBase    = `${environment.apiUrl}/gateway/auth/admin/users`;
  private readonly adminBase   = `${environment.apiUrl}/gateway/admin`;

  // BehaviorSubjects for reactive refresh
  private refreshRestaurantAppsSubject = new BehaviorSubject<string | undefined>(undefined);
  private refreshAgentAppsSubject      = new BehaviorSubject<string | undefined>(undefined);
  private refreshUsersSubject          = new BehaviorSubject<void>(undefined);

  // Auto-refreshing observables
  restaurantApplications$: Observable<any[]> = this.refreshRestaurantAppsSubject.pipe(
    switchMap(status => this.getRestaurantApplications(status))
  );

  agentApplications$: Observable<any[]> = this.refreshAgentAppsSubject.pipe(
    switchMap(status => this.getAgentApplications(status))
  );

  customers$: Observable<AdminUser[]> = this.refreshUsersSubject.pipe(
    switchMap(() => this.getCustomers())
  );

  constructor(private http: HttpClient) {}

  // ── Approvals ──────────────────────────────────────────────────────────────

  getRestaurantApplications(status?: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.catalogBase}/restaurants`, { params: status ? { status } : {} });
  }

  refreshRestaurantApplications(status?: string): void {
    this.refreshRestaurantAppsSubject.next(status);
  }

  reviewRestaurantApplication(id: string, req: ReviewRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.catalogBase}/restaurants/${id}/review`, req);
  }

  deleteRestaurant(restaurantId: string): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.catalogBase}/restaurants/${restaurantId}`);
  }

  getAgentApplications(status?: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.catalogBase}/delivery-agents`, { params: status ? { status } : {} });
  }

  refreshAgentApplications(status?: string): void {
    this.refreshAgentAppsSubject.next(status);
  }

  reviewAgentApplication(id: string, req: ReviewRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.catalogBase}/delivery-agents/${id}/review`, req);
  }

  // ── Users ──────────────────────────────────────────────────────────────────

  getCustomers(): Observable<AdminUser[]> {
    return this.http.get<AdminUser[]>(`${this.authBase}/customers`);
  }

  getDeliveryAgents(): Observable<AdminUser[]> {
    return this.http.get<AdminUser[]>(`${this.authBase}/delivery-agents`);
  }

  getApprovedDeliveryAgents(): Observable<{ id: string; fullName: string; mobile: string }[]> {
    return this.http.get<{ id: string; fullName: string; mobile: string }[]>(`${this.authBase}/delivery-agents/approved`);
  }

  getRestaurantPartners(): Observable<AdminUser[]> {
    return this.http.get<AdminUser[]>(`${this.authBase}/restaurant-partners`);
  }

  deleteUser(userId: string): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.authBase}/${userId}`);
  }

  toggleActive(userId: string): Observable<{ message: string; isActive: boolean }> {
    return this.http.patch<{ message: string; isActive: boolean }>(`${this.authBase}/${userId}/toggle-active`, {});
  }

  refreshUsers(): void {
    this.refreshUsersSubject.next();
  }

  // ── Dashboard & Reports ────────────────────────────────────────────────────

  getDashboard(): Observable<any> {
    return this.http.get<any>(`${this.adminBase}/dashboard`);
  }

  getSalesReport(from: string, to: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.adminBase}/reports/sales`, { params: { from, to } });
  }

  getPartnerReport(): Observable<any[]> {
    return this.http.get<any[]>(`${this.adminBase}/reports/partners`);
  }

  getRevenueReport(from: string, to: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.adminBase}/reports/revenue`, { params: { from, to } });
  }
}
