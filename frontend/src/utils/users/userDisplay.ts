import type { UserResponseDto } from '@/types/backend';
import type { UserDisplayConfig } from '@/types/domain/users';
import { detectUserStatus, detectUserRoleType } from './userHelpers';
import { UserStatus, UserRoleType } from '@/types/domain/users';

export function getUserInitials(fullName?: string): string {
  return fullName?.charAt(0).toUpperCase() || '?';
}

export function getUserStatusBadge(isActive: boolean): { value: string; severity: 'success' | 'danger' | 'warning' } {
  return {
    value: isActive ? 'Active' : 'Inactive',
    severity: isActive ? 'success' : 'danger',
  };
}

export function getUserStatusColor(status: UserStatus): string {
  switch (status) {
    case UserStatus.ACTIVE:
      return '#22c55e';
    case UserStatus.INACTIVE:
      return '#ef4444';
    case UserStatus.DELETED:
      return '#ec4899';
    case UserStatus.PENDING:
      return '#f59e0b';
    default:
      return '#6b7280';
  }
}

export function getUserCardClasses(user: UserResponseDto): string {
  if (user.isDeleted) {
    return 'bg-pink-100/40 dark:bg-pink-900/10 ring-1 ring-pink-300/50 opacity-90';
  }
  return 'bg-surface-0 dark:bg-surface-900 border-surface-200 dark:border-surface-700 hover:shadow-primary/40';
}

export function getRoleTagSeverity(user: UserResponseDto): 'success' | 'danger' | 'info' {
  return user.isSystemRole ? 'danger' : 'info';
}

export function getAvatarClass(user: UserResponseDto): string {
  if (user.isDeleted) return 'bg-gray-500 opacity-60 grayscale';
  return user.isActive ? 'bg-green-500' : 'bg-gray-500';
}

export function formatUserDisplay(user: UserResponseDto): UserDisplayConfig {
  const status = detectUserStatus(user);

  return {
    initials: getUserInitials(user.fullName),
    statusBadge: getUserStatusBadge(user.isActive),
    cardClasses: getUserCardClasses(user),
    roleTagSeverity: getRoleTagSeverity(user),
  };
}

export function getUserRoleTypeLabel(roleType: UserRoleType): string {
  return roleType === UserRoleType.SYSTEM ? 'System Role' : 'Clinic Role';
}

export function getUserStatusLabel(status: UserStatus): string {
  const labels: Record<UserStatus, string> = {
    [UserStatus.ACTIVE]: '🟢 Active',
    [UserStatus.INACTIVE]: '🔴 Inactive',
    [UserStatus.DELETED]: '🗑️ Deleted',
    [UserStatus.PENDING]: '⏳ Pending',
  };
  return labels[status];
}