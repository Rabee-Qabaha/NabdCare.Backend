/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { PaginationRequestDto } from "./pagination-request-dto";

export class RoleFilterRequestDto extends PaginationRequestDto {
  search?: string;
  includeDeleted: boolean;
  clinicId?: string;
  isSystemRole?: boolean;
  isTemplate?: boolean;
  fromDate?: Date;
  toDate?: Date;
  roleOrigin?: string;
}
