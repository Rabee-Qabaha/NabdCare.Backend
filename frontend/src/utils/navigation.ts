// src/utils/navigation.ts
import { useAuthStore } from '@/stores/authStore';

/**
 * Returns the default dashboard route for the logged-in user.
 * This keeps routing logic consistent across login, app init, etc.
 */
export function getDefaultDashboardRoute() {
  const authStore = useAuthStore();

  console.log('🧭 getDefaultDashboardRoute called');
  console.log('   isLoggedIn:', authStore.isLoggedIn);
  console.log('   isSuperAdmin:', authStore.isSuperAdmin);

  if (!authStore.isLoggedIn) {
    console.log('➡️ Redirecting to login');
    return { name: 'login' };
  }

  if (authStore.isSuperAdmin) {
    console.log('➡️ Redirecting to SuperAdmin Dashboard');
    return { name: 'superadmin-dashboard' };
  }

  console.log('➡️ Redirecting to Client Dashboard');
  return { name: 'client-dashboard' };
}
