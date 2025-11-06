/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { SubscriptionStatus } from './subscription-status';
import { SubscriptionType } from './subscription-type';

export class UpdateSubscriptionRequestDto {
  startDate: Date;
  endDate: Date;
  type: SubscriptionType;
  fee: number;
  status: SubscriptionStatus;
  autoRenew: boolean;
  gracePeriodDays: number;
}
