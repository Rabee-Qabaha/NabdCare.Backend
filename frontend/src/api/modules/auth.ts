import { api } from "@/api/apiClient";
import type { LoginRequestDto, AuthResponseDto } from "@/types/backend/index";

/**
 * Handles authentication logic via the backend AuthEndpoints
 */
export const authApi = {
  async login(dto: LoginRequestDto) {
    const { data } = await api.post<AuthResponseDto>("/auth/login", dto);
    return data; // contains accessToken
  },

  async refresh() {
    const { data } = await api.post<AuthResponseDto>("/auth/refresh");
    return data;
  },

  async logout() {
    await api.post("/auth/logout");
    return true;
  },
};
