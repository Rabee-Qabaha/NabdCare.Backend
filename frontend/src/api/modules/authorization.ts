import { api } from '@/api/apiClient';
import type { AuthorizationCheckRequestDto, AuthorizationResultDto } from '@/types/backend';

/**
 * Authorization API Module
 * Handles resource-level authorization checks (ABAC)
 *
 * Location: src/api/modules/authorization.ts
 * Pattern: Follows existing permissions.ts, users.ts structure
 *
 * Author: Rabee Qabaha
 * Updated: 2025-11-02
 */

/**
 * Check if current user is authorized to perform an action on a specific resource
 *
 * @param resourceType - Type of resource ("user", "clinic", "role", "subscription", etc.)
 * @param resourceId - Resource ID (GUID as string)
 * @param action - Action to check ("view", "edit", "delete", "create")
 * @returns Authorization result with allowed status, reason, and policy name
 */
export async function checkResourceAuthorization(
  resourceType: string,
  resourceId: string,
  action: string,
): Promise<AuthorizationResultDto> {
  const request: AuthorizationCheckRequestDto = {
    resourceType,
    resourceId,
    action,
  };

  const response = await api.post<AuthorizationResultDto>('/authorization/check', request);

  return response.data;
}

/**
 * Batch check authorization for multiple resources
 * Useful for filtering lists of resources
 *
 * @param checks - Array of authorization checks
 * @returns Array of authorization results
 */
export async function checkMultipleResourceAuthorizations(
  checks: AuthorizationCheckRequestDto[],
): Promise<AuthorizationResultDto[]> {
  const results = await Promise.all(
    checks.map((check) =>
      checkResourceAuthorization(check.resourceType, check.resourceId, check.action).catch(
        (err) => ({
          allowed: false,
          reason: `Authorization check failed: ${err.message}`,
          policy: null,
          resourceType: check.resourceType,
          action: check.action,
        }),
      ),
    ),
  );

  return results;
}
