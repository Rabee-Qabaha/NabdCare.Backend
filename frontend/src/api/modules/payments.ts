import { api } from '@/api/apiClient';
import type {
  BatchPaymentRequestDto,
  CreatePaymentRequestDto,
  PaginatedResult,
  PaymentDto,
} from '@/types/backend';

export const paymentsApi = {
  // ==========================================
  // 🔍 QUERIES
  // ==========================================

  /**
   * Get paginated payments for a specific clinic
   * URL: GET /api/payments/clinic/{clinicId}?page=1&pageSize=10
   */
  async getPaged(params: any) {
    // Extract clinicId from params as it's a path parameter in the backend
    const { clinicId, ...queryParams } = params;

    if (!clinicId) {
      throw new Error('Clinic ID is required for fetching payments');
    }

    const { data } = await api.get<PaginatedResult<PaymentDto>>(`/payments/clinic/${clinicId}`, {
      params: queryParams,
    });
    return data;
  },

  async getById(id: string) {
    const { data } = await api.get<PaymentDto>(`/payments/${id}`);
    return data;
  },

  // ==========================================
  // ⚡ COMMANDS
  // ==========================================

  async create(dto: CreatePaymentRequestDto) {
    const { data } = await api.post<PaymentDto>('/payments', dto);
    return data;
  },

  async createBatch(dto: BatchPaymentRequestDto) {
    const { data } = await api.post<PaymentDto[]>('/payments/batch', dto);
    return data;
  },

  async update(id: string, dto: any) {
    // Assuming partial update or specific DTO. Using any for now as UpdatePaymentRequestDto wasn't explicitly mentioned/found.
    const { data } = await api.put<PaymentDto>(`/payments/${id}`, dto);
    return data;
  },

  async updateCheque(id: string, dto: any) {
    const { data } = await api.put<PaymentDto>(`/payments/${id}/cheque`, dto);
    return data;
  },

  async delete(id: string) {
    return api.delete<void>(`/payments/${id}`);
  },
};
