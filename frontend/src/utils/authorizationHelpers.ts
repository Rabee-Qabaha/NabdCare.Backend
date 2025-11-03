import type { AuthorizationAction } from "@/types/authorization";
import type { AuthorizationResultDto } from "@/types/backend";

/**
 * Authorization Helper Utilities
 * Location: src/utils/authorizationHelpers.ts
 *
 * Provides common patterns for authorization checks, error handling, and logging
 */

/**
 * Get user-friendly error message from authorization denial reason
 */
export function getAuthorizationErrorMessage(reason?: string): string {
  if (!reason) return "Access denied";

  const messageMap: Record<string, string> = {
    "different clinic": "You don't have access to resources from other clinics",
    "not found": "This resource no longer exists or has been deleted",
    missing: "You don't have the required permission for this action",
    inactive: "This resource is no longer active",
    policy: "Your access level doesn't allow this action",
  };

  for (const [key, message] of Object.entries(messageMap)) {
    if (reason.toLowerCase().includes(key)) {
      return message;
    }
  }

  return reason;
}

/**
 * Check if error is authorization-related
 */
export function isAuthorizationError(result: AuthorizationResultDto): boolean {
  return !result.allowed;
}

/**
 * Get icon for authorization status
 */
export function getAuthorizationIcon(result: AuthorizationResultDto): string {
  if (result.allowed) return "pi pi-check-circle";
  return "pi pi-times-circle";
}

/**
 * Get severity for authorization status (for PrimeVue components)
 */
export function getAuthorizationSeverity(
  result: AuthorizationResultDto
): "success" | "error" {
  return result.allowed ? "success" : "error";
}

/**
 * Create authorization check key for caching/tracking
 */
export function createAuthorizationCheckKey(
  resourceType: string,
  resourceId: string,
  action: string
): string {
  return `${resourceType}:${resourceId}:${action}`;
}

/**
 * Parse authorization check key back to components
 */
export function parseAuthorizationCheckKey(
  key: string
): { resourceType: string; resourceId: string; action: string } | null {
  const parts = key.split(":");
  if (parts.length !== 3) return null;

  return {
    resourceType: parts[0],
    resourceId: parts[1],
    action: parts[2],
  };
}

/**
 * Check if user has permission for specific action on resource
 */
export function canPerformAction(
  result: AuthorizationResultDto,
  action: AuthorizationAction
): boolean {
  if (!result.allowed) return false;
  if (result.action !== action) return false;
  return true;
}

/**
 * Get retry strategy based on error reason
 */
export function shouldRetryAuthorizationCheck(reason?: string): boolean {
  if (!reason) return true;

  // Don't retry on resource not found
  if (reason.toLowerCase().includes("not found")) return false;

  // Don't retry on permission denied
  if (reason.toLowerCase().includes("permission")) return false;

  // Retry on other errors (network, etc.)
  return true;
}

/**
 * Log authorization check for audit/debugging
 */
export function logAuthorizationCheck(
  resourceType: string,
  resourceId: string,
  action: string,
  result: AuthorizationResultDto
): void {
  const status = result.allowed ? "✅ ALLOWED" : "❌ DENIED";
  const policy = result.policy ? ` (${result.policy})` : "";
  const reason = result.reason ? ` - ${result.reason}` : "";

  console.log(
    `[AUTH] ${status} ${resourceType}:${resourceId}:${action}${policy}${reason}`
  );
}
