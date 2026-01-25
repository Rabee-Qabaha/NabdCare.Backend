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
  settings: ClinicSettings = {"timeZone":"UTC","currency":"USD","dateFormat":"dd/MM/yyyy","locale":"en-US","enablePatientPortal":true};
  staffGrowthRate: number;
  patientGrowthRate: number;
}
