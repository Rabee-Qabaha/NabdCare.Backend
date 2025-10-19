// import { jwtDecode, type JwtPayload } from "jwt-decode";

// export interface UserInfo extends JwtPayload {
//   sub: string;
//   email: string;
//   role?: string;
//   clinicId?: string;
//   fullName?: string;
//   // This covers the Microsoft-style role claim
//   "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"?: string;
// }

// /**
//  * Decode and extract user info from access token
//  */
// export function getUserFromToken(token: string): UserInfo | null {
//   try {
//     const decoded = jwtDecode<UserInfo>(token);

//     // Normalize the role so it’s always available as `role`
//     if (
//       decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] &&
//       !decoded.role
//     ) {
//       decoded.role =
//         decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];
//     }

//     // Normalize clinicId casing
//     if ((decoded as any).ClinicId && !decoded.clinicId) {
//       decoded.clinicId = (decoded as any).ClinicId;
//     }

//     return decoded;
//   } catch (err) {
//     console.error("Invalid token:", err);
//     return null;
//   }
// }

// /**
//  * Check if JWT token is expired
//  */
// export function isTokenExpired(token: string): boolean {
//   try {
//     const decoded = jwtDecode<JwtPayload>(token);
//     if (!decoded.exp) return true;

//     const currentTime = Date.now() / 1000;
//     return decoded.exp < currentTime;
//   } catch {
//     return true;
//   }
// }

// src/utils/jwtUtils.ts
import { jwtDecode, type JwtPayload } from "jwt-decode";

export interface UserInfo extends JwtPayload {
  sub: string;
  email: string;
  role?: string;
  clinicId?: string;
  fullName?: string;
}

/**
 * Decode and extract user info from access token
 */
export function getUserFromToken(token: string): UserInfo | null {
  try {
    const decoded = jwtDecode<UserInfo>(token);

    // Normalize clinicId casing
    if ((decoded as any).ClinicId && !decoded.clinicId) {
      decoded.clinicId = (decoded as any).ClinicId;
    }

    return decoded;
  } catch (err) {
    console.error("Invalid token:", err);
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
    return decoded.exp < currentTime;
  } catch {
    return true;
  }
}
