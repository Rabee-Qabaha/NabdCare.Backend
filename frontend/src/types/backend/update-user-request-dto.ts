/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { UserRole } from "./user-role";

export class UpdateUserRequestDto {
  fullName: string = "";
  isActive: boolean = true;
  role: UserRole;
}
