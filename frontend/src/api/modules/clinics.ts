import { api } from "@/api/apiClient";
import type { PaginatedResult, PaginationRequestDto } from "@/types/backend";
import type {
  ClinicResponseDto,
  CreateClinicRequestDto,
  UpdateClinicRequestDto,
  UpdateClinicStatusDto,
} from "@/types/backend";

/**
 * Clinics API Module
 * Location: src/api/modules/clinics.ts
 *
 * ✅ FIXED: Properly pass pagination parameters
 *
 * Author: Rabee Qabaha
 * Updated: 2025-11-02
 */

export const clinicsApi = {
  /**
   * Get all clinics with pagination
   * ✅ FIXED: Properly pass params as query string
   */
  async getAll(params?: PaginationRequestDto) {
    // ✅ FIX: Merge params with defaults
    const queryParams = {
      Limit: params?.limit ?? 100,
      Descending: params?.descending ?? false,
      ...params,
    };

    console.log("📍 Clinics API - getAll params:", queryParams);

    const { data } = await api.get<PaginatedResult<ClinicResponseDto>>(
      "/clinics",
      { params: queryParams } // ✅ FIXED: Explicitly pass params
    );
    return data;
  },

  async getActive(params: PaginationRequestDto) {
    const queryParams = {
      Limit: params?.limit ?? 100,
      Descending: params?.descending ?? false,
      ...params,
    };

    const { data } = await api.get<PaginatedResult<ClinicResponseDto>>(
      "/clinics/active",
      { params: queryParams }
    );
    return data;
  },

  async search(query: string, params: PaginationRequestDto) {
    const queryParams = {
      Limit: params?.limit ?? 100,
      Descending: params?.descending ?? false,
      ...params,
      query,
    };

    const { data } = await api.get<PaginatedResult<ClinicResponseDto>>(
      "/clinics/search",
      { params: queryParams }
    );
    return data;
  },

  async getMyClinic() {
    const { data } = await api.get<ClinicResponseDto>("/clinics/me");
    return data;
  },

  async getById(id: string) {
    const { data } = await api.get<ClinicResponseDto>(`/clinics/${id}`);
    return data;
  },

  async getStats(id: string) {
    const { data } = await api.get(`/clinics/${id}/stats`);
    return data;
  },

  async create(dto: CreateClinicRequestDto) {
    const { data } = await api.post<ClinicResponseDto>("/clinics", dto);
    return data;
  },

  async update(id: string, dto: UpdateClinicRequestDto) {
    const { data } = await api.put<ClinicResponseDto>(`/clinics/${id}`, dto);
    return data;
  },

  async updateStatus(id: string, dto: UpdateClinicStatusDto) {
    const { data } = await api.put<ClinicResponseDto>(
      `/clinics/${id}/status`,
      dto
    );
    return data;
  },

  async activate(id: string) {
    const { data } = await api.put(`/clinics/${id}/activate`);
    return data;
  },

  async suspend(id: string) {
    const { data } = await api.put(`/clinics/${id}/suspend`);
    return data;
  },

  async softDelete(id: string) {
    const { data } = await api.delete(`/clinics/${id}`);
    return data;
  },

  async hardDelete(id: string) {
    const { data } = await api.delete(`/clinics/${id}/permanent`);
    return data;
  },
};
