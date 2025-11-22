// src/utils/errorHandler.ts
/**
 * Error Handler Utility
 * Location: src/utils/errorHandler.ts
 *
 * Extracts error code and message from backend response.
 * Uses backend message directly - minimal approach.
 *
 * User: Rabee-Qabaha
 * Updated: 2025-11-08 18:58:40
 */

import type { ApiErrorResponse, UIError } from '@/types/errors';
import { isAuthError, isAxiosError } from '@/types/errors';

/**
 * Fallback messages (only when backend doesn't send message)
 * Minimal set - only for critical cases
 */
const FALLBACK_MESSAGES: Record<string, string> = {
  UNAUTHORIZED: 'Unauthorized. Please log in.',
  FORBIDDEN: 'Access denied.',
  NOT_FOUND: 'Resource not found.',
  INVALID_REQUEST: 'Invalid request.',
  INTERNAL_ERROR: 'An error occurred. Please try again.',
  CONFLICT: 'This action conflicts with existing data.',
  TOO_MANY_REQUESTS: 'Too many requests. Please wait.',
};

/**
 * Process raw error into UIError format
 * Extracts backend message and uses it directly
 */
export function handleError(error: any): UIError {
  let code = 'INTERNAL_ERROR';
  let message = 'An unexpected error occurred';
  let httpStatus: number | undefined;
  let details: Record<string, any> | undefined;

  // ✅ Handle Axios error (most common)
  if (isAxiosError(error)) {
    httpStatus = error.response?.status;
    const responseData = error.response?.data as ApiErrorResponse | undefined;

    // ✅ PRIMARY: Extract from error object
    if (responseData?.error?.code) {
      code = responseData.error.code;
      // ✅ USE BACKEND MESSAGE DIRECTLY
      message = responseData.error.message || FALLBACK_MESSAGES[code] || 'An error occurred';
      details = responseData.error.details;
    }
    // ✅ Fallback: Try alternate structure
    else if (responseData?.code) {
      code = responseData.code;
      message = responseData.message || FALLBACK_MESSAGES[code] || 'An error occurred';
    }
    // ✅ No error code - map HTTP status
    else {
      code = mapHttpStatusToCode(httpStatus);
      message = error.message || FALLBACK_MESSAGES[code] || 'An error occurred';
    }
  }
  // ✅ Handle plain error object
  else if (error?.code) {
    code = error.code;
    message = error.message || FALLBACK_MESSAGES[code] || message;
    details = error.details;
  }
  // ✅ Handle Error instance
  else if (error instanceof Error) {
    message = error.message;
  }
  // ✅ Handle string error
  else if (typeof error === 'string') {
    message = error;
  }

  return {
    code,
    message, // ✅ Backend message or fallback
    httpStatus,
    isAuthError: isAuthError(code),
    details,
  };
}

/**
 * Map HTTP status code to error code (fallback only)
 * Used when backend doesn't send error code
 */
function mapHttpStatusToCode(status: number | undefined): string {
  if (!status) return 'INTERNAL_ERROR';

  switch (status) {
    case 400:
      return 'INVALID_REQUEST';
    case 401:
      return 'UNAUTHORIZED';
    case 403:
      return 'FORBIDDEN';
    case 404:
      return 'NOT_FOUND';
    case 409:
      return 'CONFLICT';
    case 429:
      return 'TOO_MANY_REQUESTS';
    case 500:
      return 'INTERNAL_ERROR';
    case 503:
      return 'INTERNAL_ERROR';
    default:
      return 'INTERNAL_ERROR';
  }
}

/**
 * Extract field-level validation errors
 * Useful for form validation
 */
export function getFieldErrors(error: any): Record<string, string> {
  if (!isAxiosError(error)) return {};

  const details = error.response?.data?.error?.details;
  if (!details || typeof details !== 'object') return {};

  const fieldErrors: Record<string, string> = {};

  for (const [field, value] of Object.entries(details)) {
    if (typeof value === 'string') {
      fieldErrors[field] = value;
    } else if (Array.isArray(value) && value.length > 0) {
      fieldErrors[field] = String(value[0]);
    }
  }

  return fieldErrors;
}
