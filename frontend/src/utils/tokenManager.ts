import { jwtDecode, type JwtPayload } from "jwt-decode";

/**
 * Secure token manager - stores access token in memory with sessionStorage backup
 * Refresh token lives in HttpOnly cookie (managed by backend)
 */
class TokenManager {
  private accessToken: string | null = null;
  private refreshTimeoutId: number | null = null;
  private refreshPromise: Promise<string | null> | null = null;

  // ============ Access Token Management ============

  /**
   * Set access token in memory and optionally backup to sessionStorage
   */
  setAccessToken(token: string, backup: boolean = true): void {
    this.accessToken = token;

    // Backup to sessionStorage for page refresh recovery
    if (backup) {
      sessionStorage.setItem("_at_backup", token);
    }

    // Schedule auto-refresh before token expires
    this.scheduleTokenRefresh(token);
  }

  /**
   * Get access token from memory (or sessionStorage backup on page load)
   */
  getAccessToken(): string | null {
    // Try memory first (fastest)
    if (this.accessToken) {
      return this.accessToken;
    }

    // Fallback to sessionStorage (survives page refresh)
    const backup = sessionStorage.getItem("_at_backup");
    if (backup && !this.isTokenExpired(backup)) {
      this.accessToken = backup;
      this.scheduleTokenRefresh(backup);
      return backup;
    }

    return null;
  }

  /**
   * Clear all tokens
   */
  clearTokens(): void {
    this.accessToken = null;
    sessionStorage.removeItem("_at_backup");

    // Clear any scheduled refresh
    if (this.refreshTimeoutId) {
      clearTimeout(this.refreshTimeoutId);
      this.refreshTimeoutId = null;
    }
  }

  // ============ Token Refresh Logic ============

  /**
   * Refresh access token using HttpOnly cookie
   * Returns new access token or null if refresh fails
   */
  async refreshAccessToken(): Promise<string | null> {
    // Prevent multiple simultaneous refresh requests
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
        credentials: "include", // ✅ CRITICAL: Sends HttpOnly cookie
        headers: {
          "Content-Type": "application/json",
        },
      });

      if (!response.ok) {
        throw new Error("Token refresh failed");
      }

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
   * Schedule automatic token refresh 5 minutes before expiry
   */
  private scheduleTokenRefresh(token: string): void {
    // Clear any existing timeout
    if (this.refreshTimeoutId) {
      clearTimeout(this.refreshTimeoutId);
    }

    try {
      const decoded = jwtDecode<JwtPayload>(token);
      if (!decoded.exp) return;

      const expiresAt = decoded.exp * 1000; // Convert to milliseconds
      const now = Date.now();
      const timeUntilExpiry = expiresAt - now;

      // Refresh 5 minutes (300,000 ms) before expiry
      const refreshIn = timeUntilExpiry - 5 * 60 * 1000;

      if (refreshIn > 0) {
        console.log(
          `🔄 Token refresh scheduled in ${Math.floor(refreshIn / 1000 / 60)} minutes`
        );

        this.refreshTimeoutId = window.setTimeout(async () => {
          console.log("🔄 Auto-refreshing token...");
          await this.refreshAccessToken();
        }, refreshIn);
      } else {
        // Token expires in less than 5 minutes, refresh immediately
        console.log("⚠️ Token expiring soon, refreshing now...");
        this.refreshAccessToken();
      }
    } catch (error) {
      console.error("❌ Failed to schedule token refresh:", error);
    }
  }

  /**
   * Check if token is expired
   */
  private isTokenExpired(token: string): boolean {
    try {
      const decoded = jwtDecode<JwtPayload>(token);
      if (!decoded.exp) return true;

      const currentTime = Date.now() / 1000;
      return decoded.exp < currentTime;
    } catch {
      return true;
    }
  }
}

export const tokenManager = new TokenManager();
