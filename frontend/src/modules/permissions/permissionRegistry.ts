// src/modules/permissions/permissionRegistry.ts
import { PermissionRegistry } from '@/config/permissionsRegistry';
import type { PermissionResponseDto } from '@/types/backend';

// =============== Types ===============

export type PermissionCategoryKey = keyof typeof PermissionRegistry;

export interface PermissionDefinition {
  /** e.g. "Users.View" */
  name: string;
  /** e.g. "view", "create", "edit" (اسم الـ static field في الكلاس) */
  key: string;
  /** e.g. "Users" / "Patients" */
  category: PermissionCategoryKey | string;
  /** Backend description أو TypeGen description */
  description?: string;
  /** GUID من الـ backend */
  id?: string;
}

export interface PermissionCategoryDefinition {
  key: PermissionCategoryKey | string;
  /** Label ودّي لعرضه في الـ UI (ممكن نطوره لاحقاً) */
  label: string;
  /** Permissions تحت هذا الكاتيجوري */
  items: PermissionDefinition[];
  /** ترتيب العرض */
  order: number;
}

export interface PermissionRegistryState {
  all: PermissionDefinition[];
  byName: Record<string, PermissionDefinition>;
  byId: Record<string, PermissionDefinition>;
  categories: PermissionCategoryDefinition[];
}

// =============== Helpers ===============

// ترتيب الكاتيجوريز في الـ UI (نقدر نعدّله لاحقاً)
const CATEGORY_ORDER: PermissionCategoryKey[] = [
  'System',
  'Clinics',
  'Clinic',
  'Subscriptions',
  'Users',
  'Roles',
  'AppPermissions',
  'Patients',
  'Appointments',
  'MedicalRecords',
  'Payments',
  'Invoices',
  'Reports',
  'Settings',
  'AuditLogs',
];

function getCategoryOrder(key: PermissionCategoryKey | string): number {
  const idx = CATEGORY_ORDER.indexOf(key as PermissionCategoryKey);
  return idx === -1 ? CATEGORY_ORDER.length + 1 : idx;
}

/**
 * Extract static permissions (names + descriptions) من TypeGen class واحد
 * Example: Users => { view: "Users.View", create: "Users.Create", ... }
 */
function extractStaticPermissionsFromClass(
  categoryKey: PermissionCategoryKey,
): PermissionDefinition[] {
  const clazz: any = PermissionRegistry[categoryKey];

  if (!clazz) return [];

  const descriptions: Record<string, string> =
    typeof clazz.descriptions === 'object' && clazz.descriptions ? clazz.descriptions : {};

  const result: PermissionDefinition[] = [];

  for (const [prop, value] of Object.entries(clazz)) {
    if (prop === 'descriptions') continue;
    if (typeof value !== 'string') continue;

    const name = value; // e.g. "Users.View"
    const key = prop; // e.g. "view"

    result.push({
      name,
      key,
      category: categoryKey,
      description: descriptions[name],
    });
  }

  return result;
}

/**
 * Create a user-friendly label من اسم الكاتيجوري
 * e.g. "AppPermissions" => "App Permissions"
 */
function formatCategoryLabel(key: string): string {
  // بسيط حالياً، نقدر نطوره لاحقاً
  return key.replace(/([a-z])([A-Z])/g, '$1 $2');
}

/**
 * Build runtime registry state من:
 * - static TypeGen classes
 * - dynamic backend list (PermissionResponseDto[])
 */
export function buildPermissionRegistry(
  backendPermissions: PermissionResponseDto[],
): PermissionRegistryState {
  const backendByName = new Map<string, PermissionResponseDto>();
  const backendById = new Map<string, PermissionResponseDto>();

  for (const p of backendPermissions) {
    backendByName.set(p.name, p);
    backendById.set(p.id, p);
  }

  const categories: PermissionCategoryDefinition[] = [];
  const all: PermissionDefinition[] = [];
  const byName: Record<string, PermissionDefinition> = {};
  const byId: Record<string, PermissionDefinition> = {};

  // 1) أولاً: نمشي على كل الكاتيجوريز اللي جايين من TypeGen
  const categoryKeys = Object.keys(PermissionRegistry) as PermissionCategoryKey[];

  for (const categoryKey of categoryKeys) {
    const staticDefs = extractStaticPermissionsFromClass(categoryKey);

    const resolvedItems = staticDefs.map<PermissionDefinition>((def) => {
      const backend = backendByName.get(def.name);

      const merged: PermissionDefinition = {
        ...def,
        id: backend?.id,
        description: backend?.description ?? def.description,
      };

      if (merged.name) {
        byName[merged.name] = merged;
      }
      if (merged.id) {
        byId[merged.id] = merged;
      }

      all.push(merged);
      return merged;
    });

    // لو هذا الكاتيجوري ما فيه ولا permission، نتجاهله
    if (!resolvedItems.length) continue;

    categories.push({
      key: categoryKey,
      label: formatCategoryLabel(categoryKey),
      items: resolvedItems,
      order: getCategoryOrder(categoryKey),
    });
  }

  // 2) ثانياً: أي permissions جاية من backend بس مش موجودة في TypeGen (حالات mismatch)
  for (const backend of backendPermissions) {
    if (byName[backend.name]) continue; // already handled

    const [categoryName] = backend.name.split('.');
    const categoryKey = categoryName as PermissionCategoryKey | string;

    const def: PermissionDefinition = {
      name: backend.name,
      key: backend.name.split('.').slice(1).join('.') || backend.name,
      category: categoryKey,
      id: backend.id,
      description: backend.description,
    };

    all.push(def);
    byName[def.name] = def;
    if (def.id) byId[def.id] = def;

    // أضفها للكاتيجوري المناسب (أو أنشئ واحد جديد)
    let cat = categories.find((c) => c.key === categoryKey);

    if (!cat) {
      cat = {
        key: categoryKey,
        label: formatCategoryLabel(String(categoryKey)),
        items: [],
        order: getCategoryOrder(categoryKey),
      };
      categories.push(cat);
    }

    cat.items.push(def);
  }

  // 3) رتّب الكاتيجوريز والـ items داخلياً
  categories.sort((a, b) => a.order - b.order || a.label.localeCompare(b.label));

  for (const cat of categories) {
    cat.items.sort((a, b) => a.name.localeCompare(b.name));
  }

  return {
    all,
    byName,
    byId,
    categories,
  };
}

// Registry فاضي تستخدمه قبل ما تحمل البيانات من backend
export const emptyPermissionRegistry: PermissionRegistryState = {
  all: [],
  byName: {},
  byId: {},
  categories: [],
};
