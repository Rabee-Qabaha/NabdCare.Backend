/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { PaymentContext } from "./payment-context";
import { PaymentMethod } from "./payment-method";
import { PaymentAllocationRequestDto } from "./payment-allocation-request-dto";
import { CreateChequeDetailDto } from "./create-cheque-detail-dto";

export class CreatePaymentRequestDto {
  context: PaymentContext;
  clinicId: string;
  patientId: string;
  amount: number;
  paymentDate: Date = new Date("2026-02-07T21:25:21.2147540Z");
  method: PaymentMethod;
  transactionId: string;
  notes: string;
  allocations: PaymentAllocationRequestDto[] = [];
  chequeDetail: CreateChequeDetailDto;
}
