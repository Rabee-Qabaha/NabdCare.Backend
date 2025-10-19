/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { UserRole } from "./user-role";

export class CreateUserRequestDto {
  email: string = "";
  password: string = "";
  fullName: string = "";
  role: UserRole;
  clinicId: string;
}
