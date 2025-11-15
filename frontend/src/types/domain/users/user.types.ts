import type { UserResponseDto } from "@/types/backend";

export enum UserStatus {
  ACTIVE = 'active',
  INACTIVE = 'inactive',
  DELETED = 'deleted',
  PENDING = 'pending',
}

export enum UserRoleType {
  SYSTEM = 'system',
  CLINIC = 'clinic',
}

export interface UserWithMetadata extends Omit<UserResponseDto, 'id'> {
  id: string;
  status: UserStatus;
  roleType: UserRoleType;
  isLocked: boolean;
  canEdit: boolean;
  canDelete: boolean;
  canActivate: boolean;
}

export interface UserActionAvailability {
  edit: boolean;
  delete: boolean;
  activate: boolean;
  deactivate: boolean;
  resetPassword: boolean;
  restore: boolean;
}

export interface UserDisplayConfig {
  initials: string;
  statusBadge: {
    value: string;
    severity: 'success' | 'danger' | 'warning';
  };
  cardClasses: string;
  roleTagSeverity: 'success' | 'danger' | 'info';
}