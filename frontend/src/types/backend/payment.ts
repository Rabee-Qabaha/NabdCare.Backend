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
import { PaymentAllocation } from "./payment-allocation";

export class Payment {
  context: PaymentContext;
  clinicId: string;
  clinic: Clinic;
  patientId: string;
  patient: Patient;
  amount: number;
  refundedAmount: number;
  paymentDate: Date = new Date("2026-02-08T20:18:39.2727170Z");
  method: PaymentMethod;
  status: PaymentStatus;
  transactionId: string;
  notes: string;
  chequeDetail: ChequePaymentDetail;
  allocations: PaymentAllocation[] = [];
  unallocatedAmount: number;
}
