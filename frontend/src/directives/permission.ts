// src/directives/permission.ts
import { useAuthStore } from '@/stores/authStore';
import { hasAnyPermission } from '@/utils/permissions';
import { watch } from 'vue';

/**
 * v-permission directive
 * -----------------------
 * Usage:
 *   <Button v-permission="'Users.Edit'" />
 *   <div v-permission="['Users.Create', 'Users.Edit']">...</div>
 */
export const permissionDirective = {
  mounted(el: HTMLElement, binding: any) {
    const store = useAuthStore();

    const evaluate = () => {
      const perms = Array.isArray(binding.value) ? binding.value : [binding.value];
      const visible = hasAnyPermission(...perms);
      el.style.display = visible ? '' : 'none';
    };

    // 🧠 If permissions aren't ready yet, wait
    if (!store.isPermissionsLoaded) {
      const stop = watch(
        () => store.isPermissionsLoaded,
        (loaded) => {
          if (loaded) {
            evaluate();
            stop();
          }
        },
        { immediate: true },
      );
    } else {
      evaluate();
    }
  },
};
