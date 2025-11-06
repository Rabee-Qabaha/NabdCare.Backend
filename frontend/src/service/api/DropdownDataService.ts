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
  // =========================================================
  // 🔹 Fetch Roles
  // =========================================================
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

      // ✅ Sort by displayOrder if exists
      roles = roles.sort(
        (a, b) => (a.displayOrder || 0) - (b.displayOrder || 0)
      );

      console.log(`✅ Loaded ${roles.length} roles`);
      console.table(
        roles.map((r) => ({
          name: r.name,
          system: r.isSystemRole,
          clinic: r.clinicName,
        }))
      );

      return roles;
    } catch (error) {
      console.error("❌ DropdownDataService.fetchRoles() - error:", error);
      throw new Error("Failed to load roles. Please try again.");
    }
  }

  // =========================================================
  // 🔹 Fetch Clinics
  // =========================================================
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

      const clinics = this.normalizeResponse<ClinicResponseDto>(data);

      console.log(`✅ Loaded ${clinics.length} clinics`);
      return clinics;
    } catch (error) {
      console.error("❌ DropdownDataService.fetchClinics() - error:", error);
      throw new Error("Failed to load clinics. Please try again.");
    }
  }

  // =========================================================
  // 🔹 Normalization Helper
  // =========================================================
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
   */
  private static normalizeResponse<T>(response: any): T[] {
    if (Array.isArray(response)) {
      console.log("📊 Normalization: Detected direct array format");
      return response;
    }

    if (response && typeof response === "object" && "items" in response) {
      const items = Array.isArray(response.items) ? response.items : [];
      console.log(
        `📊 Normalization: Detected paginated format, extracted ${items.length} items`
      );
      return items;
    }

    console.warn("⚠️ Normalization: Unexpected response format, returning []");
    console.warn("⚠️ Response was:", response);
    return [];
  }

  // =========================================================
  // 🔹 Role Utilities
  // =========================================================

  /**
   * Search roles by name/description
   */
  static async searchRoles(query: string): Promise<RoleResponseDto[]> {
    if (!query?.trim()) return this.fetchRoles();
    return this.fetchRoles({ search: query });
  }

  /**
   * Get only system roles
   */
  static async fetchSystemRoles(): Promise<RoleResponseDto[]> {
    const roles = await this.fetchRoles();
    return roles.filter((r) => r.isSystemRole);
  }

  /**
   * Get only user/clinic roles (non-system)
   */
  static async fetchClinicRoles(clinicId?: string): Promise<RoleResponseDto[]> {
    const roles = await this.fetchRoles();

    return roles.filter((r) => {
      if (clinicId) {
        return !r.isSystemRole && r.clinicId === clinicId;
      }
      return !r.isSystemRole;
    });
  }

  /**
   * Group roles into system and user/clinic roles
   * Perfect for dropdowns and UI grouping
   */
  static async fetchGroupedRoles(): Promise<{
    systemRoles: RoleResponseDto[];
    clinicRoles: RoleResponseDto[];
  }> {
    const roles = await this.fetchRoles();

    return {
      systemRoles: roles.filter((r) => r.isSystemRole),
      clinicRoles: roles.filter((r) => !r.isSystemRole),
    };
  }

  // =========================================================
  // 🔹 Clinic Utilities
  // =========================================================

  /**
   * Search clinics by name
   */
  static async searchClinics(query: string): Promise<ClinicResponseDto[]> {
    if (!query?.trim()) return this.fetchClinics();
    return this.fetchClinics({ search: query });
  }
}
