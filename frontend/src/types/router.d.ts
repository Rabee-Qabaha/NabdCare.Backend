import type { RouteMeta } from 'vue-router';
import type { ResourceType, AuthorizationAction } from '@/types/authorization';

/**
 * Extended Vue Router Meta Types
 * Location: src/types/router.d.ts
 *
 * Extends Vue Router's RouteMeta to include ABAC and enhanced RBAC metadata
 *
 * Author: Rabee Qabaha
 * Updated: 2025-11-02
 */

declare module 'vue-router' {
  interface RouteMeta {
    /**
     * Page title for document.title and breadcrumbs
     */
    title?: string;

    /**
     * Whether route is public (no authentication required)
     */
    public?: boolean;

    /**
     * Requires authentication to access
     */
    requiresAuth?: boolean;

    /**
     * Required permission(s) for RBAC check
     * Can be a single permission string or array of permissions
     * User must have at least one permission to access
     */
    permission?: string | string[];

    /**
     * Alias for 'permission' - same functionality
     */
    permissions?: string | string[];

    /**
     * Required role(s) for RBAC check
     * User must have one of these roles
     */
    roles?: string | string[];

    /**
     * Route level: defines scope of access
     * - "system": SuperAdmin only (System.ManageSettings permission)
     * - "clinic": Clinic users only (requires clinic context)
     */
    level?: 'system' | 'clinic';

    /**
     * Resource type for ABAC (Attribute-Based Access Control) check
     *
     * Supported values:
     * - "user": User resource
     * - "clinic": Clinic resource
     * - "role": Role resource
     * - "subscription": Subscription resource
     * - "patient": Patient resource
     * - "payment": Payment resource
     * - "medicalrecord": Medical record resource
     * - "appointment": Appointment resource
     * - null: No ABAC check (for list views, dashboards)
     *
     * Example use case:
     * When accessing /users/:id, set abacResource to "user"
     * to check if user can access the specific user resource
     */
    abacResource?: ResourceType | null;

    /**
     * Route parameter name containing the resource ID
     *
     * This tells the router guard which param contains the resource ID
     *
     * Example:
     * - Route: /users/:id → abacResourceIdParam: "id"
     * - Route: /patients/:patientId → abacResourceIdParam: "patientId"
     * - Route: /clinics/:clinicId/edit → abacResourceIdParam: "clinicId"
     */
    abacResourceIdParam?: string;

    /**
     * Action to check in ABAC policy
     *
     * Determines what operation is being checked
     * - "view": Read-only access (default)
     * - "edit": Modification access
     * - "delete": Deletion access
     * - "create": Creation access
     *
     * Default: "view"
     *
     * Example:
     * - /users/:id → abacAction: "view" (just viewing)
     * - /users/:id/edit → abacAction: "edit" (editing)
     * - /users/:id/delete → abacAction: "delete" (deleting)
     */
    abacAction?: AuthorizationAction;
  }
}
