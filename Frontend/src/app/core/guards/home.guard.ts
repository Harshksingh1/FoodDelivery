import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const homeGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.role() === 'Admin') { router.navigate(['/admin/dashboard']); return false; }
  if (auth.role() === 'RestaurantPartner') { router.navigate(['/partner/dashboard']); return false; }
  if (auth.role() === 'DeliveryAgent') { router.navigate(['/delivery/dashboard']); return false; }
  return true;
};
