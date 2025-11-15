// src/composables/query/roles/useRolePermissions.ts
import { useQuery } from "@tanstack/vue-query";
import { rolesApi } from "@/api/modules/roles";
import { useMutationWithInvalidate } from "@/composables/query/helpers/useMutationWithInvalidate";

export function useRolePermissions(roleId: string) {
  const query = useQuery({
    queryKey: ["roles", "permissions", roleId],
    queryFn: () => rolesApi.getPermissions(roleId),
    enabled: !!roleId,
    staleTime: 1000 * 60 * 5,
    placeholderData: (prev) => prev,
  });

  const assignPermission = useMutationWithInvalidate({
    mutationKey: ["role", "assign-permission"],
    mutationFn: (permissionId: string) => rolesApi.assignPermission(roleId, permissionId),
    invalidateKeys: [["roles", "permissions", roleId]],
    successMessage: "Permission assigned successfully.",
    errorMessage: "Failed to assign permission.",
  });

  const removePermission = useMutationWithInvalidate({
    mutationKey: ["role", "remove-permission"],
    mutationFn: (permissionId: string) => rolesApi.removePermission(roleId, permissionId),
    invalidateKeys: [["roles", "permissions", roleId]],
    successMessage: "Permission removed successfully.",
    errorMessage: "Failed to remove permission.",
  });

  return {
    permissions: query.data,
    isLoading: query.isLoading,
    error: query.error,
    assignPermission,
    removePermission,
  };
}