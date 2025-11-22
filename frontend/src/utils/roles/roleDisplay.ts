// src/utils/roles/roleDisplay.ts
import type { RoleResponseDto } from '@/types/backend';
import type { RoleDisplayConfig } from '@/types/domain/roles';

export function getRoleInitials(name?: string): string {
  return name?.charAt(0).toUpperCase() || '?';
}

export function getRoleCategoryBadge(isSystemRole: boolean): { value: string; severity: string } {
  return {
    value: isSystemRole ? 'System' : 'Clinic',
    severity: isSystemRole ? 'danger' : 'info',
  };
}

export function getRoleCardClasses(role: RoleResponseDto): string {
  if (role.isDeleted) {
    return 'border-pink-300 bg-pink-50 dark:border-pink-700 dark:bg-pink-950';
  }
  if (role.isSystemRole) {
    return 'border-red-300 bg-red-50 dark:border-red-700 dark:bg-red-950';
  }
  return 'border-surface-200 bg-surface-0 dark:border-surface-700 dark:bg-surface-900';
}

export function getAvatarClass(role: RoleResponseDto): string {
  if (role.isDeleted) return 'bg-gray-500 opacity-60 grayscale';
  if (role.isSystemRole) return 'bg-red-500';
  if (role.isTemplate) return 'bg-purple-500';
  return 'bg-blue-500';
}

export function formatRoleDisplay(role: RoleResponseDto): RoleDisplayConfig {
  return {
    initials: getRoleInitials(role.name),
    categoryBadge: getRoleCategoryBadge(role.isSystemRole),
    cardClasses: getRoleCardClasses(role),
    deletedBadge: role.isDeleted ? { value: 'Deleted', severity: 'danger' } : undefined,
  };
}

export function getRoleCategoryLabel(isSystemRole: boolean): string {
  return isSystemRole ? 'System Role' : 'Clinic Role';
}

export function getPermissionCountText(count: number): string {
  return `${count} permission${count !== 1 ? 's' : ''}`;
}

export function getUserCountText(count: number): string {
  return `${count} user${count !== 1 ? 's' : ''}`;
}

export function getRoleTypeLabel(role: RoleResponseDto): string {
  if (role.isTemplate) return '📋 Template';
  if (role.isSystemRole) return '🔐 System';
  return '🏢 Clinic';
}
