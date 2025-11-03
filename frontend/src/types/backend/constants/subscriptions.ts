/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

export class Subscriptions {
  static readonly descriptions: { [key: string]: string; } = {"Subscriptions.View":"View subscriptions belonging to own clinic or others (if SuperAdmin)","Subscriptions.ViewActive":"View only the active subscription for a clinic","Subscriptions.ViewAll":"View all subscriptions across all clinics (SuperAdmin only)","Subscriptions.Create":"Create new subscriptions (SuperAdmin only)","Subscriptions.Edit":"Edit existing subscriptions","Subscriptions.Delete":"Soft delete (cancel) a subscription","Subscriptions.HardDelete":"Permanently delete a subscription (SuperAdmin only)","Subscriptions.ChangeStatus":"Change subscription status (SuperAdmin only)","Subscriptions.Renew":"Renew a subscription for a clinic (SuperAdmin only)","Subscriptions.ToggleAutoRenew":"Enable or disable auto-renew for a subscription"};
  static readonly viewAll: string = "Subscriptions.ViewAll";
  static readonly view: string = "Subscriptions.View";
  static readonly create: string = "Subscriptions.Create";
  static readonly edit: string = "Subscriptions.Edit";
  static readonly delete: string = "Subscriptions.Delete";
  static readonly hardDelete: string = "Subscriptions.HardDelete";
  static readonly changeStatus: string = "Subscriptions.ChangeStatus";
  static readonly renew: string = "Subscriptions.Renew";
  static readonly viewActive: string = "Subscriptions.ViewActive";
  static readonly toggleAutoRenew: string = "Subscriptions.ToggleAutoRenew";
}
