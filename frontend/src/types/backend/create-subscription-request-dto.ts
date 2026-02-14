/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

export class CreateSubscriptionRequestDto {
  clinicId: string;
  planId: string = "";
  extraBranches: number;
  extraUsers: number;
  bonusBranches: number;
  bonusUsers: number;
  autoRenew: boolean = true;
  customStartDate: Date;
}
