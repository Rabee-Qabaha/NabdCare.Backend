import type {
  AuthorizationCheckRequestDto,
  AuthorizationResultDto,
} from "@/types/backend";

/**
 * Local Authorization Types
 * Location: src/types/authorization.ts
 *
 * Note: AuthorizationCheckRequestDto and AuthorizationResultDto are auto-generated
 * from backend via TypeGen and exported from @/types/backend
 */

/**
 * Enhanced authorization result with helper methods
 */
export interface AuthorizationCheck extends AuthorizationResultDto {
  /**
   * Whether access was denied
   */
  isDenied: boolean;

  /**
   * Whether check is still pending
   */
  isLoading: boolean;

  /**
   * User-friendly error message
   */
  errorMessage?: string;
}

/**
 * Resource-level authorization context
 */
export interface ResourceAuthContext {
  resourceType: string;
  resourceId: string;
  canView: boolean;
  canEdit: boolean;
  canDelete: boolean;
  canCreate: boolean;
  denialReason?: string;
}

/**
 * Supported resource types for authorization
 */
export type ResourceType =
  | "user"
  | "clinic"
  | "role"
  | "subscription"
  | "patient"
  | "payment"
  | "medicalrecord"
  | "appointment";

/**
 * Supported authorization actions
 */
export type AuthorizationAction = "view" | "edit" | "delete" | "create";

/**
 * Request for checking multiple authorizations
 */
export interface BatchAuthorizationCheckRequest {
  checks: AuthorizationCheckRequestDto[];
}

/**
 * Response for batch authorization checks
 */
export interface BatchAuthorizationCheckResponse {
  results: AuthorizationResultDto[];
  failedCount: number;
}
