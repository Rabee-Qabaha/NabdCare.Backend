// src/service/AuthService.ts
import { apiService } from "@/service/apiService";
import type { LoginRequestDto, AuthResponseDto } from "@/types/backend";
import { getUserFromToken, isTokenExpired } from "@/utils/jwtUtils";
import type { UserInfo } from "@/utils/jwtUtils";
import { tokenManager } from "@/utils/tokenManager";

/**
 * AuthService: Handles login, logout, token management, and user info
 */
export class AuthService {
  // ============ 🔐 Token Management ============

  /**
   * Get access token from secure storage
   */
  static getAccessToken(): string | null {
    return tokenManager.getAccessToken();
  }

  /**
   * Store access token securely
   * NOTE: Refresh token is in HttpOnly cookie (backend manages it)
   */
  static storeAccessToken(token: string): void {
    tokenManager.setAccessToken(token, true);
  }

  /**
   * Clear all tokens
   */
  static clearTokens(): void {
    tokenManager.clearTokens();
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

    // Store access token (refresh token is in HttpOnly cookie)
    this.storeAccessToken(response.accessToken);

    return response;
  }

  static async logout(): Promise<void> {
    try {
      // Backend will clear the HttpOnly cookie
      await apiService.post("/auth/logout", {});
    } catch (err) {
      console.warn("Logout API failed, continuing cleanup", err);
    } finally {
      this.clearTokens();
    }
  }

  /**
   * Refresh access token using HttpOnly cookie
   */
  static async refreshAccessToken(): Promise<string | null> {
    return tokenManager.refreshAccessToken();
  }
}
