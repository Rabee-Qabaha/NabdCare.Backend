import type { RoleResponseDto } from '@/types/backend';
import type { RoleFilterOptions, RoleWithMetadata } from '@/types/domain/roles';
import { RoleCategory } from '@/types/domain/roles';
import { ROLE_VALIDATION } from '@/types/domain/roles';

export function detectRoleCategory(role: RoleResponseDto): RoleCategory {
  return role.isSystemRole ? RoleCategory.SYSTEM : RoleCategory.CLINIC;
}

export function canEditRole(role: RoleResponseDto): boolean {
  return !role.isSystemRole && !role.isDeleted;
}

export function canDeleteRole(role: RoleResponseDto): boolean {
  return !role.isSystemRole && !role.isDeleted && (role.userCount || 0) === 0;
}

export function canCloneRole(role: RoleResponseDto): boolean {
  return role.isTemplate === true && !role.isDeleted;
}

export function enrichRole(role: RoleResponseDto): RoleWithMetadata {
  return {
    id: role.id,
    name: role.name,
    description: role.description,
    isSystemRole: role.isSystemRole,
    isTemplate: role.isTemplate,
    permissionCount: role.permissionCount || 0,
    userCount: role.userCount || 0,
    isDeleted: role.isDeleted || false,
    createdAt: role.createdAt,
    updatedAt: role.updatedAt,
    createdByUserName: role.createdByUserName,
  };
}

export function filterRolesByOptions(
  roles: RoleResponseDto[],
  options: RoleFilterOptions,
): RoleResponseDto[] {
  return roles.filter((role) => {
    if (options.searchTerm) {
      const term = options.searchTerm.toLowerCase();
      const matchesSearch = [role.name, role.description].some(
        (val) => val?.toLowerCase().includes(term),
      );
      if (!matchesSearch) return false;
    }

    if (options.category !== undefined && options.category !== null) {
      const isSystem = options.category === 'system';
      if (role.isSystemRole !== isSystem) return false;
    }

    if (options.isDeleted !== undefined && options.isDeleted !== null) {
      if ((role.isDeleted || false) !== options.isDeleted) return false;
    }

    if (options.createdDate) {
      const roleDate = new Date(role.createdAt).toDateString();
      const filterDate = new Date(options.createdDate).toDateString();
      if (roleDate !== filterDate) return false;
    }

    return true;
  });
}

export function isValidRoleName(name: string): boolean {
  if (!name) return false;
  const trimmed = name.trim();
  return (
    trimmed.length >= ROLE_VALIDATION.NAME.MIN_LENGTH &&
    trimmed.length <= ROLE_VALIDATION.NAME.MAX_LENGTH
  );
}

export function getActionDisabledReason(role: RoleResponseDto, action: string): string | null {
  switch (action) {
    case 'edit':
      if (role.isSystemRole) return 'System roles cannot be edited';
      if (role.isDeleted) return 'Deleted roles cannot be edited';
      break;

    case 'delete':
      if (role.isSystemRole) return 'System roles cannot be deleted';
      if (role.isDeleted) return 'Role already deleted';
      if ((role.userCount || 0) > 0) {
        return `${role.userCount} user${role.userCount !== 1 ? 's' : ''} still using this role`;
      }
      break;

    case 'clone':
      if (!role.isTemplate) return 'Only template roles can be cloned';
      if (role.isDeleted) return 'Deleted roles cannot be cloned';
      break;
  }

  return null;
}

export function sortRolesByField(
  roles: RoleResponseDto[],
  field: keyof RoleResponseDto,
  descending = false,
): RoleResponseDto[] {
  return [...roles].sort((a, b) => {
    const aVal = a[field];
    const bVal = b[field];

    if (aVal === bVal) return 0;
    if (aVal === null || aVal === undefined) return 1;
    if (bVal === null || bVal === undefined) return -1;

    const comparison = aVal < bVal ? -1 : 1;
    return descending ? -comparison : comparison;
  });
}