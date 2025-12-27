/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { PaginationRequestDto } from "./pagination-request-dto";
import { SubscriptionStatus } from "./subscription-status";
import { SubscriptionType } from "./subscription-type";

export class ClinicFilterRequestDto extends PaginationRequestDto {
  search: string;
  name: string;
  email: string;
  phone: string;
  status: SubscriptionStatus;
  subscriptionType: SubscriptionType;
  subscriptionFee: number;
  createdAt: Date;
  includeDeleted: boolean;
}
