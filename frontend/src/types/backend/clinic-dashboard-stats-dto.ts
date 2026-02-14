/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { SubscriptionStatus } from "./subscription-status";
import { ClinicSettings } from "./clinic-settings";

export class ClinicDashboardStatsDto {
  clinicId: string;
  name: string = "";
  identifier: string;
  logoUrl: string;
  isActive: boolean;
  activeUsersCount: number;
  totalBranches: number;
  activePatientsCount: number;
  subscriptionPlan: string = "None";
  subscriptionStatus: SubscriptionStatus;
  subscriptionExpiresAt: Date;
  hasOverdueInvoices: boolean;
  lastLoginAt: Date;
  primaryAdminName: string;
  createdAt: Date;
  taxNumber: string;
  registrationNumber: string;
  settings: ClinicSettings = {"timeZone":"UTC","currency":0,"dateFormat":"dd/MM/yyyy","locale":"en-US","exchangeRateMarkupType":0,"exchangeRateMarkupValue":0.0,"enablePatientPortal":true};
  staffGrowthRate: number;
  patientGrowthRate: number;
}
