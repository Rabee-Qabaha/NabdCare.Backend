/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { Invoice } from "./invoice";
import { Product } from "./product";
import { InvoiceItemType } from "./invoice-item-type";

export class InvoiceItem {
  invoiceId: string;
  invoice: Invoice;
  productId: string;
  product: Product;
  description: string = "";
  note: string;
  type: InvoiceItemType;
  quantity: number;
  unitPrice: number;
  total: number;
  periodStart: Date;
  periodEnd: Date;
}
