import { jwtDecode, type JwtPayload } from "jwt-decode";

/**
 * Token Manager - stores access token in memory w/ backup in sessionStorage
 * Refresh Token stays in HttpOnly cookie only ✅ Secure
 */
class TokenManager {
  private accessToken: string | null = null;
  private refreshTimeoutId: number | null = null;
  private refreshPromise: Promise<string | null> | null = null;

  /**
   * Set and schedule auto refresh
   */
  setAccessToken(token: string, backup: boolean = true): void {
    this.accessToken = token;

    if (backup) {
      sessionStorage.setItem("_at_backup", token);
    }

    this.scheduleTokenRefresh(token);
  }

  /**
   * Get token from memory or fallback backup
   */
  getAccessToken(): string | null {
    if (this.accessToken) return this.accessToken;

    const backup = sessionStorage.getItem("_at_backup");
    if (backup && !this.isTokenExpired(backup)) {
      this.accessToken = backup;
      this.scheduleTokenRefresh(backup);
      return backup;
    }

    return null;
  }

  /**
   * Clear everything
   */
  clearTokens(): void {
    this.accessToken = null;
    sessionStorage.removeItem("_at_backup");

    if (this.refreshTimeoutId) {
      clearTimeout(this.refreshTimeoutId);
      this.refreshTimeoutId = null;
    }
  }

  /**
   * Refresh access token using HttpOnly cookie
   */
  async refreshAccessToken(): Promise<string | null> {
    if (this.refreshPromise) {
      return this.refreshPromise;
    }

    this.refreshPromise = this._performRefresh();

    try {
      return await this.refreshPromise;
    } finally {
      this.refreshPromise = null;
    }
  }

  private async _performRefresh(): Promise<string | null> {
    try {
      const baseURL =
        import.meta.env.VITE_API_BASE_URL || "http://localhost:5175/api";

      const response = await fetch(`${baseURL}/auth/refresh`, {
        method: "POST",
        credentials: "include",
        headers: {
          "Content-Type": "application/json",
        },
      });

      if (!response.ok) throw new Error("Token refresh failed");

      const data = await response.json();

      if (data.accessToken) {
        this.setAccessToken(data.accessToken, true);
        return data.accessToken;
      }

      return null;
    } catch (error) {
      console.error("❌ Token refresh error:", error);
      this.clearTokens();
      return null;
    }
  }

  /**
   * Auto refresh 5 mins before expiry
   */
  private scheduleTokenRefresh(token: string): void {
    if (this.refreshTimeoutId) {
      clearTimeout(this.refreshTimeoutId);
    }

    try {
      const decoded = jwtDecode<JwtPayload>(token);
      if (!decoded.exp) return;

      const expiresAt = decoded.exp * 1000;
      const now = Date.now();
      const msUntilExpire = expiresAt - now;
      const refreshMs = msUntilExpire - 5 * 60 * 1000;

      if (refreshMs > 0) {
        this.refreshTimeoutId = window.setTimeout(
          () => this.refreshAccessToken(),
          refreshMs
        );
      } else {
        this.refreshAccessToken();
      }
    } catch (error) {
      console.error("❌ Failed to schedule refresh:", error);
    }
  }

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
