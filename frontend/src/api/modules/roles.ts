import { api } from '@/api/apiClient';
import type {
  CloneRoleRequestDto,
  CreateRoleRequestDto,
  PaginatedResult,
  RoleFilterRequestDto,
  RoleResponseDto,
  UpdateRoleRequestDto,
} from '@/types/backend';
import type { AxiosResponse } from 'axios';

export const rolesApi = {
  // -----------------------------------------
  // QUERIES
  // -----------------------------------------

  // 1. For Dropdowns (List) - Uses Unified Filter
  getAll(params?: RoleFilterRequestDto): Promise<AxiosResponse<RoleResponseDto[]>> {
    return api.get('/roles', { params });
  },

  // 2. For Grid (Paged) - Uses Unified Filter
  getPaged(
    params?: RoleFilterRequestDto,
  ): Promise<AxiosResponse<PaginatedResult<RoleResponseDto>>> {
    return api.get('/roles/paged', { params: { descending: true, ...params } });
  },

  getSystem() {
    return api.get<RoleResponseDto[]>('/roles/system');
  },

  getTemplates() {
    return api.get<RoleResponseDto[]>('/roles/templates');
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
    return api.get<string[]>(`/roles/${roleId}/permissions`);
  },

  assignPermission(roleId: string, permissionId: string) {
    return api.post(`/roles/${roleId}/permissions/${permissionId}`);
  },

  removePermission(roleId: string, permissionId: string) {
    return api.delete(`/roles/${roleId}/permissions/${permissionId}`);
  },
};
