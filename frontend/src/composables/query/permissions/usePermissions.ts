import { permissionsApi } from '@/api/modules/permissions';
import { useErrorHandler } from '@/composables/errorHandling/useErrorHandler';
import { useToastService } from '@/composables/useToastService';
import type {
  AssignPermissionToUserDto,
  CreatePermissionDto,
  PaginatedResult,
  PaginationRequestDto,
  PermissionResponseDto,
  UpdatePermissionDto,
} from '@/types/backend';
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/vue-query';
import { computed, unref, type Ref } from 'vue';

/* 🔹 Query keys */
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

export function useAllPermissionsPaged(params: Ref<PaginationRequestDto> | PaginationRequestDto) {
  return useQuery<PaginatedResult<PermissionResponseDto>>({
    queryKey: computed(() => permissionKeys.paged(unref(params))),
    queryFn: () => permissionsApi.getAllPaged(unref(params)),
    placeholderData: keepPreviousData, // Prevents flickering
    staleTime: 1000 * 60 * 5, // 5 minutes
  });
}

export function useAllPermissions() {
  return useQuery<PermissionResponseDto[]>({
    queryKey: permissionKeys.all,
    queryFn: () => permissionsApi.getAll(),
    staleTime: 1000 * 60 * 5,
  });
}

export function useGroupedPermissions() {
  return useQuery({
    queryKey: permissionKeys.grouped,
    queryFn: () => permissionsApi.getGrouped(),
    staleTime: 1000 * 60 * 5,
  });
}

export function usePermissionById(id: Ref<string> | string) {
  return useQuery({
    queryKey: computed(() => permissionKeys.byId(unref(id))),
    queryFn: () => permissionsApi.getById(unref(id)),
    enabled: computed(() => !!unref(id)),
  });
}

export function useMyPermissions() {
  return useQuery({
    queryKey: permissionKeys.me,
    queryFn: () => permissionsApi.getMine(),
  });
}

export function useUserPermissions(userId: Ref<string | null> | string) {
  return useQuery({
    queryKey: computed(() => permissionKeys.user(unref(userId) || '')),
    queryFn: () => permissionsApi.getByUser(unref(userId)!),
    enabled: computed(() => !!unref(userId)),
  });
}

export function useRolePermissions(roleId: Ref<string | null> | string) {
  return useQuery({
    queryKey: computed(() => permissionKeys.role(unref(roleId) || '')),
    queryFn: () => permissionsApi.getByRole(unref(roleId)!),
    enabled: computed(() => !!unref(roleId)),
  });
}

/* ✅ Mutations */

export function useCreatePermission() {
  const toast = useToastService();
  const queryClient = useQueryClient();
  const { handleErrorAndNotify } = useErrorHandler();

  return useMutation({
    mutationFn: (dto: CreatePermissionDto) => permissionsApi.create(dto),
    onSuccess: () => {
      toast.success('Permission created successfully!');
      queryClient.invalidateQueries({ queryKey: permissionKeys.all });
    },
    onError: (err) => handleErrorAndNotify(err),
  });
}

export function useUpdatePermission() {
  const toast = useToastService();
  const queryClient = useQueryClient();
  const { handleErrorAndNotify } = useErrorHandler();

  return useMutation({
    mutationFn: (data: { id: string; dto: UpdatePermissionDto }) =>
      permissionsApi.update(data.id, data.dto),
    onSuccess: (_, variables) => {
      toast.success('Permission updated successfully!');
      queryClient.invalidateQueries({ queryKey: permissionKeys.all });
      queryClient.invalidateQueries({ queryKey: permissionKeys.byId(variables.id) });
    },
    onError: (err) => handleErrorAndNotify(err),
  });
}

export function useDeletePermission() {
  const toast = useToastService();
  const queryClient = useQueryClient();
  const { handleErrorAndNotify } = useErrorHandler();

  return useMutation({
    mutationFn: (id: string) => permissionsApi.delete(id),
    onSuccess: () => {
      toast.success('Permission deleted successfully!');
      queryClient.invalidateQueries({ queryKey: permissionKeys.all });
    },
    onError: (err) => handleErrorAndNotify(err),
  });
}

export function useAssignPermissionToUser() {
  const toast = useToastService();
  const queryClient = useQueryClient();
  const { handleErrorAndNotify } = useErrorHandler();

  return useMutation({
    mutationFn: (dto: AssignPermissionToUserDto) => permissionsApi.assignToUser(dto),
    onSuccess: (_, variables) => {
      toast.success('Permission assigned successfully!');
      queryClient.invalidateQueries({ queryKey: permissionKeys.user(variables.userId) });
    },
    onError: (err) => handleErrorAndNotify(err),
  });
}

export function useRemovePermissionFromUser() {
  const toast = useToastService();
  const queryClient = useQueryClient();
  const { handleErrorAndNotify } = useErrorHandler();

  return useMutation({
    mutationFn: (data: { userId: string; permissionId: string }) =>
      permissionsApi.removeFromUser(data.userId, data.permissionId),
    onSuccess: (_, variables) => {
      toast.success('Permission removed successfully!');
      queryClient.invalidateQueries({ queryKey: permissionKeys.user(variables.userId) });
    },
    onError: (err) => handleErrorAndNotify(err),
  });
}

export function useRefreshUserPermissions() {
  const toast = useToastService();
  const queryClient = useQueryClient();
  const { handleErrorAndNotify } = useErrorHandler();

  return useMutation({
    mutationFn: (userId: string) => permissionsApi.refreshUser(userId),
    onSuccess: () => {
      toast.success('Permissions cache refreshed successfully!');
      queryClient.invalidateQueries({ queryKey: permissionKeys.all });
    },
    onError: (err) => handleErrorAndNotify(err),
  });
}
export function usePermissions() {
  const { data: myPermissionsData } = useMyPermissions();

  // The API returns { userId, roleId, permissions: string[], ... }
  const myPermissions = computed(() => myPermissionsData.value?.permissions || []);

  const can = (permission: string) => {
    return myPermissions.value.includes(permission);
  };

  const canAny = (permissions: string[]) => {
    return permissions.some((p) => myPermissions.value.includes(p));
  };

  const canAll = (permissions: string[]) => {
    return permissions.every((p) => myPermissions.value.includes(p));
  };

  return {
    can,
    canAny,
    canAll,
    permissions: myPermissions,
  };
}
