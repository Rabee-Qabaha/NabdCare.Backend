/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { PaginationRequestDto } from "./pagination-request-dto";
import { InvoiceStatus } from "./invoice-status";
import { InvoiceType } from "./invoice-type";
import { Currency } from "./currency";

export class InvoiceListRequestDto extends PaginationRequestDto {
  clinicId: string;
  subscriptionId: string;
  invoiceNumber: string;
  status: InvoiceStatus;
  type: InvoiceType;
  currency: Currency;
  fromDate: Date;
  toDate: Date;
}
