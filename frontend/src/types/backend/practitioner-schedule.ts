/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { Clinic } from "./clinic";
import { User } from "./user";
import { DayOfWeek } from "./day-of-week";

export class PractitionerSchedule {
  clinicId: string;
  clinic: Clinic;
  userId: string;
  user: User;
  dayOfWeek: DayOfWeek;
  startTime: string;
  endTime: string;
  allowOnlineBooking: boolean = true;
  label: string;
}
