import { api } from '@/api/apiClient';
import type {
  BranchResponseDto,
  CreateBranchRequestDto,
  UpdateBranchRequestDto,
} from '@/types/backend';

export const branchesApi = {
  /** 🔹 Get branches (SuperAdmin can filter by clinicId) */
  async getAll(params: { clinicId?: string } = {}) {
    const { data } = await api.get<BranchResponseDto[]>('/branches', { params });
    return data;
  },

  /** 🔹 Get branch by ID */
  async getById(id: string) {
    const { data } = await api.get<BranchResponseDto>(`/branches/${id}`);
    return data;
  },

  /** 🔹 Create branch */
  async create(payload: CreateBranchRequestDto) {
    const { data } = await api.post<BranchResponseDto>('/branches', payload);
    return data;
  },

  /** 🔹 Update branch */
  async update(id: string, payload: UpdateBranchRequestDto) {
    const { data } = await api.put<BranchResponseDto>(`/branches/${id}`, payload);
    return data;
  },

  // 🔹 Toggle Status (Open/Close)
  async toggleStatus(id: string) {
    const { data } = await api.patch<BranchResponseDto>(`/branches/${id}/toggle-status`);
    return data;
  },

  /** 🔹 Delete branch */
  async delete(id: string) {
    await api.delete(`/branches/${id}`);
  },
};
