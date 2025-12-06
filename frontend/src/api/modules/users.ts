// src/api/modules/users.ts
import { api } from '@/api/apiClient';
import type {
  ChangePasswordRequestDto,
  CreateUserRequestDto,
  PaginatedResult,
  ResetPasswordRequestDto,
  UpdateUserRequestDto,
  UserResponseDto,
} from '@/types/backend';
import { type AxiosResponse } from 'axios';

export interface UsersPagedParams {
  cursor?: string | null;
  limit?: number;
  descending?: boolean;
  includeDeleted?: boolean;
  search?: string;
  clinicId?: string;
}

const defaultPaging = { limit: 20, descending: true };

export const usersApi = {
  getPaged(params?: UsersPagedParams): Promise<AxiosResponse<PaginatedResult<UserResponseDto>>> {
    const query = {
      Limit: params?.limit ?? defaultPaging.limit,
      Descending: params?.descending ?? defaultPaging.descending,
      Cursor: params?.cursor ?? null,
      IncludeDeleted: params?.includeDeleted ?? false,
      Search: params?.search,
      ClinicId: params?.clinicId,
    };

    return api.get('/users/paged', { params: query });
  },

  getByClinicPaged(
    clinicId: string,
    params?: UsersPagedParams,
  ): Promise<AxiosResponse<PaginatedResult<UserResponseDto>>> {
    const query = {
      Limit: params?.limit ?? defaultPaging.limit,
      Descending: params?.descending ?? defaultPaging.descending,
      Cursor: params?.cursor ?? null,
      IncludeDeleted: params?.includeDeleted ?? false,
      Search: params?.search,
    };

    return api.get(`/users/clinic/${clinicId}/paged`, { params: query });
  },

  getById(id: string): Promise<AxiosResponse<UserResponseDto>> {
    return api.get(`/users/${id}`);
  },

  getMe(): Promise<AxiosResponse<UserResponseDto>> {
    return api.get('/users/me');
  },

  checkEmailExists(
    email: string,
  ): Promise<AxiosResponse<{ exists: boolean; isDeleted: boolean; userId: string | null }>> {
    return api.get('/users/check-email', { params: { email } });
  },

  restore(id: string): Promise<AxiosResponse<UserResponseDto>> {
    return api.put(`/users/${id}/restore`);
  },

  create(payload: CreateUserRequestDto): Promise<AxiosResponse<UserResponseDto>> {
    return api.post('/users', payload);
  },

  update(id: string, payload: UpdateUserRequestDto): Promise<AxiosResponse<UserResponseDto>> {
    return api.put(`/users/${id}`, payload);
  },

  updateRole(id: string, roleId: string): Promise<AxiosResponse<UserResponseDto>> {
    return api.put(`/users/${id}/role`, { roleId });
  },

  activate(id: string): Promise<AxiosResponse<UserResponseDto>> {
    return api.put(`/users/${id}/activate`);
  },

  deactivate(id: string): Promise<AxiosResponse<UserResponseDto>> {
    return api.put(`/users/${id}/deactivate`);
  },

  delete(id: string): Promise<AxiosResponse<void>> {
    return api.delete(`/users/${id}`);
  },

  hardDelete(id: string): Promise<AxiosResponse<void>> {
    return api.delete(`/users/${id}/permanent`);
  },

  changePassword(id: string, payload: ChangePasswordRequestDto): Promise<AxiosResponse<void>> {
    return api.post(`/users/${id}/change-password`, payload);
  },

  resetPassword(id: string, payload: ResetPasswordRequestDto): Promise<AxiosResponse<void>> {
    return api.post(`/users/${id}/reset-password`, payload);
  },
};
