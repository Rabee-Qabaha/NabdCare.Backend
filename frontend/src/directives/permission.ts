// src/directives/permission.ts
import { usePermission } from '@/composables/usePermission';
import { useAuthStore } from '@/stores/authStore';
import { watchEffect } from 'vue';

/**
 * Permission directive
 * Handles showing/hiding elements based on user permissions.
 *
 * Usage:
 *   <Button v-permission="'Users.Edit'" />
 *   <div v-permission="['Users.Create', 'Users.Edit']">
 */
export const permissionDirective = {
  mounted(el: HTMLElement, binding: any) {
    const authStore = useAuthStore();
    const { can, canAny } = usePermission();

    /**
     * Evaluate element visibility based on permissions.
     */
    const evaluate = () => {
      const value = binding.value;
      const required = Array.isArray(value) ? value : [value];
      const allowed = required.length === 1 ? can(required[0]) : canAny(required);

      el.style.display = allowed ? '' : 'none';
    };

    /**
     * Automatically re-run when:
     * - permissionsVersion changes
     * - permissions array changes
     * - user changes
     */
    watchEffect(() => {
      if (!authStore.isPermissionsLoaded) {
        el.style.display = 'none';
        return;
      }

      evaluate();
    });
  },
};
