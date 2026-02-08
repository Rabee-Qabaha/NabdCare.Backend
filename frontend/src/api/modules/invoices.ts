import { api } from '@/api/apiClient';
import type { InvoiceDto, InvoiceListRequestDto, PaginatedResult } from '@/types/backend';

export const invoicesApi = {
  // ==========================================
  // 🔍 QUERIES
  // ==========================================

  /**
   * Get paginated invoices with filters
   * Uses the generated InvoiceListRequestDto for type safety
   */
  async getPaged(params: InvoiceListRequestDto) {
    const { data } = await api.get<PaginatedResult<InvoiceDto>>('/invoices', {
      params: params,
    });
    return data;
  },

  async getById(id: string) {
    const { data } = await api.get<InvoiceDto>(`/invoices/${id}`);
    return data;
  },

  async downloadPdf(id: string) {
    // Returns the raw blob for file download
    const { data } = await api.get<Blob>(`/invoices/${id}/pdf`, {
      responseType: 'blob',
    });
    return data;
  },

  async writeOff(id: string, payload: { reason: string }) {
    const { data } = await api.post(`/invoices/${id}/write-off`, payload);
    return data;
  },
};
