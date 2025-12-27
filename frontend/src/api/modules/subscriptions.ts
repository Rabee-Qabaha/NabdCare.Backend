import { api } from '@/api/apiClient';
import type {
  CreateSubscriptionRequestDto,
  PaginatedResult,
  PlanDefinition,
  SubscriptionResponseDto,
  SubscriptionStatus,
  SubscriptionType,
  UpdateSubscriptionRequestDto,
} from '@/types/backend';

export const subscriptionsApi = {
  // =========================================================================
  // 🔍 READ OPERATIONS
  // =========================================================================

  /** * 🆕 Get available subscription plans (Product Catalog)
   */
  async getPlans() {
    const { data } = await api.get<PlanDefinition[]>('/subscriptions/plans');
    return data;
  },

  /** * 🔹 Get subscription by ID
   */
  async getById(id: string) {
    const { data } = await api.get<SubscriptionResponseDto>(`/subscriptions/${id}`);
    return data;
  },

  /** * 🔹 Get active subscription for a clinic
   * Returns details + expiration status
   * 🛑 UPDATED: Return type now includes '| null' to correctly reflect backend behavior
   */
  async getActiveForClinic(clinicId: string) {
    const { data } = await api.get<{
      subscription: SubscriptionResponseDto;
      daysRemaining: number;
      isExpiringSoon: boolean;
      isExpired: boolean;
    } | null>(`/subscriptions/clinic/${clinicId}/active`);

    // Normalize Axios response: sometimes null JSON comes as empty string
    // This ensures we return a proper null object if the subscription is missing
    return data || null;
  },

  /** * 🔹 Get subscriptions for a clinic (paginated)
   * @param params - PaginationRequestDto & { includePayments?: boolean }
   */
  async getByClinicId(clinicId: string, params: Record<string, any> = {}) {
    const { data } = await api.get<PaginatedResult<SubscriptionResponseDto>>(
      `/subscriptions/clinic/${clinicId}`,
      { params },
    );
    return data;
  },

  /** * 🔹 Get all subscriptions (SuperAdmin only, paginated)
   */
  async getAll(params: Record<string, any> = {}) {
    const { data } = await api.get<PaginatedResult<SubscriptionResponseDto>>('/subscriptions', {
      params,
    });
    return data;
  },

  // =========================================================================
  // 📝 WRITE OPERATIONS
  // =========================================================================

  /** * 🔹 Create subscription
   */
  async create(payload: CreateSubscriptionRequestDto) {
    const { data } = await api.post<SubscriptionResponseDto>('/subscriptions', payload);
    return data;
  },

  /** * 🔹 Update subscription details
   */
  async update(id: string, payload: UpdateSubscriptionRequestDto) {
    const { data } = await api.put<SubscriptionResponseDto>(`/subscriptions/${id}`, payload);
    return data;
  },

  /** * 🔹 Change subscription status (Admin)
   */
  async changeStatus(id: string, newStatus: SubscriptionStatus) {
    // Backend expects [FromBody] SubscriptionStatus
    const { data } = await api.patch<{ message: string }>(`/subscriptions/${id}/status`, newStatus);
    return data;
  },

  /** * 🔹 Renew subscription
   * Note: Backend returns { Message: string, Subscription: Dto }
   */
  async renew(id: string, type: SubscriptionType) {
    const { data } = await api.post<{ message: string; subscription: SubscriptionResponseDto }>(
      `/subscriptions/${id}/renew`,
      null,
      {
        params: { type }, // Sends enum as query param
      },
    );
    return data;
  },

  /** * 🔹 Toggle auto-renew (Enable/Disable)
   */
  async toggleAutoRenew(id: string, enable: boolean) {
    const { data } = await api.patch<SubscriptionResponseDto>(
      `/subscriptions/${id}/auto-renew`,
      null,
      {
        params: { enable },
      },
    );
    return data;
  },

  // =========================================================================
  // ❌ DELETE OPERATIONS
  // =========================================================================

  /** * 🔹 Cancel subscription (Soft Delete Logic)
   * Stops renewal, marks status as Cancelled.
   */
  async cancel(id: string) {
    const { data } = await api.delete<{ message: string }>(`/subscriptions/${id}`);
    return data;
  },

  /** * 🔹 Hard delete (Permanent)
   * Removes all history. Admin only.
   */
  async hardDelete(id: string) {
    const { data } = await api.delete<{ message: string }>(`/subscriptions/${id}/hard`);
    return data;
  },
};
