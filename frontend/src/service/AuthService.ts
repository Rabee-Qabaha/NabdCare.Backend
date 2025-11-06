import { api } from '@/api/apiClient';
import type { LoginRequestDto, AuthResponseDto } from '@/types/backend';
import { getUserFromToken, isTokenExpired, type UserInfo } from '@/utils/jwtUtils';
import { tokenManager } from '@/utils/tokenManager';

/**
 * Handles all authentication logic:
 *  - Login, logout, refresh
 *  - Token persistence via tokenManager
 *  - User decoding and validity checks
 *  - Auto refresh concurrency guard
 */
export class AuthService {
  // ============ 📦 TYPES ============
  static refreshingPromise: Promise<string | null> | null = null;

  static getAccessToken(): string | null {
    return tokenManager.getAccessToken();
  }

  static storeAccessToken(token: string): void {
    tokenManager.setAccessToken(token, true);
  }

  static clearTokens(): void {
    tokenManager.clearTokens();
  }

  // ============ 👤 USER HELPERS ============

  /**
   * Decode and return current user if token is valid.
   */
  static getCurrentUser(): UserInfo | null {
    const token = this.getAccessToken();
    if (!token || isTokenExpired(token)) return null;
    return getUserFromToken(token);
  }

  /**
   * Ensure user info is up-to-date.
   * If token is expired, it will auto-refresh.
   */
  static async ensureCurrentUser(): Promise<UserInfo | null> {
    let token = this.getAccessToken();
    if (!token || isTokenExpired(token)) {
      await this.safeRefresh();
      token = this.getAccessToken();
    }
    return token ? getUserFromToken(token) : null;
  }

  // ============ 🔑 AUTH ACTIONS ============

  /**
   * Perform login with backend and store access token.
   */
  static async login(request: LoginRequestDto): Promise<{ accessToken: string; user: UserInfo }> {
    try {
      const { data } = await api.post<AuthResponseDto>('/auth/login', request);
      if (!data?.accessToken) throw new Error('Missing access token in response');

      this.storeAccessToken(data.accessToken);
      const user = getUserFromToken(data.accessToken);
      if (!user) throw new Error('Invalid token received from server');

      return { accessToken: data.accessToken, user };
    } catch (error: any) {
      const msg =
        error?.response?.data?.error?.message ||
        error.message ||
        'Login failed. Please check your credentials.';
      console.error('❌ AuthService.login:', msg);
      throw new Error(msg);
    }
  }

  /**
   * Refresh token using secure HttpOnly cookie.
   */
  static async refreshAccessToken(): Promise<string | null> {
    try {
      const { data } = await api.post<AuthResponseDto>('/auth/refresh');
      if (data?.accessToken) {
        this.storeAccessToken(data.accessToken);
        return data.accessToken;
      }
      return null;
    } catch (error: any) {
      console.warn('⚠️ Token refresh failed:', error?.message);
      this.clearTokens();
      return null;
    }
  }

  /**
   * Guard against concurrent refresh attempts.
   */
  static async safeRefresh(): Promise<string | null> {
    if (this.refreshingPromise) return this.refreshingPromise;

    this.refreshingPromise = this.refreshAccessToken().finally(() => {
      this.refreshingPromise = null;
    });

    return this.refreshingPromise;
  }

  /**
   * Logout: revoke refresh token (backend) + clear local tokens.
   */
  static async logout(): Promise<void> {
    try {
      await api.post('/auth/logout');
    } catch (err) {
      console.warn('⚠️ Logout API failed — continuing cleanup', err);
    } finally {
      this.clearTokens();
    }
  }
}
