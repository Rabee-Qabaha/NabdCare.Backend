/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { PaginationRequestDto } from "./pagination-request-dto";

export class UserFilterRequestDto extends PaginationRequestDto {
  search: string;
  includeDeleted: boolean;
  clinicId: string;
  roleId: string;
  isActive: boolean;
  fromDate: Date;
  toDate: Date;
}
