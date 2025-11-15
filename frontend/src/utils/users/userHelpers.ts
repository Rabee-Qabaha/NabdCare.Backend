import type { UserResponseDto } from '@/types/backend';
import { type UserFilterOptions, type UserWithMetadata, type UserActionAvailability, UserRoleType, UserStatus } from '@/types/domain/users';
import { USER_VALIDATION } from '@/types/domain/users';

export function detectUserStatus(user: UserResponseDto): UserStatus {
  if (user.isDeleted) return UserStatus.DELETED;
  if (!user.isActive) return UserStatus.INACTIVE;
  return UserStatus.ACTIVE;
}

export function detectUserRoleType(user: UserResponseDto): UserRoleType {
  return user.isSystemRole ? UserRoleType.SYSTEM : UserRoleType.CLINIC;
}

export function isUserLocked(user: UserResponseDto): boolean {
  return user.isSystemRole || user.isDeleted;
}

export function canEditUser(user: UserResponseDto): boolean {
  return !user.isDeleted && !user.isSystemRole;
}

export function canDeleteUser(user: UserResponseDto): boolean {
  return !user.isDeleted;
}

export function canActivateUser(user: UserResponseDto): boolean {
  return !user.isDeleted;
}

export function canRestoreUser(user: UserResponseDto): boolean {
  return user.isDeleted;
}

export function getAvailableUserActions(user: UserResponseDto): UserActionAvailability {
  return {
    edit: canEditUser(user),
    delete: canDeleteUser(user),
    activate: canActivateUser(user),
    deactivate: !user.isDeleted,
    resetPassword: !user.isDeleted,
    restore: canRestoreUser(user),
  };
}

export function enrichUser(user: UserResponseDto): UserWithMetadata {
  const status = detectUserStatus(user);
  const roleType = detectUserRoleType(user);
  const isLocked = isUserLocked(user);

  return {
    ...user,
    id: user.id,
    status,
    roleType,
    isLocked,
    canEdit: canEditUser(user),
    canDelete: canDeleteUser(user),
    canActivate: canActivateUser(user),
  };
}

export function filterUsersByOptions(
  users: UserResponseDto[],
  options: UserFilterOptions,
): UserResponseDto[] {
  return users.filter((user) => {
    if (options.searchTerm) {
      const term = options.searchTerm.toLowerCase();
      const matchesSearch = [user.fullName, user.email, user.roleName, user.clinicName].some(
        (val) => val?.toLowerCase().includes(term),
      );
      if (!matchesSearch) return false;
    }

    if (options.status !== undefined && options.status !== null) {
      if (user.isActive !== options.status) return false;
    }

    if (options.roleId && user.roleId !== options.roleId) return false;

    if (options.clinicId && user.clinicId !== options.clinicId) return false;

    if (options.isDeleted !== undefined) {
      if (user.isDeleted !== options.isDeleted) return false;
    }

    if (options.createdDate) {
      const userDate = new Date(user.createdAt).toDateString();
      const filterDate = new Date(options.createdDate).toDateString();
      if (userDate !== filterDate) return false;
    }

    return true;
  });
}

export function isValidEmail(email: string): boolean {
  return USER_VALIDATION.EMAIL_REGEX.test(email);
}

export function getActionDisabledReason(user: UserResponseDto, action: string): string | null {
  switch (action) {
    case 'edit':
      if (user.isSystemRole) return 'System users cannot be edited';
      if (user.isDeleted) return 'Deleted users cannot be edited';
      break;

    case 'delete':
      if (user.isDeleted) return 'User already deleted';
      break;

    case 'activate':
    case 'deactivate':
      if (user.isDeleted) return 'Deleted users cannot be modified';
      break;

    case 'restore':
      if (!user.isDeleted) return 'User is not deleted';
      break;
  }

  return null;
}

export function sortUsersByField(
  users: UserResponseDto[],
  field: keyof UserResponseDto,
  descending = false,
): UserResponseDto[] {
  return [...users].sort((a, b) => {
    const aVal = a[field];
    const bVal = b[field];

    if (aVal === bVal) return 0;
    if (aVal === null || aVal === undefined) return 1;
    if (bVal === null || bVal === undefined) return -1;

    const comparison = aVal < bVal ? -1 : 1;
    return descending ? -comparison : comparison;
  });
}