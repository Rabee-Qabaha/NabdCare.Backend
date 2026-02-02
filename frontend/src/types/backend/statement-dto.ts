/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { StatementLineItemDto } from "./statement-line-item-dto";

export class StatementDto {
  clinicId: string;
  startDate: Date;
  endDate: Date;
  openingBalance: number;
  closingBalance: number;
  lines: StatementLineItemDto[] = [];
}
