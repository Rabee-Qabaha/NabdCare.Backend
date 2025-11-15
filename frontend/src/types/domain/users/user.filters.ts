export interface UserFilterOptions {
  searchTerm?: string;
  status?: boolean | null;
  roleId?: string;
  clinicId?: string;
  isDeleted?: boolean;
  createdDate?: Date;
}

export interface UserFilterState {
  global: string | null;
  fullName: string | null;
  email: string | null;
  roleName: string | null;
  clinicName: string | null;
  isActive: boolean | null;
  createdAt: Date | null;
  showDeleted: boolean;
}