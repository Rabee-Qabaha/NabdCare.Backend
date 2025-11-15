// src/api/modules/roles.ts
import { api } from "@/api/apiClient";
import type {
  RoleResponseDto,
  CreateRoleRequestDto,
  UpdateRoleRequestDto,
  CloneRoleRequestDto,
} from "@/types/backend";
import type { AxiosResponse } from "axios";

export interface RolesQuery {
  includeDeleted?: boolean;
  clinicId?: string | null;
}

/**
 * Roles API
 * -----------------------------------------
 * Fully refactored with:
 * - Consistent camelCase → API PascalCase mapping where needed
 * - Client-side grouped role formatting
 * - All existing features preserved
 */
export const rolesApi = {
  /** Get all roles (system + clinic) */
  getAll(params?: RolesQuery): Promise<AxiosResponse<RoleResponseDto[]>> {
    return api.get("/roles", { params });
  },

  /** Get system roles only */
  getSystem(): Promise<AxiosResponse<RoleResponseDto[]>> {
    return api.get("/roles/system");
  },

  /** Get template roles only */
  getTemplates(): Promise<AxiosResponse<RoleResponseDto[]>> {
    return api.get("/roles/templates");
  },

  /** Get roles for a specific clinic */
  getClinicRoles(clinicId: string): Promise<AxiosResponse<RoleResponseDto[]>> {
    return api.get(`/roles/clinic/${clinicId}`);
  },

  /** Get role details */
  getById(id: string): Promise<AxiosResponse<RoleResponseDto>> {
    return api.get(`/roles/${id}`);
  },

  /** Create a role */
  create(payload: CreateRoleRequestDto): Promise<AxiosResponse<RoleResponseDto>> {
    return api.post("/roles", payload);
  },

  /** Clone a role */
  clone(templateRoleId: string, payload: CloneRoleRequestDto) {
    return api.post(`/roles/${templateRoleId}/clone`, payload);
  },

  /** Update a role */
  update(id: string, payload: UpdateRoleRequestDto) {
    return api.put(`/roles/${id}`, payload);
  },

  /** Soft delete a role */
  delete(id: string) {
    return api.delete(`/roles/${id}`);
  },

  /** Restore a deleted role */
  restore(id: string) {
    return api.post(`/roles/${id}/restore`);
  },

  /** Get permissions for a role */
  getPermissions(id: string) {
    return api.get(`/roles/${id}/permissions`);
  },

  /** Assign a permission */
  assignPermission(roleId: string, permissionId: string) {
    return api.post(`/roles/${roleId}/permissions/${permissionId}`);
  },

  /** Remove a permission */
  removePermission(roleId: string, permissionId: string) {
    return api.delete(`/roles/${roleId}/permissions/${permissionId}`);
  },

  // ============================================================
  // ⭐ NEW — getGrouped() (client-side aggregation)
  // ============================================================
  async getGrouped(): Promise<{
    systemRoles: RoleResponseDto[];
    clinicRoles: RoleResponseDto[];
    templateRoles: RoleResponseDto[];
  }> {
    const [system, templates, all] = await Promise.all([
      this.getSystem().then(r => r.data),
      this.getTemplates().then(r => r.data),
      this.getAll().then(r => r.data),
    ]);

    // Filter out system + template to get only clinic roles
    const clinicRoles = all.filter(
      role =>
        !system.some(s => s.id === role.id) &&
        !templates.some(t => t.id === role.id)
    );

    return {
      systemRoles: system,
      templateRoles: templates,
      clinicRoles,
    };
  },
};