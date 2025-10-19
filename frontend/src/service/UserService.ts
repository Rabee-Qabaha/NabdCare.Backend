// src/service/UserService.ts
import { apiService } from "@/service/apiService";
import type {
  User,
  CreateUserRequestDto,
  UpdateUserRequestDto,
} from "@/types/backend/index";

export const UserService = {
  async getAllUsers(): Promise<User[]> {
    const response = await apiService.get<User[]>("/users");
    return response;
  },

  async getClinicUsers(clinicId: string): Promise<User[]> {
    const response = await apiService.get<User[]>(`/clinics/${clinicId}/users`);
    return response;
  },

  async createUser(data: CreateUserRequestDto): Promise<User> {
    const response = await apiService.post<User>("/users", data);
    return response;
  },

  async updateUser(id: string, data: UpdateUserRequestDto): Promise<User> {
    const response = await apiService.put<User>(`/users/${id}`, data);
    return response;
  },

  async deleteUser(id: string): Promise<void> {
    await apiService.delete(`/users/${id}`);
  },

  async changePassword(id: string, newPassword: string): Promise<void> {
    await apiService.post(`/users/${id}/change-password`, { newPassword });
  },
};
