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
  roleId: string;
  role: Role;
  email: string = "";
  passwordHash: string = "";
  isActive: boolean = true;
  lastLoginAt: Date;
  fullName: string = "";
  jobTitle: string;
  phoneNumber: string;
  address: string;
  profilePictureUrl: string;
  bio: string;
  licenseNumber: string;
  createdByUserId: string;
  createdByUser: User;
  permissions: UserPermission[] = [];
  schedules: PractitionerSchedule[] = [];
  appointments: Appointment[] = [];
}
