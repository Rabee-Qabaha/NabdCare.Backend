export interface RoleFilterOptions {
  searchTerm?: string;
  category?: 'system' | 'clinic' | null;
  isDeleted?: boolean | null;
  createdDate?: Date;
}

export interface RoleFilterState {
  global: string | null;
  name: string | null;
  description: string | null;
  category: string | null;
  isDeleted: boolean | null;
  createdAt: Date | null;
}