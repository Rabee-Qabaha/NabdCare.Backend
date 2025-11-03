import { api } from "@/api/apiClient";
import type {
  UserResponseDto,
  CreateUserRequestDto,
  UpdateUserRequestDto,
  ChangePasswordRequestDto,
  ResetPasswordRequestDto,
  PaginatedResult,
} from "@/types/backend";

/**
 * User API Module
 * Location: src/api/modules/users.ts
 *
 * Handles all user-related API calls
 *
 * Author: Rabee Qabaha
 * Updated: 2025-11-02
 */

export const userApi = {
  /**
   * Get users (paged with cursor pagination)
   *
   * Query params:
   * - cursor?: string - Cursor for pagination
   * - limit?: number - Number of items per page (default: 20)
   * - search?: string - Search by email or name
   * - Descending?: boolean - Sort descending (required by backend)
   *
   * ✅ FIXED: Added default Descending parameter
   */
  async getPaged(params?: Record<string, any>) {
    try {
      // ✅ FIX: Ensure Descending is always sent (default: true)
      const queryParams = {
        Limit: 20,
        Descending: true,
        ...params, // Allow override
      };

      console.log("📍 Fetching users with params:", queryParams);

      const response = await api.get<PaginatedResult<UserResponseDto>>(
        "/users/paged",
        { params: queryParams }
      );

      return response;
    } catch (error) {
      console.error("❌ Error fetching users:", error);
      throw error;
    }
  },

  /**
   * Get users in specific clinic (SuperAdmin only)
   *
   * @param clinicId - Clinic ID
   * @param params - Query parameters (cursor, limit, search, etc.)
   *
   * ✅ FIXED: Added default Descending parameter
   */
  async getByClinicPaged(clinicId: string, params?: Record<string, any>) {
    try {
      const queryParams = {
        Limit: 20,
        Descending: true,
        ...params,
      };

      const response = await api.get<PaginatedResult<UserResponseDto>>(
        `/users/clinic/${clinicId}/paged`,
        { params: queryParams }
      );

      return response;
    } catch (error) {
      console.error(`❌ Error fetching users for clinic ${clinicId}:`, error);
      throw error;
    }
  },

  /**
   * Get user by ID
   *
   * @param id - User ID (GUID)
   */
  async getById(id: string) {
    try {
      const response = await api.get<UserResponseDto>(`/users/${id}`);
      return response;
    } catch (error) {
      console.error(`❌ Error fetching user ${id}:`, error);
      throw error;
    }
  },

  /**
   * Get current user (profile)
   */
  async getMe() {
    try {
      const response = await api.get<UserResponseDto>("/users/me");
      return response;
    } catch (error) {
      console.error("❌ Error fetching current user:", error);
      throw error;
    }
  },

  /**
   * Create user
   *
   * @param data - User creation data
   */
  async create(data: CreateUserRequestDto) {
    try {
      const response = await api.post<UserResponseDto>("/users", data);
      return response;
    } catch (error) {
      console.error("❌ Error creating user:", error);
      throw error;
    }
  },

  /**
   * Update user
   *
   * @param id - User ID
   * @param data - Update data
   */
  async update(id: string, data: UpdateUserRequestDto) {
    try {
      const response = await api.put<UserResponseDto>(`/users/${id}`, data);
      return response;
    } catch (error) {
      console.error(`❌ Error updating user ${id}:`, error);
      throw error;
    }
  },

  /**
   * Update user role
   *
   * @param id - User ID
   * @param roleId - New role ID
   */
  async updateRole(id: string, roleId: string) {
    try {
      const response = await api.put<UserResponseDto>(`/users/${id}/role`, {
        roleId,
      });
      return response;
    } catch (error) {
      console.error(`❌ Error updating user ${id} role to ${roleId}:`, error);
      throw error;
    }
  },

  /**
   * Activate user
   *
   * @param id - User ID
   */
  async activate(id: string) {
    try {
      const response = await api.put<UserResponseDto>(
        `/users/${id}/activate`,
        {}
      );
      return response;
    } catch (error) {
      console.error(`❌ Error activating user ${id}:`, error);
      throw error;
    }
  },

  /**
   * Deactivate user
   *
   * @param id - User ID
   */
  async deactivate(id: string) {
    try {
      const response = await api.put<UserResponseDto>(
        `/users/${id}/deactivate`,
        {}
      );
      return response;
    } catch (error) {
      console.error(`❌ Error deactivating user ${id}:`, error);
      throw error;
    }
  },

  /**
   * Delete user (soft delete)
   *
   * @param id - User ID
   */
  async delete(id: string) {
    try {
      const response = await api.delete(`/users/${id}`);
      return response;
    } catch (error) {
      console.error(`❌ Error deleting user ${id}:`, error);
      throw error;
    }
  },

  /**
   * Hard delete user (permanent)
   *
   * @param id - User ID
   */
  async hardDelete(id: string) {
    try {
      const response = await api.delete(`/users/${id}/permanent`);
      return response;
    } catch (error) {
      console.error(`❌ Error hard deleting user ${id}:`, error);
      throw error;
    }
  },

  /**
   * Change password (self-service)
   *
   * @param id - User ID
   * @param data - Password change data
   */
  async changePassword(id: string, data: ChangePasswordRequestDto) {
    try {
      const response = await api.post(`/users/${id}/change-password`, data);
      return response;
    } catch (error) {
      console.error(`❌ Error changing password for user ${id}:`, error);
      throw error;
    }
  },

  /**
   * Reset password (admin action)
   *
   * @param id - User ID
   * @param data - Password reset data
   */
  async resetPassword(id: string, data: ResetPasswordRequestDto) {
    try {
      const response = await api.post(`/users/${id}/reset-password`, data);
      return response;
    } catch (error) {
      console.error(`❌ Error resetting password for user ${id}:`, error);
      throw error;
    }
  },
};
