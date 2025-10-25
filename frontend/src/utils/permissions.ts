import { useAuthStore } from "@/stores/authStore";

/**
 * Check if current user has a specific permission
 */
export function hasPermission(permissionName: string): boolean {
  const authStore = useAuthStore();
  return authStore.hasPermission(permissionName);
}

/**
 * Check if user has ANY of the given permissions
 */
export function hasAnyPermission(...permissions: string[]): boolean {
  return permissions.some((p) => hasPermission(p));
}

/**
 * Check if user has ALL of the given permissions
 */
export function hasAllPermissions(...permissions: string[]): boolean {
  return permissions.every((p) => hasPermission(p));
}

/**
 * Vue directive for v-permission
 * Usage: <Button v-permission="'Users.Create'" label="Add User" />
 */
export const vPermission = {
  mounted(el: HTMLElement, binding: { value: string | string[] }) {
    const permissions = Array.isArray(binding.value)
      ? binding.value
      : [binding.value];

    if (!hasAnyPermission(...permissions)) {
      el.style.display = "none";
    }
  },
  updated(el: HTMLElement, binding: { value: string | string[] }) {
    const permissions = Array.isArray(binding.value)
      ? binding.value
      : [binding.value];

    if (!hasAnyPermission(...permissions)) {
      el.style.display = "none";
    } else {
      el.style.display = "";
    }
  },
};
