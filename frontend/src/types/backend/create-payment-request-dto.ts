/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { PaymentContext } from "./payment-context";
import { Currency } from "./currency";
import { PaymentMethod } from "./payment-method";
import { PaymentAllocationRequestDto } from "./payment-allocation-request-dto";
import { CreateChequeDetailDto } from "./create-cheque-detail-dto";

export class CreatePaymentRequestDto {
  context: PaymentContext;
  clinicId: string;
  patientId: string;
  amount: number;
  currency: Currency;
  paymentDate: Date = new Date("2026-02-14T21:00:46.3303040Z");
  method: PaymentMethod;
  transactionId: string;
  notes: string;
  allocations: PaymentAllocationRequestDto[] = [];
  chequeDetail: CreateChequeDetailDto;
  baseExchangeRate: number;
  finalExchangeRate: number;
}
