/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { InvoiceItemType } from "./invoice-item-type";

export class GenerateInvoiceItemDto {
  description: string = "";
  note: string;
  type: InvoiceItemType;
  quantity: number;
  unitPrice: number;
  periodStart: Date;
  periodEnd: Date;
}
