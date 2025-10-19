// src/service/AuthService.ts
import { apiService } from "@/service/apiService";
import type {
  LoginRequestDto,
  AuthResponseDto,
  RefreshRequestDto,
} from "@/types/backend";
import { getUserFromToken, isTokenExpired } from "@/utils/jwtUtils";
import type { UserInfo } from "@/utils/jwtUtils";

/**
 * AuthService: Handles login, logout, token management, and user info
 * Used by apiService + Pinia auth store
 */
export class AuthService {
  // ============ 🔐 Token Management ============
  private static accessTokenKey = "accessToken";
  private static refreshTokenKey = "refreshToken";

  /**
   * Get access token
   */
  static getAccessToken(): string | null {
    return (
      localStorage.getItem(this.accessTokenKey) ||
      sessionStorage.getItem(this.accessTokenKey)
    );
  }

  /**
   * Get refresh token
   */
  static getRefreshToken(): string | null {
    return (
      localStorage.getItem(this.refreshTokenKey) ||
      sessionStorage.getItem(this.refreshTokenKey)
    );
  }

  /**
   * Store tokens persistently
   */
  static storeTokens(tokens: AuthResponseDto, rememberMe: boolean): void {
    const storage = rememberMe ? localStorage : sessionStorage;

    storage.setItem(this.accessTokenKey, tokens.accessToken);
    storage.setItem(this.refreshTokenKey, tokens.refreshToken);

    // Clear the other storage type (avoid conflicts)
    const otherStorage = rememberMe ? sessionStorage : localStorage;
    otherStorage.removeItem(this.accessTokenKey);
    otherStorage.removeItem(this.refreshTokenKey);
  }

  /**
   * Clear all tokens
   */
  static clearTokens(): void {
    localStorage.removeItem(this.accessTokenKey);
    localStorage.removeItem(this.refreshTokenKey);
    sessionStorage.removeItem(this.accessTokenKey);
    sessionStorage.removeItem(this.refreshTokenKey);
  }

  // ============ 👤 User Info ============
  static getCurrentUser(): UserInfo | null {
    const token = this.getAccessToken();
    if (!token || isTokenExpired(token)) return null;
    return getUserFromToken(token);
  }

  // ============ 🔑 Auth Actions ============

  static async login(request: LoginRequestDto): Promise<AuthResponseDto> {
    const response = await apiService.post<AuthResponseDto>(
      "/auth/login",
      request
    );
    return response;
  }

  static async logout(): Promise<void> {
    const refreshToken = this.getRefreshToken();
    if (refreshToken) {
      try {
        await apiService.post("/auth/logout", { refreshToken });
      } catch (err) {
        console.warn("Logout API failed, continuing cleanup", err);
      }
    }
    this.clearTokens();
  }

  static async refreshTokens(
    request: RefreshRequestDto
  ): Promise<AuthResponseDto> {
    const response = await apiService.post<AuthResponseDto>(
      "/auth/refresh",
      request
    );
    return response;
  }
}
