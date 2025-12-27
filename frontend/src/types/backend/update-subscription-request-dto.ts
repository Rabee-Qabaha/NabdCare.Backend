/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { SubscriptionStatus } from "./subscription-status";

export class UpdateSubscriptionRequestDto {
  extraBranches: number;
  extraUsers: number;
  bonusBranches: number;
  bonusUsers: number;
  autoRenew: boolean;
  cancelAtPeriodEnd: boolean;
  gracePeriodDays: number;
  status: SubscriptionStatus;
  endDate: Date;
}
