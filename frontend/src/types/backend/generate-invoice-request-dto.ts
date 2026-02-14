/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { InvoiceType } from "./invoice-type";
import { Currency } from "./currency";
import { GenerateInvoiceItemDto } from "./generate-invoice-item-dto";

export class GenerateInvoiceRequestDto {
  clinicId: string;
  subscriptionId: string;
  type: InvoiceType;
  currency: Currency;
  idempotencyKey: string;
  issueDate: Date = new Date("2026-02-14T21:00:46.3392590Z");
  dueDate: Date;
  items: GenerateInvoiceItemDto[] = [];
  taxRate: number;
}
