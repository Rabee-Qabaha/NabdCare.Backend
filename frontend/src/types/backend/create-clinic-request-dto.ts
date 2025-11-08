/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { SubscriptionStatus } from "./subscription-status";
import { SubscriptionType } from "./subscription-type";

export class CreateClinicRequestDto {
  name: string = "";
  email: string;
  phone: string;
  address: string;
  status: SubscriptionStatus;
  subscriptionStartDate: Date;
  subscriptionEndDate: Date;
  subscriptionType: SubscriptionType;
  subscriptionFee: number;
  branchCount: number = 1;
}
