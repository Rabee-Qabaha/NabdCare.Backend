/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { PaymentMethod } from "./payment-method";

export class PaymentAllocationDto {
  id: string;
  paymentId: string;
  invoiceId: string;
  invoiceNumber: string = "";
  amount: number;
  allocationDate: Date;
  paymentMethod: PaymentMethod;
  reference: string;
  paymentDate: Date;
}
