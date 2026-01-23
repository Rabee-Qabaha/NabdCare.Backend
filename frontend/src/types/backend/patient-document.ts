/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { Clinic } from "./clinic";
import { Patient } from "./patient";
import { DocumentType } from "./document-type";

export class PatientDocument {
  clinicId: string;
  clinic: Clinic;
  patientId: string;
  patient: Patient;
  title: string = "";
  type: DocumentType;
  storageUrl: string = "";
  fileType: string = "";
  sizeInBytes: number;
}
