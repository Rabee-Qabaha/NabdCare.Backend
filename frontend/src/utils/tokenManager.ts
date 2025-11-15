// src/utils/tokenManager.ts
import { jwtDecode, type JwtPayload } from 'jwt-decode';

/**
 * TokenManager
 * -----------------------------------------------------
 * Manages short-lived access tokens in-memory
 * + auto-refresh using HttpOnly cookie (secure backend flow)
 *
 * Access token is stored in memory only — with an optional
 * backup in sessionStorage (for tab reload recovery).
 */
class TokenManager {
  private accessToken: string | null = null;
  private refreshTimeoutId: number | null = null;
  private refreshPromise: Promise<string | null> | null = null;
  private readonly BACKUP_KEY = '_at_backup';
  private readonly REFRESH_PATH = "/auth/refresh";

  /**
   * ✅ Store token in memory and optional session backup
   */
  setAccessToken(token: string, backup = true): void {
    this.accessToken = token;

    if (backup) {
      try {
        sessionStorage.setItem(this.BACKUP_KEY, token);
      } catch {
        console.warn('⚠️ SessionStorage not available — running in private mode?');
      }
    }

    this.scheduleTokenRefresh(token);
  }

  /**
   * ✅ Retrieve token from memory or backup storage
   */
  getAccessToken(): string | null {
    if (this.accessToken) return this.accessToken;

    try {
      const backup = sessionStorage.getItem(this.BACKUP_KEY);
      if (backup && !this.isTokenExpired(backup)) {
        this.accessToken = backup;
        this.scheduleTokenRefresh(backup);
        return backup;
      }
    } catch {
      console.warn('⚠️ Could not read from sessionStorage.');
    }

    return null;
  }

  /**
   * ✅ Clear all stored tokens and timers
   */
  clearTokens(): void {
    this.accessToken = null;

    try {
      sessionStorage.removeItem(this.BACKUP_KEY);
    } catch {
      /* ignore */
    }

    if (this.refreshTimeoutId) {
      clearTimeout(this.refreshTimeoutId);
      this.refreshTimeoutId = null;
    }
  }

  /**
   * ✅ Refresh access token via HttpOnly cookie
   * Prevents duplicate calls across rapid re-renders
   */
  async refreshAccessToken(): Promise<string | null> {
    if (this.refreshPromise) return this.refreshPromise;

    this.refreshPromise = this._performRefresh();
    try {
      return await this.refreshPromise;
    } finally {
      this.refreshPromise = null;
    }
  }

private async _performRefresh(): Promise<string | null> {
    const baseURL = import.meta.env.VITE_API_BASE_URL || "/api";

    try {
      // ✅ use the constant here instead of hardcoding
      const response = await fetch(`${baseURL}${this.REFRESH_PATH}`, {
        method: "POST",
        credentials: "include",
        headers: { "Content-Type": "application/json" },
      });

      if (!response.ok) throw new Error(`Token refresh failed (${response.status})`);

      const data = await response.json();

      if (data?.accessToken) {
        this.setAccessToken(data.accessToken, true);
        console.log("🔁 Access token refreshed successfully");
        return data.accessToken;
      }

      console.warn("⚠️ No token received during refresh");
      this.clearTokens();
      return null;
    } catch (error) {
      console.error("❌ Token refresh error:", error);
      this.clearTokens();
      return null;
    }
  }

  /**
   * ⏳ Automatically schedule refresh ~5 minutes before expiration
   */
  private scheduleTokenRefresh(token: string): void {
    if (this.refreshTimeoutId) {
      clearTimeout(this.refreshTimeoutId);
      this.refreshTimeoutId = null;
    }

    try {
      const decoded = jwtDecode<JwtPayload>(token);
      if (!decoded.exp) return;

      const expiresAt = decoded.exp * 1000;
      const now = Date.now();
      const msUntilExpire = expiresAt - now;
      const refreshDelay = Math.max(msUntilExpire - 5 * 60 * 1000, 10_000); // at least 10s before

      console.log(`🕐 Scheduling next refresh in ${(refreshDelay / 1000 / 60).toFixed(1)} min`);

      this.refreshTimeoutId = window.setTimeout(() => {
        this.refreshAccessToken().catch((err) => console.error('⚠️ Auto-refresh failed:', err));
      }, refreshDelay);
    } catch (err) {
      console.error('❌ Failed to schedule refresh:', err);
    }
  }

  /**
   * 🔒 Check if token is expired
   */
  private isTokenExpired(token: string): boolean {
    try {
      const decoded = jwtDecode<JwtPayload>(token);
      if (!decoded.exp) return true;
      return decoded.exp < Date.now() / 1000;
    } catch {
      return true;
    }
  }
}

export const tokenManager = new TokenManager();
