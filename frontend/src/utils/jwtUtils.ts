// src/utils/jwtUtils.ts
import { jwtDecode, type JwtPayload } from 'jwt-decode';

export interface UserInfo extends JwtPayload {
  sub?: string;
  email?: string;
  name?: string;
  role?: string;
  clinicId?: string;
  roleId?: string;
  jti?: string;
}

/**
 * Decode and extract user details from JWT
 * ✅ Uses clean claim names coming from backend
 */
export function getUserFromToken(token: string): UserInfo | null {
  try {
    const decoded = jwtDecode<UserInfo>(token);

    const normalized: UserInfo = {
      ...decoded,
      sub: decoded.sub,
      email: decoded.email,
      name: decoded.name,
      role: decoded.role,
      clinicId: decoded.clinicId,
      roleId: decoded.roleId,
    };

    return normalized;
  } catch (err) {
    console.error('❌ Invalid token:', err);
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

    const now = Date.now() / 1000;
    return decoded.exp < now;
  } catch {
    return true;
  }
}

/**
 * Get expiration timestamp (ms)
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
 * Get ms until expiry
 */
export function getTimeUntilExpiry(token: string): number | null {
  const expiresAt = getTokenExpiryTime(token);
  if (!expiresAt) return null;

  const remaining = expiresAt - Date.now();
  return remaining > 0 ? remaining : 0;
}
