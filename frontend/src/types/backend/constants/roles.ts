/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

export class Roles {
  static readonly descriptions: { [key: string]: string; } = {"Roles.ViewAll":"View all roles (system + clinic + templates)","Roles.ViewSystem":"View system roles (SuperAdmin only)","Roles.ViewTemplates":"View template roles for cloning","Roles.ViewClinic":"View roles of a specific clinic","Roles.View":"View a specific role by ID","Roles.Create":"Create new roles (clinic-level)","Roles.Clone":"Clone template roles to clinic","Roles.Edit":"Edit existing roles","Roles.Delete":"Delete clinic roles (cannot delete system roles)"};
  static readonly viewAll: string = "Roles.ViewAll";
  static readonly viewSystem: string = "Roles.ViewSystem";
  static readonly viewTemplates: string = "Roles.ViewTemplates";
  static readonly viewClinic: string = "Roles.ViewClinic";
  static readonly view: string = "Roles.View";
  static readonly create: string = "Roles.Create";
  static readonly clone: string = "Roles.Clone";
  static readonly edit: string = "Roles.Edit";
  static readonly delete: string = "Roles.Delete";
}
