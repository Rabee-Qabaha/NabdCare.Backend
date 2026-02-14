/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { PaymentContext } from "./payment-context";
import { Currency } from "./currency";
import { PaymentMethod } from "./payment-method";
import { PaymentStatus } from "./payment-status";
import { ChequePaymentDetailDto } from "./cheque-payment-detail-dto";
import { PaymentAllocationDto } from "./payment-allocation-dto";

export class PaymentDto {
  id: string;
  context: PaymentContext;
  clinicId: string;
  patientId: string;
  amount: number;
  currency: Currency;
  baseExchangeRate: number;
  finalExchangeRate: number;
  amountInFunctionalCurrency: number;
  unallocatedAmount: number;
  paymentDate: Date;
  method: PaymentMethod;
  status: PaymentStatus;
  transactionId: string;
  notes: string;
  chequeDetail: ChequePaymentDetailDto;
  allocations: PaymentAllocationDto[] = [];
}
