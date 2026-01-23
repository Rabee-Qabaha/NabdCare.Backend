/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { Clinic } from "./clinic";
import { Role } from "./role";
import { UserPermission } from "./user-permission";
import { PractitionerSchedule } from "./practitioner-schedule";
import { Appointment } from "./appointment";

export class User {
  clinicId: string;
  clinic: Clinic;
  email: string = "";
  passwordHash: string = "";
  fullName: string = "";
  roleId: string;
  role: Role;
  isActive: boolean = true;
  createdByUserId: string;
  createdByUser: User;
  permissions: UserPermission[] = [];
  schedules: PractitionerSchedule[] = [];
  appointments: Appointment[] = [];
}
