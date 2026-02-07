/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { PaginationRequestDto } from "./pagination-request-dto";
import { PaymentMethod } from "./payment-method";
import { PaymentStatus } from "./payment-status";

export class PaymentFilterRequestDto extends PaginationRequestDto {
  method: PaymentMethod;
  status: PaymentStatus;
  startDate: Date;
  endDate: Date;
  reference: string;
}
