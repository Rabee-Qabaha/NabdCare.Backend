/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { Payment } from "./payment";
import { Invoice } from "./invoice";

export class PaymentAllocation {
  paymentId: string;
  payment: Payment;
  invoiceId: string;
  invoice: Invoice;
  amount: number;
  allocationDate: Date = new Date("2026-02-02T21:24:33.3092730Z");
}
