/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { Clinic } from "./clinic";
import { Gender } from "./gender";
import { BloodType } from "./blood-type";
import { Appointment } from "./appointment";
import { ClinicalEncounter } from "./clinical-encounter";
import { PatientDocument } from "./patient-document";
import { Invoice } from "./invoice";

export class Patient {
  clinicId: string;
  clinic: Clinic;
  medicalRecordNumber: string = "";
  firstName: string = "";
  lastName: string = "";
  fullName: string = " ";
  dateOfBirth: Date;
  gender: Gender;
  bloodType: BloodType;
  phoneNumber: string;
  email: string;
  address: string;
  hasAllergies: boolean;
  hasChronicConditions: boolean;
  medicalAlertNote: string;
  appointments: Appointment[] = [];
  encounters: ClinicalEncounter[] = [];
  documents: PatientDocument[] = [];
  invoices: Invoice[] = [];
}
