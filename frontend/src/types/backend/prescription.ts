/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { Clinic } from "./clinic";
import { Patient } from "./patient";
import { User } from "./user";
import { ClinicalEncounter } from "./clinical-encounter";

export class Prescription {
  clinicId: string;
  clinic: Clinic;
  patientId: string;
  patient: Patient;
  doctorId: string;
  doctor: User;
  clinicalEncounterId: string;
  clinicalEncounter: ClinicalEncounter;
  medicationName: string = "";
  dosage: string = "";
  frequency: string = "";
  duration: string = "";
  instructions: string;
}
