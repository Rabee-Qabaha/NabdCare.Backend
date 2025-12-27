/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { SubscriptionType } from "./subscription-type";
import { SubscriptionStatus } from "./subscription-status";
import { InvoiceStatus } from "./invoice-status";

export class SubscriptionResponseDto {
  id: string;
  clinicId: string;
  planId: string = "";
  externalSubscriptionId: string;
  startDate: Date;
  endDate: Date;
  trialEndsAt: Date;
  billingCycleAnchor: Date;
  type: SubscriptionType;
  currency: string = "USD";
  fee: number;
  status: SubscriptionStatus;
  autoRenew: boolean;
  cancelAtPeriodEnd: boolean;
  cancellationReason: string;
  gracePeriodDays: number;
  maxBranches: number;
  purchasedBranches: number;
  includedBranchesSnapshot: number;
  bonusBranches: number;
  maxUsers: number;
  purchasedUsers: number;
  includedUsersSnapshot: number;
  bonusUsers: number;
  latestInvoiceId: string;
  latestInvoiceNumber: string;
  latestInvoiceStatus: InvoiceStatus;
}
