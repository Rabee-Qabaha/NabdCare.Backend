// src/service/AuthService.ts
import { jwtDecode } from "@/utils/jwtDecoder";
import { refreshToken as apiRefreshToken } from "@/types/sdk.gen";
import { apiClient } from "@/api/apiClientInstance";

interface JwtPayload {
  sub: string;
  email: string;
  role: string;
  exp: number;
  ClinicId?: string;
}

const ACCESS_TOKEN_KEY = "access_token";
const REFRESH_TOKEN_KEY = "refresh_token";

export class AuthService {
  static saveTokens(accessToken: string, refreshToken: string) {
    localStorage.setItem(ACCESS_TOKEN_KEY, accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken);
  }

  static getAccessToken(): string | null {
    return localStorage.getItem(ACCESS_TOKEN_KEY);
  }

  static getRefreshToken(): string | null {
    return localStorage.getItem(REFRESH_TOKEN_KEY);
  }

  static clearTokens() {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
  }

  static async refreshToken() {
    const refreshToken = this.getRefreshToken();
    if (!refreshToken) throw new Error("No refresh token found.");

    const { data, error } = await apiRefreshToken({
      client: apiClient,
      body: { refreshToken },
    });

    if (error) throw error;

    this.saveTokens(data.accessToken, data.refreshToken);
    return data;
  }

  static decodeToken(): JwtPayload | null {
    const token = this.getAccessToken();
    if (!token) return null;
    try {
      return jwtDecode<JwtPayload>(token);
    } catch {
      return null;
    }
  }

  static async logout() {
    this.clearTokens();
  }
}
