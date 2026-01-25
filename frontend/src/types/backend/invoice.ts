/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { Clinic } from "./clinic";
import { Subscription } from "./subscription";
import { Patient } from "./patient";
import { ClinicalEncounter } from "./clinical-encounter";
import { InvoiceStatus } from "./invoice-status";
import { InvoiceType } from "./invoice-type";
import { InvoiceItem } from "./invoice-item";
import { Payment } from "./payment";

export class Invoice {
  invoiceNumber: string = "";
  idempotencyKey: string;
  clinicId: string;
  clinic: Clinic;
  subscriptionId: string;
  subscription: Subscription;
  patientId: string;
  patient: Patient;
  clinicalEncounterId: string;
  clinicalEncounter: ClinicalEncounter;
  billedToName: string = "";
  billedToAddress: string;
  billedToTaxNumber: string;
  pdfUrl: string;
  hostedPaymentUrl: string;
  issueDate: Date = new Date("2026-01-25T21:15:25.1109100Z");
  dueDate: Date;
  paidDate: Date;
  paymentAttemptCount: number;
  nextPaymentAttempt: Date;
  status: InvoiceStatus;
  type: InvoiceType;
  currency: string = "USD";
  subTotal: number;
  taxRate: number;
  taxAmount: number;
  totalAmount: number;
  paidAmount: number;
  balanceDue: number;
  items: InvoiceItem[] = [];
  payments: Payment[] = [];
}
