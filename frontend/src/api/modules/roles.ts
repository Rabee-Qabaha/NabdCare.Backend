// src/api/modules/roles.ts
import { api } from "@/api/apiClient";
import type {
  RoleResponseDto,
  CreateRoleRequestDto,
  UpdateRoleRequestDto,
  CloneRoleRequestDto,
} from "@/types/backend/index";

export const rolesApi = {
  /** 📋 Get all roles (filtered by tenant context) */
  async getAll() {
    const { data } = await api.get<RoleResponseDto[]>("/roles");
    return data;
  },

  /** 📋 Get system roles (SuperAdmin only) */
  async getSystem() {
    const { data } = await api.get<RoleResponseDto[]>("/roles/system");
    return data;
  },

  /** 📋 Get template roles */
  async getTemplates() {
    const { data } = await api.get<RoleResponseDto[]>("/roles/templates");
    return data;
  },

  /** 📋 Get roles for a specific clinic */
  async getClinicRoles(clinicId: string) {
    const { data } = await api.get<RoleResponseDto[]>(
      `/roles/clinic/${clinicId}`
    );
    return data;
  },

  /** 🔍 Get role by ID */
  async getById(id: string) {
    const { data } = await api.get<RoleResponseDto>(`/roles/${id}`);
    return data;
  },

  /** ➕ Create custom role */
  async create(payload: CreateRoleRequestDto) {
    const { data } = await api.post<RoleResponseDto>("/roles", payload);
    return data;
  },

  /** 🔄 Clone template role for a clinic */
  async clone(templateRoleId: string, payload: CloneRoleRequestDto) {
    const { data } = await api.post<RoleResponseDto>(
      `/roles/clone/${templateRoleId}`,
      payload
    );
    return data;
  },

  /** ✏️ Update an existing role */
  async update(id: string, payload: UpdateRoleRequestDto) {
    const { data } = await api.put<RoleResponseDto>(`/roles/${id}`, payload);
    return data;
  },

  /** 🗑️ Delete a role */
  async delete(id: string) {
    const { data } = await api.delete(`/roles/${id}`);
    return data;
  },

  /** 🔐 Get role permissions */
  async getPermissions(roleId: string) {
    const { data } = await api.get<string[]>(`/roles/${roleId}/permissions`);
    return data;
  },

  /** ➕ Assign a permission to a role */
  async assignPermission(roleId: string, permissionId: string) {
    const { data } = await api.post(
      `/roles/${roleId}/permissions/${permissionId}`
    );
    return data;
  },

  /** ➖ Remove a permission from a role */
  async removePermission(roleId: string, permissionId: string) {
    const { data } = await api.delete(
      `/roles/${roleId}/permissions/${permissionId}`
    );
    return data;
  },
};
