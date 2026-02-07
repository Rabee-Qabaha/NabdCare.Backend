/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

export class PaymentAllocationDto {
  id: string;
  paymentId: string;
  invoiceId: string;
  invoiceNumber: string = '';
  amount: number;
  allocationDate: Date;
}
