/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { SubscriptionType } from "./subscription-type";

export interface PlanDefinition {
  id: string;
  name: string;
  type: SubscriptionType;
  baseFee: number;
  branchPrice: number;
  includedBranches: number;
  userPrice: number;
  includedUsers: number;
  durationDays: number;
  allowAddons: boolean;
}
