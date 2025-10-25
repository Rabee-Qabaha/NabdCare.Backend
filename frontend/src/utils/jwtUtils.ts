import { jwtDecode, type JwtPayload } from "jwt-decode";

export interface UserInfo extends JwtPayload {
  sub?: string;
  email?: string;
  role?: string;
  clinicId?: string;
  fullName?: string;
  jti?: string;

  // ✅ Support for .NET claim names (what backend actually sends)
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"?: string;
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"?: string;
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"?: string;
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"?: string;
  ClinicId?: string;
  RoleId?: string;
}

/**
 * Decode and extract user info from access token
 * ✅ Normalizes .NET claim names to simple property names
 */
export function getUserFromToken(token: string): UserInfo | null {
  try {
    const decoded = jwtDecode<UserInfo>(token);

    // ✅ CRITICAL: Normalize .NET claim names to simple properties
    const normalized: UserInfo = {
      ...decoded,

      // Extract user ID
      sub:
        decoded.sub ||
        decoded[
          "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
        ],

      // Extract email
      email:
        decoded.email ||
        decoded[
          "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"
        ],

      // Extract full name
      fullName:
        decoded.fullName ||
        decoded["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"],

      // ✅ THIS IS THE KEY FIX: Extract role from .NET claim
      role:
        decoded.role ||
        decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"],

      // Extract clinic ID (backend sends "ClinicId" with capital C)
      clinicId: decoded.clinicId || decoded["ClinicId"],
    };

    console.log("🔍 JWT Decoded:", {
      raw: decoded,
      normalized: {
        email: normalized.email,
        role: normalized.role,
        fullName: normalized.fullName,
        clinicId: normalized.clinicId,
      },
    });

    return normalized;
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
