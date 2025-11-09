// src/composables/query/permissions/usePermissions.ts
import { permissionsApi } from '@/api/modules/permissions';
import { useMutationWithInvalidate } from '@/composables/query/helpers/useMutationWithInvalidate';
import { useQueryWithToasts } from '@/composables/query/helpers/useQueryWithToasts';
import type {
  AssignPermissionToUserDto,
  CreatePermissionDto,
  PaginatedResult,
  PaginationRequestDto,
  PermissionResponseDto,
  UpdatePermissionDto,
} from '@/types/backend';

/* 🔹 Query keys — fully type-safe */
export const permissionKeys = {
  all: ['permissions'] as const,
  paged: (params?: PaginationRequestDto) => ['permissions', 'paged', params] as const,
  grouped: ['permissions', 'grouped'] as const,
  byId: (id: string) => ['permissions', id] as const,
  user: (userId: string) => ['permissions', 'user', userId] as const,
  role: (roleId: string) => ['permissions', 'role', roleId] as const,
  me: ['permissions', 'me'] as const,
};

/* ✅ Queries */

export function useAllPermissionsPaged(params: PaginationRequestDto) {
  return useQueryWithToasts<PaginatedResult<PermissionResponseDto>>({
    queryKey: permissionKeys.paged(params),
    queryFn: () => permissionsApi.getAllPaged(params),
    successMessage: 'Permissions loaded successfully.',
    errorMessage: 'Failed to load permissions.',
  });
}

export function useAllPermissions() {
  return useQueryWithToasts<PermissionResponseDto[]>({
    queryKey: permissionKeys.all,
    queryFn: () => permissionsApi.getAll(),
    successMessage: 'Permissions loaded successfully.',
    errorMessage: 'Failed to load permissions.',
  });
}

export function useGroupedPermissions() {
  return useQueryWithToasts({
    queryKey: permissionKeys.grouped,
    queryFn: () => permissionsApi.getGrouped(),
    successMessage: 'Grouped permissions loaded successfully.',
    errorMessage: 'Failed to load grouped permissions.',
  });
}

export function usePermissionById(id: string) {
  return useQueryWithToasts({
    queryKey: permissionKeys.byId(id),
    queryFn: () => permissionsApi.getById(id),
    enabled: !!id,
    successMessage: 'Permission loaded successfully.',
    errorMessage: 'Failed to load permission.',
  });
}

export function useMyPermissions() {
  return useQueryWithToasts({
    queryKey: permissionKeys.me,
    queryFn: () => permissionsApi.getMine(),
    successMessage: "Fetched current user's permissions.",
    errorMessage: "Failed to fetch current user's permissions.",
  });
}

export function useUserPermissions(userId: string) {
  return useQueryWithToasts({
    queryKey: permissionKeys.user(userId),
    queryFn: () => permissionsApi.getByUser(userId),
    enabled: !!userId,
    successMessage: 'Fetched user permissions.',
    errorMessage: 'Failed to fetch user permissions.',
  });
}

export function useRolePermissions(roleId: string) {
  return useQueryWithToasts({
    queryKey: permissionKeys.role(roleId),
    queryFn: () => permissionsApi.getByRole(roleId),
    enabled: !!roleId,
    successMessage: 'Fetched role permissions.',
    errorMessage: 'Failed to fetch role permissions.',
  });
}

/* ✅ Mutations */

export function useCreatePermission() {
  return useMutationWithInvalidate({
    mutationKey: ['permissions', 'create'],
    mutationFn: (dto: CreatePermissionDto) => permissionsApi.create(dto),
    invalidateKeys: [permissionKeys.all],
    successMessage: 'Permission created successfully!',
    errorMessage: 'Failed to create permission.',
  });
}

export function useUpdatePermission() {
  return useMutationWithInvalidate({
    mutationKey: ['permissions', 'update'],
    mutationFn: (data: { id: string; dto: UpdatePermissionDto }) =>
      permissionsApi.update(data.id, data.dto),
    invalidateKeys: [(v) => permissionKeys.byId(v.id), permissionKeys.all],
    successMessage: 'Permission updated successfully!',
    errorMessage: 'Failed to update permission.',
  });
}

export function useDeletePermission() {
  return useMutationWithInvalidate({
    mutationKey: ['permissions', 'delete'],
    mutationFn: (id: string) => permissionsApi.delete(id),
    invalidateKeys: [permissionKeys.all],
    successMessage: 'Permission deleted successfully!',
    errorMessage: 'Failed to delete permission.',
  });
}

export function useAssignPermissionToUser() {
  return useMutationWithInvalidate({
    mutationKey: ['permissions', 'assign-user'],
    mutationFn: (dto: AssignPermissionToUserDto) => permissionsApi.assignToUser(dto),
    invalidateKeys: [(v) => permissionKeys.user(v.userId)],
    successMessage: 'Permission assigned successfully!',
    errorMessage: 'Failed to assign permission.',
  });
}

export function useRemovePermissionFromUser() {
  return useMutationWithInvalidate({
    mutationKey: ['permissions', 'remove-user'],
    mutationFn: (data: { userId: string; permissionId: string }) =>
      permissionsApi.removeFromUser(data.userId, data.permissionId),
    invalidateKeys: [(v) => permissionKeys.user(v.userId)],
    successMessage: 'Permission removed successfully!',
    errorMessage: 'Failed to remove permission.',
  });
}

export function useRefreshUserPermissions() {
  return useMutationWithInvalidate({
    mutationKey: ['permissions', 'refresh-user'],
    mutationFn: (userId: string) => permissionsApi.refreshUser(userId),
    invalidateKeys: [permissionKeys.all],
    successMessage: 'Permissions cache refreshed successfully!',
    errorMessage: 'Failed to refresh permissions cache.',
  });
}
