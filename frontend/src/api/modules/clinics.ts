// src/api/modules/clinics.ts
import { api } from "@/api/apiClient";
import type {
  ClinicResponseDto,
  PaginatedResult,
  PaginationRequestDto,
  CreateClinicRequestDto,
  UpdateClinicRequestDto,
  UpdateClinicStatusDto,
} from "@/types/backend";

/**
 * Normalize pagination params to backend PascalCase.
 *
 * Backend expects:
 *  - Limit
 *  - Cursor
 *  - Descending
 *  - SortBy
 *  - Filter
 */
function mapPagination(params?: PaginationRequestDto) {
  if (!params) return { Limit: 50, Descending: true };

  return {
    Limit: params.limit ?? 50,
    Descending: params.descending ?? true,
    Cursor: params.cursor ?? undefined,
    SortBy: params.sortBy ?? undefined,
    Filter: params.filter ?? undefined,
  };
}

export const clinicsApi = {
  /** Get all clinics (paginated, SuperAdmin only) */
  async getAll(params?: PaginationRequestDto) {
    const query = mapPagination(params);
    const { data } = await api.get<PaginatedResult<ClinicResponseDto>>(
      "/clinics",
      { params: query }
    );
    return data;
  },

  /** Get all active clinics (SuperAdmin only) */
  async getActive(params: PaginationRequestDto) {
    const query = mapPagination(params);
    const { data } = await api.get("/clinics/active", { params: query });
    return data;
  },

  /** Search clinics (SuperAdmin only) */
  async search(queryStr: string, params: PaginationRequestDto) {
    const query = {
      ...mapPagination(params),
      query: queryStr,
    };

    const { data } = await api.get<PaginatedResult<ClinicResponseDto>>(
      "/clinics/search",
      { params: query }
    );

    return data;
  },

  /** Get the clinic of the current user */
  async getMyClinic() {
    const { data } = await api.get<ClinicResponseDto>("/clinics/me");
    return data;
  },

  /** Get a clinic by ID */
  async getById(id: string) {
    const { data } = await api.get<ClinicResponseDto>(`/clinics/${id}`);
    return data;
  },

  /** Get statistics for a clinic */
  async getStats(id: string) {
    const { data } = await api.get(`/clinics/${id}/stats`);
    return data;
  },

  /** Create a clinic */
  async create(dto: CreateClinicRequestDto) {
    const { data } = await api.post("/clinics", dto);
    return data;
  },

  /** Update a clinic */
  async update(id: string, dto: UpdateClinicRequestDto) {
    const { data } = await api.put(`/clinics/${id}`, dto);
    return data;
  },

  /** Update clinic status */
  async updateStatus(id: string, dto: UpdateClinicStatusDto) {
    const { data } = await api.put(`/clinics/${id}/status`, dto);
    return data;
  },

  /** Activate clinic */
  async activate(id: string) {
    const { data } = await api.put(`/clinics/${id}/activate`);
    return data;
  },

  /** Suspend clinic */
  async suspend(id: string) {
    const { data } = await api.put(`/clinics/${id}/suspend`);
    return data;
  },

  /** Soft delete clinic */
  async softDelete(id: string) {
    const { data } = await api.delete(`/clinics/${id}`);
    return data;
  },

  /** Hard delete clinic */
  async hardDelete(id: string) {
    const { data } = await api.delete(`/clinics/${id}/permanent`);
    return data;
  },
};