/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { Clinic } from "./clinic";
import { Patient } from "./patient";
import { User } from "./user";
import { Branch } from "./branch";
import { AppointmentStatus } from "./appointment-status";
import { AppointmentType } from "./appointment-type";

export class Appointment {
  clinicId: string;
  clinic: Clinic;
  patientId: string;
  patient: Patient;
  doctorId: string;
  doctor: User;
  branchId: string;
  branch: Branch;
  startDateTime: Date;
  endDateTime: Date;
  status: AppointmentStatus;
  type: AppointmentType;
  reasonForVisit: string;
  cancellationReason: string;
  clinicalEncounterId: string;
}
