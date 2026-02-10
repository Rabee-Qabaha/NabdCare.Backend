/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { CreatePaymentRequestDto } from "./create-payment-request-dto";
import { PaymentAllocationRequestDto } from "./payment-allocation-request-dto";

export class BatchPaymentRequestDto {
  clinicId: string;
  patientId: string;
  payments: CreatePaymentRequestDto[] = [];
  invoicesToPay: PaymentAllocationRequestDto[] = [];
}
