/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { Subscription } from './subscription';
import { SubscriptionStatus } from './subscription-status';

export class Clinic {
  name: string = '';
  email: string;
  phone: string;
  address: string;
  status: SubscriptionStatus;
  subscriptions: Subscription[] = [];
  branchCount: number = 1;
}
