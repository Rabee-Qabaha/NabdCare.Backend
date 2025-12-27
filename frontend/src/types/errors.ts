// Path: src/types/errors.ts
/**
 * Error Types
 * Location: src/types/errors.ts
 *
 * Minimal type definitions.
 * User: Rabee-Qabaha
 * Updated: 2025-11-08 18:58:40
 */

import type { AxiosError } from 'axios';

/**
 * Backend API error response structure
 */
export interface ApiErrorResponse {
  error?: {
    code: string;
    message: string;
    details?: Record<string, any>;
  };
  message?: string;
  code?: string;
}

/**
 * Processed error for UI display
 */
export interface UIError {
  code: string;
  message: string; // Backend message or fallback
  httpStatus?: number;
  isAuthError: boolean;
  details?: Record<string, any>;
}

/**
 * Type guard: Check if error is Axios error
 */
export function isAxiosError(error: any): error is AxiosError<ApiErrorResponse> {
  return error?.response !== undefined && error?.config !== undefined;
}

/**
 * Type guard: Check if error code is auth-related
 */
export function isAuthError(code: string): boolean {
  return [
    'UNAUTHORIZED',
    'SESSION_EXPIRED',
    'TOKEN_INVALID',
    'INVALID_CREDENTIALS',
    'AUTHENTICATION_FAILED',
    'SECURITY_VIOLATION',
    'TOKEN_REUSE_DETECTED',
  ].includes(code);
}
