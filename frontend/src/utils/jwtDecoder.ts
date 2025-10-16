// src/utils/jwtDecoder.ts
import { jwtDecode } from "jwt-decode";

export interface JwtPayload {
  sub: string;
  email: string;
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": string;
  ClinicId?: string;
  exp?: number;
  iss?: string;
  aud?: string;
}

export function parseJwt(token: string): JwtPayload | null {
  try {
    return jwtDecode<JwtPayload>(token);
  } catch (error) {
    console.error("Failed to parse JWT:", error);
    return null;
  }
}
export { jwtDecode };
