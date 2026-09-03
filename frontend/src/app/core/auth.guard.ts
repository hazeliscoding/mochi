import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

/** Dashboard routes need a session; anonymous visitors go to login or first-run setup. */
export const authGuard: CanActivateFn = async () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const s = await auth.ensureStatus();
  if (s.needsSetup) return router.parseUrl('/setup');
  if (!s.authenticated) return router.parseUrl('/login');
  return true;
};

/** Login is for existing installs with no session. */
export const loginGuard: CanActivateFn = async () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const s = await auth.ensureStatus();
  if (s.needsSetup) return router.parseUrl('/setup');
  if (s.authenticated) return router.parseUrl('/overview');
  return true;
};

/** Setup only exists while the server has zero accounts. */
export const setupGuard: CanActivateFn = async () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const s = await auth.ensureStatus();
  if (s.needsSetup) return true;
  return router.parseUrl(s.authenticated ? '/overview' : '/login');
};
