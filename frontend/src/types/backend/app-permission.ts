/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { RolePermission } from "./role-permission";
import { UserPermission } from "./user-permission";

export class AppPermission {
  name: string = "";
  description: string;
  rolePermissions: RolePermission[] = [];
  userPermissions: UserPermission[] = [];
}
