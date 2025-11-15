export enum RoleCategory {
  SYSTEM = 'system',
  CLINIC = 'clinic',
}

export interface RoleWithMetadata {
  id: string;
  name: string;
  description?: string;
  isSystemRole: boolean;
  isTemplate?: boolean;
  permissionCount: number;
  userCount: number;
  isDeleted: boolean;
  createdAt: Date;
  updatedAt: Date;
  createdByUserName?: string;
}

export interface RoleActionAvailability {
  edit: boolean;
  delete: boolean;
  clone: boolean;
}

export interface RoleDisplayConfig {
  initials: string;
  categoryBadge: {
    value: string;
    severity: 'success' | 'danger' | 'warning';
  };
  cardClasses: string;
  deletedBadge?: {
    value: string;
    severity: 'danger';
  };
}