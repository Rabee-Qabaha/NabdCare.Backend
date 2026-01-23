// src/api/modules/clinics.ts
import { api } from '@/api/apiClient';
import type {
  ClinicDashboardStatsDto,
  ClinicFilterRequestDto,
  ClinicResponseDto,
  CreateClinicRequestDto,
  PaginatedResult,
  UpdateClinicRequestDto,
  UpdateClinicStatusDto,
} from '@/types/backend';

export const clinicsApi = {
  // ==========================================
  // 🔍 QUERIES
  // ==========================================

  async getAllPaged(params: ClinicFilterRequestDto) {
    const { data } = await api.get<PaginatedResult<ClinicResponseDto>>('/clinics', {
      params: params,
    });
    return data;
  },

  async getById(id: string) {
    const { data } = await api.get<ClinicResponseDto>(`/clinics/${id}`);
    return data;
  },

  // ✅ NEW: Dashboard Analytics
  // Fetches the aggregated stats for the clinic profile header & overview tab
  async getStats(id: string) {
    const { data } = await api.get<ClinicDashboardStatsDto>(`/clinics/${id}/stats`);
    return data;
  },

  // ==========================================
  // ⚡ COMMANDS
  // ==========================================
  async create(dto: CreateClinicRequestDto) {
    const { data } = await api.post<ClinicResponseDto>('/clinics', dto);
    return data;
  },

  async update(id: string, dto: UpdateClinicRequestDto) {
    const { data } = await api.put<ClinicResponseDto>(`/clinics/${id}`, dto);
    return data;
  },

  async updateStatus(id: string, dto: UpdateClinicStatusDto) {
    const { data } = await api.put<ClinicResponseDto>(`/clinics/${id}/status`, dto);
    return data;
  },

  async activate(id: string) {
    const { data } = await api.put<ClinicResponseDto>(`/clinics/${id}/activate`);
    return data;
  },

  async suspend(id: string) {
    const { data } = await api.put<ClinicResponseDto>(`/clinics/${id}/suspend`);
    return data;
  },

  async restore(id: string) {
    const { data } = await api.put<{ message: string }>(`/clinics/${id}/restore`);
    return data;
  },

  async delete(id: string) {
    return api.delete<void>(`/clinics/${id}`);
  },

  async hardDelete(id: string) {
    return api.delete<void>(`/clinics/${id}/permanent`);
  },
};