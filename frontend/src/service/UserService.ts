// src/service/UserService.ts
import { apiService } from "@/service/apiService";
import type {
  UserResponseDto,
  CreateUserRequestDto,
  UpdateUserRequestDto,
  ChangePasswordRequestDto,
  ResetPasswordRequestDto,
} from "@/types/backend";

export const UserService = {
  // ============ GET USERS ============

  /**
   * Get all users (SuperAdmin sees all, ClinicAdmin sees only clinic users)
   */
  async getAllUsers(): Promise<UserResponseDto[]> {
    return await apiService.get<UserResponseDto[]>("/users");
  },

  /**
   * Get users by clinic ID (SuperAdmin only)
   */
  async getUsersByClinic(clinicId: string): Promise<UserResponseDto[]> {
    return await apiService.get<UserResponseDto[]>(`/users/clinic/${clinicId}`);
  },

  /**
   * Get current user's details
   */
  async getCurrentUser(): Promise<UserResponseDto> {
    return await apiService.get<UserResponseDto>("/users/me");
  },

  /**
   * Get user by ID
   */
  async getUserById(id: string): Promise<UserResponseDto> {
    return await apiService.get<UserResponseDto>(`/users/${id}`);
  },

  // ============ CREATE/UPDATE/DELETE ============

  async createUser(data: CreateUserRequestDto): Promise<UserResponseDto> {
    return await apiService.post<UserResponseDto>("/users", data);
  },

  async updateUser(
    id: string,
    data: UpdateUserRequestDto
  ): Promise<UserResponseDto> {
    return await apiService.put<UserResponseDto>(`/users/${id}`, data);
  },

  async updateUserRole(id: string, roleId: string): Promise<UserResponseDto> {
    return await apiService.put<UserResponseDto>(`/users/${id}/role`, {
      roleId,
    });
  },

  /**
   * Soft delete user
   */
  async deleteUser(id: string): Promise<void> {
    await apiService.delete(`/users/${id}`);
  },

  /**
   * Hard delete user (SuperAdmin only)
   */
  async hardDeleteUser(id: string): Promise<void> {
    await apiService.delete(`/users/${id}/permanent`);
  },

  // ============ ACTIVATE/DEACTIVATE ============

  async activateUser(id: string): Promise<UserResponseDto> {
    return await apiService.put<UserResponseDto>(`/users/${id}/activate`, {});
  },

  async deactivateUser(id: string): Promise<UserResponseDto> {
    return await apiService.put<UserResponseDto>(`/users/${id}/deactivate`, {});
  },

  // ============ PASSWORD MANAGEMENT ============

  /**
   * Change own password (requires current password)
   */
  async changePassword(
    id: string,
    data: ChangePasswordRequestDto
  ): Promise<UserResponseDto> {
    return await apiService.post<UserResponseDto>(
      `/users/${id}/change-password`,
      data
    );
  },

  /**
   * Reset user password (ClinicAdmin)
   */
  async resetPassword(
    id: string,
    data: ResetPasswordRequestDto
  ): Promise<UserResponseDto> {
    return await apiService.post<UserResponseDto>(
      `/users/${id}/reset-password`,
      data
    );
  },

  /**
   * Admin reset password (SuperAdmin)
   */
  async adminResetPassword(
    id: string,
    data: ResetPasswordRequestDto
  ): Promise<UserResponseDto> {
    return await apiService.post<UserResponseDto>(
      `/users/${id}/admin-reset-password`,
      data
    );
  },
};
