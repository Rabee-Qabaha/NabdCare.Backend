/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { Clinic } from "./clinic";
import { UserRole } from "./user-role";
import { UserPermission } from "./user-permission";

export class User {
  clinicId: string;
  clinic: Clinic;
  email: string = "";
  passwordHash: string = "";
  fullName: string = "";
  role: UserRole;
  isActive: boolean = true;
  createdByUserId: string;
  createdByUser: User;
  permissions: UserPermission[] = [];
}
