/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { Clinic } from "./clinic";
import { SubscriptionType } from "./subscription-type";
import { Currency } from "./currency";
import { SubscriptionStatus } from "./subscription-status";
import { Invoice } from "./invoice";
import { Payment } from "./payment";

export class Subscription {
  clinicId: string;
  clinic: Clinic;
  planId: string = "";
  externalSubscriptionId: string;
  startDate: Date;
  endDate: Date;
  billingCycleAnchor: Date;
  trialEndsAt: Date;
  cancelAtPeriodEnd: boolean;
  canceledAt: Date;
  cancellationReason: string;
  type: SubscriptionType;
  currency: Currency;
  fee: number;
  status: SubscriptionStatus;
  purchasedBranches: number;
  includedBranchesSnapshot: number = 1;
  bonusBranches: number;
  maxBranches: number = 1;
  purchasedUsers: number;
  includedUsersSnapshot: number = 1;
  bonusUsers: number;
  maxUsers: number = 1;
  autoRenew: boolean = true;
  gracePeriodDays: number = 7;
  notes: string;
  previousSubscriptionId: string;
  previousSubscription: Subscription;
  invoices: Invoice[] = [];
  payments: Payment[] = [];
}
