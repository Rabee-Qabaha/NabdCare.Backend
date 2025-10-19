// src/utils/jwtUtils.ts
import { jwtDecode, type JwtPayload } from "jwt-decode";

export interface UserInfo extends JwtPayload {
  sub: string;
  email: string;
  role?: string;
  clinicId?: string;
  fullName?: string;
  jti?: string; // ✅ Unique token ID
}

/**
 * Decode and extract user info from access token
 */
export function getUserFromToken(token: string): UserInfo | null {
  try {
    const decoded = jwtDecode<UserInfo>(token);

    // ✅ Normalize clinicId casing (backend sends "ClinicId")
    if ((decoded as any).ClinicId && !decoded.clinicId) {
      decoded.clinicId = (decoded as any).ClinicId;
    }

    return decoded;
  } catch (err) {
    console.error("❌ Invalid token:", err);
    return null;
  }
}

/**
 * Check if JWT token is expired
 */
export function isTokenExpired(token: string): boolean {
  try {
    const decoded = jwtDecode<JwtPayload>(token);
    if (!decoded.exp) return true;

    const currentTime = Date.now() / 1000;

    // ✅ Consider clock skew (5 seconds buffer)
    return decoded.exp < currentTime + 5;
  } catch {
    return true;
  }
}

/**
 * Get token expiry time in milliseconds
 */
export function getTokenExpiryTime(token: string): number | null {
  try {
    const decoded = jwtDecode<JwtPayload>(token);
    return decoded.exp ? decoded.exp * 1000 : null;
  } catch {
    return null;
  }
}

/**
 * Get time until token expires (in milliseconds)
 */
export function getTimeUntilExpiry(token: string): number | null {
  const expiryTime = getTokenExpiryTime(token);
  if (!expiryTime) return null;

  const timeUntil = expiryTime - Date.now();
  return timeUntil > 0 ? timeUntil : 0;
}
