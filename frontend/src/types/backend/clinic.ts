/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { ClinicSettings } from "./clinic-settings";
import { SubscriptionStatus } from "./subscription-status";
import { Subscription } from "./subscription";

export class Clinic {
  name: string = "";
  slug: string = "";
  email: string;
  phone: string;
  address: string;
  logoUrl: string;
  website: string;
  taxNumber: string;
  registrationNumber: string;
  settings: ClinicSettings = {"timeZone":"UTC","currency":"USD","dateFormat":"dd/MM/yyyy","locale":"en-US","enablePatientPortal":true};
  status: SubscriptionStatus;
  subscriptions: Subscription[] = [];
  branchCount: number = 1;
}
