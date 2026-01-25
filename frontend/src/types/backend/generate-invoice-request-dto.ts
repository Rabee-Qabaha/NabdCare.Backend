/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { InvoiceType } from "./invoice-type";
import { GenerateInvoiceItemDto } from "./generate-invoice-item-dto";

export class GenerateInvoiceRequestDto {
  clinicId: string;
  subscriptionId: string;
  type: InvoiceType;
  currency: string = "USD";
  idempotencyKey: string;
  issueDate: Date = new Date("2026-01-25T21:15:25.0607540Z");
  dueDate: Date;
  items: GenerateInvoiceItemDto[] = [];
  taxRate: number;
}
