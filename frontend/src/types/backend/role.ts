/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { Clinic } from './clinic';
import { RolePermission } from './role-permission';
import { User } from './user';

export class Role {
  name: string = '';
  description: string;
  clinicId: string;
  clinic: Clinic;
  isSystemRole: boolean;
  isTemplate: boolean;
  templateRoleId: string;
  templateRole: Role;
  displayOrder: number = 100;
  colorCode: string;
  iconClass: string;
  rolePermissions: RolePermission[] = [];
  users: User[] = [];
}
