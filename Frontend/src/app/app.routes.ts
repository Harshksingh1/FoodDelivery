import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';

import { homeGuard } from './core/guards/home.guard';

export const routes: Routes = [
  { path: '', canActivate: [homeGuard], loadComponent: () => import('./features/home/home.component').then(m => m.HomeComponent) },
  // Auth
  { path: 'auth/login', loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent) },
  { path: 'auth/register', loadComponent: () => import('./features/auth/register/register.component').then(m => m.RegisterComponent) },
  { path: 'auth/verify-otp', loadComponent: () => import('./features/auth/verify-otp/verify-otp.component').then(m => m.VerifyOtpComponent) },
  { path: 'auth/forgot-password', loadComponent: () => import('./features/auth/forgot-password/forgot-password.component').then(m => m.ForgotPasswordComponent) },
  { path: 'auth/reset-password', loadComponent: () => import('./features/auth/reset-password/reset-password.component').then(m => m.ResetPasswordComponent) },

  // Public catalog
  { path: 'restaurants', loadComponent: () => import('./features/restaurants/restaurant-list/restaurant-list.component').then(m => m.RestaurantListComponent) },
  { path: 'restaurants/:id', loadComponent: () => import('./features/restaurants/restaurant-detail/restaurant-detail.component').then(m => m.RestaurantDetailComponent) },

  // Customer
  {
    path: 'customer',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Customer'] },
    children: [
      { path: 'cart', loadComponent: () => import('./features/customer/cart/cart.component').then(m => m.CartComponent) },
      { path: 'checkout', loadComponent: () => import('./features/customer/checkout/checkout.component').then(m => m.CheckoutComponent) },
      { path: 'orders', loadComponent: () => import('./features/customer/orders/orders.component').then(m => m.OrdersComponent) },
      { path: 'profile', loadComponent: () => import('./shared/components/profile/profile.component').then(m => m.ProfileComponent) },
    ]
  },

  // Partner
  {
    path: 'partner',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['RestaurantPartner'] },
    children: [
      { path: 'dashboard', loadComponent: () => import('./features/partner/dashboard/partner-dashboard.component').then(m => m.PartnerDashboardComponent) },
      { path: 'restaurants', loadComponent: () => import('./features/partner/restaurants/partner-restaurants.component').then(m => m.PartnerRestaurantsComponent) },
      { path: 'orders', loadComponent: () => import('./features/partner/orders/partner-orders.component').then(m => m.PartnerOrdersComponent) },
      { path: 'apply', loadComponent: () => import('./features/partner/apply/partner-apply.component').then(m => m.PartnerApplyComponent) },
      { path: 'profile', loadComponent: () => import('./shared/components/profile/profile.component').then(m => m.ProfileComponent) },
    ]
  },

  // Admin
  {
    path: 'admin',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Admin'] },
    children: [
      { path: 'dashboard', loadComponent: () => import('./features/admin/dashboard/admin-dashboard.component').then(m => m.AdminDashboardComponent) },
      { path: 'users', loadComponent: () => import('./features/admin/users/admin-users.component').then(m => m.AdminUsersComponent) },
      { path: 'approvals', loadComponent: () => import('./features/admin/approvals/admin-approvals.component').then(m => m.AdminApprovalsComponent) },
      { path: 'partner-report', loadComponent: () => import('./features/admin/partner-report/admin-partner-report.component').then(m => m.AdminPartnerReportComponent) },
      { path: 'revenue-report', loadComponent: () => import('./features/admin/revenue-report/admin-revenue-report.component').then(m => m.AdminRevenueReportComponent) },
      { path: 'profile', loadComponent: () => import('./shared/components/profile/profile.component').then(m => m.ProfileComponent) },
    ]
  },

  // Delivery
  {
    path: 'delivery',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['DeliveryAgent'] },
    children: [
      { path: 'dashboard', loadComponent: () => import('./features/delivery/dashboard/delivery-dashboard.component').then(m => m.DeliveryDashboardComponent) },
      { path: 'profile', loadComponent: () => import('./shared/components/profile/profile.component').then(m => m.ProfileComponent) },
    ]
  },

  { path: '**', redirectTo: '' }
];
