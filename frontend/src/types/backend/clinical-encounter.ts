/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { Clinic } from "./clinic";
import { Patient } from "./patient";
import { User } from "./user";
import { Appointment } from "./appointment";
import { EncounterStatus } from "./encounter-status";
import { Prescription } from "./prescription";

export class ClinicalEncounter {
  clinicId: string;
  clinic: Clinic;
  patientId: string;
  patient: Patient;
  doctorId: string;
  doctor: User;
  appointmentId: string;
  appointment: Appointment;
  date: Date;
  chiefComplaint: string;
  diagnosis: string;
  treatmentPlan: string;
  specialtyData: string;
  vitalsSnapshot: string;
  status: EncounterStatus;
  prescriptions: Prescription[] = [];
}
