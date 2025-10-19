/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { UserRole } from "./user-role";

export class UserResponseDto {
  id: string;
  email: string = "";
  fullName: string = "";
  role: UserRole;
  isActive: boolean;
  clinicId: string;
  clinicName: string;
}
