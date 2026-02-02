/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { InvoiceStatus } from "./invoice-status";
import { InvoiceType } from "./invoice-type";
import { InvoiceItemDto } from "./invoice-item-dto";
import { PaymentAllocationDto } from "./payment-allocation-dto";

export class InvoiceDto {
  id: string;
  invoiceNumber: string = "";
  clinicId: string;
  subscriptionId: string;
  billedToName: string = "";
  billedToAddress: string;
  billedToTaxNumber: string;
  issueDate: Date;
  dueDate: Date;
  paidDate: Date;
  status: InvoiceStatus;
  type: InvoiceType;
  pdfUrl: string;
  hostedPaymentUrl: string;
  currency: string = "";
  subTotal: number;
  taxAmount: number;
  totalAmount: number;
  paidAmount: number;
  balanceDue: number;
  items: InvoiceItemDto[] = [];
  payments: PaymentAllocationDto[] = [];
}
