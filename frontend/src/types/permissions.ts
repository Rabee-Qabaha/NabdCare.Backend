// src/types/permissions.ts
import { PermissionRegistry } from "@/config/permissionsRegistry";

/**
 * Extracts only the static string fields from a given permission class.
 * Example: from `Users` it extracts values like "Users.View", "Users.Edit", etc.
 */
type ExtractPermissionValues<T> = {
  [K in keyof T]: T[K] extends string ? T[K] : never;
}[keyof T];

/**
 * The shape of the entire permission registry (e.g. { Users, Clinics, Reports, ... })
 */
type Registry = typeof PermissionRegistry;

/**
 * Collects all string values from every class inside the PermissionRegistry.
 * Example result: "Users.View" | "Users.Edit" | "Clinics.Create" | ...
 */
export type PermissionValue = {
  [K in keyof Registry]: ExtractPermissionValues<Registry[K]>;
}[keyof Registry];

/**
 * (Optional) If you ever need key-style strings like "Users.view" or "Patients.create"
 * instead of the actual values, you can use this type.
 */
export type PermissionKey = {
  [K in keyof Registry]: `${Extract<K, string>}.${Extract<
    keyof Registry[K],
    string
  >}`;
}[keyof Registry];

/**
 * Handy global alias to access permissions easily (optional).
 * Example usage:
 *   import { $perm } from "@/types/permissions";
 *   permission: $perm.Users.view
 */
export const $perm = PermissionRegistry;
