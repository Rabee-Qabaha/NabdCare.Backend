/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { PaymentContext } from "./payment-context";
import { Clinic } from "./clinic";
import { Patient } from "./patient";
import { PaymentMethod } from "./payment-method";
import { PaymentStatus } from "./payment-status";
import { ChequePaymentDetail } from "./cheque-payment-detail";

export class Payment {
  context: PaymentContext;
  clinicId: string;
  clinic: Clinic;
  patientId: string;
  patient: Patient;
  amount: number;
  paymentDate: Date = new Date("2026-01-10T16:18:47.2685200Z");
  method: PaymentMethod;
  status: PaymentStatus;
  chequeDetail: ChequePaymentDetail;
}
