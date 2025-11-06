// src/api/modules/subscriptions.ts
import { api } from '@/api/apiClient';
import type { PaginatedResult } from '@/types/backend';
import type {
  SubscriptionResponseDto,
  CreateSubscriptionRequestDto,
  UpdateSubscriptionRequestDto,
} from '@/types/backend';
import type { SubscriptionStatus, SubscriptionType } from '@/types/backend';

export const subscriptionsApi = {
  /** 🔹 Get subscription by ID */
  async getById(id: string) {
    const { data } = await api.get<SubscriptionResponseDto>(`/subscriptions/${id}`);
    return data;
  },

  /** 🔹 Get active subscription for a clinic */
  async getActiveForClinic(clinicId: string) {
    const { data } = await api.get(`/subscriptions/clinic/${clinicId}/active`);
    return data;
  },

  /** 🔹 Get subscriptions for a clinic (paginated) */
  async getByClinicId(clinicId: string, params: Record<string, any> = {}) {
    const { data } = await api.get<PaginatedResult<SubscriptionResponseDto>>(
      `/subscriptions/clinic/${clinicId}`,
      { params },
    );
    return data;
  },

  /** 🔹 Get all subscriptions (SuperAdmin only, paginated) */
  async getAll(params: Record<string, any> = {}) {
    const { data } = await api.get<PaginatedResult<SubscriptionResponseDto>>('/subscriptions', {
      params,
    });
    return data;
  },

  /** 🔹 Create subscription */
  async create(payload: CreateSubscriptionRequestDto) {
    const { data } = await api.post<SubscriptionResponseDto>('/subscriptions', payload);
    return data;
  },

  /** 🔹 Update subscription */
  async update(id: string, payload: UpdateSubscriptionRequestDto) {
    const { data } = await api.put<SubscriptionResponseDto>(`/subscriptions/${id}`, payload);
    return data;
  },

  /** 🔹 Change subscription status */
  async changeStatus(id: string, newStatus: SubscriptionStatus) {
    const { data } = await api.patch(`/subscriptions/${id}/status`, newStatus);
    return data;
  },

  /** 🔹 Renew subscription (SuperAdmin only) */
  async renew(id: string, type: SubscriptionType) {
    const { data } = await api.post(`/subscriptions/${id}/renew`, null, {
      params: { type },
    });
    return data;
  },

  /** 🔹 Toggle auto-renew */
  async toggleAutoRenew(id: string, enable: boolean) {
    const { data } = await api.patch(`/subscriptions/${id}/auto-renew`, null, {
      params: { enable },
    });
    return data;
  },

  /** 🔹 Soft delete (cancel) */
  async cancel(id: string) {
    const { data } = await api.delete(`/subscriptions/${id}`);
    return data;
  },

  /** 🔹 Hard delete (permanent) */
  async hardDelete(id: string) {
    const { data } = await api.delete(`/subscriptions/${id}/hard`);
    return data;
  },
};
