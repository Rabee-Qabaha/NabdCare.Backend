/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { SubscriptionStatus } from "./subscription-status";
import { SubscriptionResponseDto } from "./subscription-response-dto";

export class ClinicStatisticsDto {
  clinicId: string;
  clinicName: string = "";
  status: SubscriptionStatus;
  branchCount: number;
  totalSubscriptions: number;
  currentSubscription: SubscriptionResponseDto;
  isSubscriptionActive: boolean;
  daysUntilExpiration: number;
  isExpiringSoon: boolean;
  createdAt: Date;
}
