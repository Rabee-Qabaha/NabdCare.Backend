import { api } from '@/api/apiClient';
import type { PaginationRequestDto, PaginatedResult } from '@/types/backend';
import type {
  PermissionResponseDto,
  CreatePermissionDto,
  UpdatePermissionDto,
  AssignPermissionToUserDto,
} from '@/types/backend';

export const permissionsApi = {
  async getAllPaged(params: PaginationRequestDto) {
    const { data } = await api.get<PaginatedResult<PermissionResponseDto>>('/permissions/paged', {
      params,
    });
    return data;
  },

  async getAll() {
    const { data } = await api.get<PermissionResponseDto[]>('/permissions');
    return data;
  },

  async getGrouped() {
    const { data } = await api.get<Record<string, PermissionResponseDto[]>>('/permissions/grouped');
    return data;
  },

  async getById(id: string) {
    const { data } = await api.get<PermissionResponseDto>(`/permissions/${id}`);
    return data;
  },

  async getMine() {
    const { data } = await api.get<{
      userId: string;
      roleId: string;
      permissions: string[];
      permissionsVersion: string;
    }>('/permissions/me');
    return data;
  },

  async getByUser(userId: string) {
    const { data } = await api.get<PermissionResponseDto[]>(`/permissions/user/${userId}`);
    return data;
  },

  async getByRole(roleId: string) {
    const { data } = await api.get<PermissionResponseDto[]>(`/permissions/role/${roleId}`);
    return data;
  },

  async refreshUser(userId: string) {
    const { data } = await api.post(`/permissions/refresh/${userId}`);
    return data;
  },

  async create(dto: CreatePermissionDto) {
    const { data } = await api.post<PermissionResponseDto>('/permissions', dto);
    return data;
  },

  async update(id: string, dto: UpdatePermissionDto) {
    const { data } = await api.put<PermissionResponseDto>(`/permissions/${id}`, dto);
    return data;
  },

  async delete(id: string) {
    await api.delete(`/permissions/${id}`);
    return true;
  },

  async assignToUser(dto: AssignPermissionToUserDto) {
    const { data } = await api.post('/permissions/assign-user', dto);
    return data;
  },

  async removeFromUser(userId: string, permissionId: string) {
    const { data } = await api.delete(`/permissions/user/${userId}/permission/${permissionId}`);
    return data;
  },
};
