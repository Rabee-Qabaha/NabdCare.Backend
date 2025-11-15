// src/composables/query/roles/useRoleActions.ts
import { rolesApi } from "@/api/modules/roles";
import { useMutationWithInvalidate } from "@/composables/query/helpers/useMutationWithInvalidate";
import type {
  CreateRoleRequestDto,
  UpdateRoleRequestDto,
  CloneRoleRequestDto,
} from "@/types/backend";

export function useCreateRole() {
  return useMutationWithInvalidate({
    mutationKey: ["roles", "create"],
    mutationFn: (data: CreateRoleRequestDto) => rolesApi.create(data),
    invalidateKeys: [["roles"]],
    successMessage: "Role created successfully",
    errorMessage: "Failed to create role",
  });
}

export function useUpdateRole() {
  return useMutationWithInvalidate({
    mutationKey: ["roles", "update"],
    mutationFn: ({ id, data }: { id: string; data: UpdateRoleRequestDto }) =>
      rolesApi.update(id, data),
    invalidateKeys: [["roles"]],
    successMessage: "Role updated successfully",
    errorMessage: "Failed to update role",
  });
}

export function useDeleteRole() {
  return useMutationWithInvalidate({
    mutationKey: ["roles", "delete"],
    mutationFn: (id: string) => rolesApi.delete(id),
    invalidateKeys: [["roles"]],
    successMessage: "Role deleted",
    errorMessage: "Failed to delete role",
  });
}

export function useRestoreRole() {
  return useMutationWithInvalidate({
    mutationKey: ["roles", "restore"],
    mutationFn: (id: string) => rolesApi.restore(id),
    invalidateKeys: [["roles"]],
    successMessage: "Role restored successfully",
    errorMessage: "Failed to restore role",
  });
}

export function useCloneRole() {
  return useMutationWithInvalidate({
    mutationKey: ["roles", "clone"],
    mutationFn: (payload: CloneRoleRequestDto & { templateRoleId: string }) =>
      rolesApi.clone(payload.templateRoleId, payload),
    invalidateKeys: [["roles"]],
    successMessage: "Role cloned successfully",
    errorMessage: "Failed to clone role",
  });
}