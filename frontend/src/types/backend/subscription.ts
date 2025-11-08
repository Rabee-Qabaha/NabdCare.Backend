/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { Clinic } from "./clinic";
import { SubscriptionType } from "./subscription-type";
import { SubscriptionStatus } from "./subscription-status";
import { Payment } from "./payment";

export class Subscription {
  clinicId: string;
  clinic: Clinic;
  startDate: Date;
  endDate: Date;
  type: SubscriptionType;
  fee: number;
  status: SubscriptionStatus;
  previousSubscriptionId: string;
  previousSubscription: Subscription;
  paymentId: string;
  invoiceNumber: string;
  autoRenew: boolean;
  gracePeriodDays: number = 7;
  notes: string;
  payments: Payment[] = [];
}
