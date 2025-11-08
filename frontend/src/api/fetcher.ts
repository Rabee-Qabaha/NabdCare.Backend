// src/api/fetcher.ts
import { api } from '@/api/apiClient';

/**
 * Generic fetcher function used by Vue Query hooks
 * Supports pagination, filters, etc.
 */
export async function fetcher<T>(endpoint: string, params?: Record<string, any>): Promise<T> {
  const { data } = await api.get<T>(endpoint, { params });
  return data;
}
