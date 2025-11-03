import { api } from "@/api/apiClient";
import type {
  RoleResponseDto,
  ClinicResponseDto,
  PaginatedResult,
} from "@/types/backend";

/**
 * Dropdown Data Service
 * Location: src/services/api/DropdownDataService.ts
 *
 * Purpose:
 * - Centralize all dropdown/select data loading
 * - Handle API pagination quirks
 * - Normalize different response formats
 * - Provide consistent error handling
 *
 * Benefits:
 * - Single source of truth
 * - Easy to test and mock
 * - Changes in one place
 * - Reusable across app
 *
 * Updated: 2025-11-02
 */

export interface DropdownFetchOptions {
  limit?: number;
  descending?: boolean;
  search?: string;
}

/**
 * Service for fetching dropdown/select data
 * Handles all API quirks and pagination automatically
 */
export class DropdownDataService {
  /**
   * Get roles for dropdown
   *
   * Features:
   * - Automatic pagination params
   * - Response normalization
   * - Error logging
   * - Sorts by displayOrder
   *
   * @param options - Fetch options (limit, descending, search)
   * @returns Array of roles sorted by displayOrder
   * @throws Error with user-friendly message
   */
  static async fetchRoles(
    options: DropdownFetchOptions = {}
  ): Promise<RoleResponseDto[]> {
    const { limit = 100, descending = false, search = "" } = options;

    try {
      console.log("📍 DropdownDataService.fetchRoles() - options:", options);

      const { data } = await api.get<
        PaginatedResult<RoleResponseDto> | RoleResponseDto[]
      >("/roles", {
        params: {
          Limit: limit,
          Descending: descending,
          ...(search ? { search } : {}),
        },
      });

      // ✅ Normalize response
      let roles = this.normalizeResponse<RoleResponseDto>(data);

      // ✅ Sort by displayOrder (important for UI consistency)
      roles = roles.sort((a, b) => a.displayOrder - b.displayOrder);

      console.log(
        `✅ DropdownDataService.fetchRoles() - loaded ${roles.length} roles`
      );

      // ✅ Log role breakdown
      const breakdown = {
        system: roles.filter((r) => r.isSystemRole).length,
        templates: roles.filter((r) => r.isTemplate).length,
        clinicSpecific: roles.filter((r) => r.clinicId).length,
      };
      console.log("📊 Role breakdown:", breakdown);

      return roles;
    } catch (error) {
      console.error("❌ DropdownDataService.fetchRoles() - error:", error);
      throw new Error("Failed to load roles. Please try again.");
    }
  }

  /**
   * Get clinics for dropdown
   *
   * Features:
   * - Automatic pagination params
   * - Response normalization
   * - Handles empty results
   *
   * @param options - Fetch options
   * @returns Array of clinics
   * @throws Error with user-friendly message
   */
  static async fetchClinics(
    options: DropdownFetchOptions = {}
  ): Promise<ClinicResponseDto[]> {
    const { limit = 100, descending = false, search = "" } = options;

    try {
      console.log("📍 DropdownDataService.fetchClinics() - options:", options);

      const { data } = await api.get<
        PaginatedResult<ClinicResponseDto> | ClinicResponseDto[]
      >("/clinics", {
        params: {
          Limit: limit,
          Descending: descending,
          ...(search ? { search } : {}),
        },
      });

      // ✅ Normalize response
      const clinics = this.normalizeResponse<ClinicResponseDto>(data);

      console.log(
        `✅ DropdownDataService.fetchClinics() - loaded ${clinics.length} clinics`
      );
      return clinics;
    } catch (error) {
      console.error("❌ DropdownDataService.fetchClinics() - error:", error);
      throw new Error("Failed to load clinics. Please try again.");
    }
  }

  /**
   * Normalize API responses to handle multiple formats:
   *
   * Format 1 - Direct array:
   * [{ id: "1", name: "Admin" }, ...]
   *
   * Format 2 - Paginated:
   * { items: [...], hasMore: true, nextCursor: "...", totalCount: 5 }
   *
   * ✅ Handles both formats gracefully
   *
   * @param response - API response (any format)
   * @returns Normalized array of items
   */
  private static normalizeResponse<T>(response: any): T[] {
    // ✅ If it's already an array, return as-is
    if (Array.isArray(response)) {
      console.log("📊 Normalization: Detected direct array format");
      return response;
    }

    // ✅ If it's a paginated response with items
    if (response && typeof response === "object" && "items" in response) {
      const items = Array.isArray(response.items) ? response.items : [];
      console.log(
        `📊 Normalization: Detected paginated format, extracted ${items.length} items`
      );
      return items;
    }

    // ✅ Fallback - return empty array
    console.warn("⚠️ Normalization: Unexpected response format, returning []");
    console.warn("⚠️ Response was:", response);
    return [];
  }

  /**
   * Search roles by name/description
   * Respects displayOrder sorting
   *
   * @param query - Search term
   * @returns Filtered and sorted roles
   */
  static async searchRoles(query: string): Promise<RoleResponseDto[]> {
    if (!query || query.trim().length === 0) {
      return this.fetchRoles();
    }
    return this.fetchRoles({ search: query });
  }

  /**
   * Search clinics by name
   *
   * @param query - Search term
   * @returns Filtered clinics
   */
  static async searchClinics(query: string): Promise<ClinicResponseDto[]> {
    if (!query || query.trim().length === 0) {
      return this.fetchClinics();
    }
    return this.fetchClinics({ search: query });
  }

  /**
   * Get only system roles
   */
  static async fetchSystemRoles(): Promise<RoleResponseDto[]> {
    const roles = await this.fetchRoles();
    return roles.filter((r) => r.isSystemRole);
  }

  /**
   * Get only template roles
   */
  static async fetchTemplateRoles(): Promise<RoleResponseDto[]> {
    const roles = await this.fetchRoles();
    return roles.filter((r) => r.isTemplate);
  }

  /**
   * Get clinic-specific roles
   */
  static async fetchClinicRoles(clinicId?: string): Promise<RoleResponseDto[]> {
    const roles = await this.fetchRoles();
    return roles.filter((r) => {
      if (clinicId) {
        return r.clinicId === clinicId;
      }
      return !r.isSystemRole && !r.isTemplate && r.clinicId;
    });
  }
}
