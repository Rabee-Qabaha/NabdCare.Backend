// src/api/modules/roles.ts
import { api } from '@/api/apiClient';
import type {
  CloneRoleRequestDto,
  CreateRoleRequestDto,
  PermissionResponseDto,
  RoleResponseDto,
  UpdateRoleRequestDto,
} from '@/types/backend';

export const rolesApi = {
  // -----------------------------------------
  // QUERIES
  // -----------------------------------------

  getAll(iparams: { includeDeleted?: boolean; clinicId?: string | null }) {
    return api.get<RoleResponseDto[]>('/roles', {
      params: iparams,
    });
  },

  getSystem() {
    return api.get<RoleResponseDto[]>('/roles/system');
  },

  getTemplates() {
    return api.get<RoleResponseDto[]>('/roles/templates');
  },

  getByClinic(clinicId: string) {
    return api.get<RoleResponseDto[]>(`/roles/clinic/${clinicId}`);
  },

  getById(id: string) {
    return api.get<RoleResponseDto>(`/roles/${id}`);
  },

  // -----------------------------------------
  // COMMANDS
  // -----------------------------------------

  create(data: CreateRoleRequestDto) {
    return api.post<RoleResponseDto>('/roles', data);
  },

  clone(templateRoleId: string, data: CloneRoleRequestDto) {
    return api.post<RoleResponseDto>(`/roles/${templateRoleId}/clone`, data);
  },

  update(id: string, data: UpdateRoleRequestDto) {
    return api.put<RoleResponseDto>(`/roles/${id}`, data);
  },

  softDelete(id: string) {
    return api.delete<RoleResponseDto>(`/roles/${id}`);
  },

  hardDelete(id: string) {
    return api.delete<RoleResponseDto>(`/roles/${id}/permanent`);
  },

  restore(id: string) {
    return api.post<RoleResponseDto>(`/roles/${id}/restore`);
  },

  // -----------------------------------------
  // PERMISSIONS
  // -----------------------------------------

  getPermissions(roleId: string) {
    // Note: Ensure backend returns PermissionResponseDto[].
    // If backend returns string[] (IDs only), this type should be string[].
    return api.get<PermissionResponseDto[]>(`/roles/${roleId}/permissions`);
  },

  assignPermission(roleId: string, permissionId: string) {
    return api.post(`/roles/${roleId}/permissions/${permissionId}`);
  },

  removePermission(roleId: string, permissionId: string) {
    return api.delete(`/roles/${roleId}/permissions/${permissionId}`);
  },
};
