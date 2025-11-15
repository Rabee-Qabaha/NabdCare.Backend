import { ROLE_VALIDATION } from '@/types/domain/roles';

export function validateRoleName(name: string): boolean {
  if (!name) return false;
  const trimmed = name.trim();
  return (
    trimmed.length >= ROLE_VALIDATION.NAME.MIN_LENGTH &&
    trimmed.length <= ROLE_VALIDATION.NAME.MAX_LENGTH
  );
}

export function validateRoleDescription(description: string): boolean {
  if (!description) return true;
  return description.length <= ROLE_VALIDATION.DESCRIPTION.MAX_LENGTH;
}

export function getRoleNameError(name: string): string | null {
  if (!name) return 'Role name is required';
  if (name.length < ROLE_VALIDATION.NAME.MIN_LENGTH) {
    return `Minimum ${ROLE_VALIDATION.NAME.MIN_LENGTH} characters`;
  }
  if (name.length > ROLE_VALIDATION.NAME.MAX_LENGTH) {
    return `Maximum ${ROLE_VALIDATION.NAME.MAX_LENGTH} characters`;
  }
  return null;
}

export function getRoleDescriptionError(description: string): string | null {
  if (!description) return null;
  if (description.length > ROLE_VALIDATION.DESCRIPTION.MAX_LENGTH) {
    return `Maximum ${ROLE_VALIDATION.DESCRIPTION.MAX_LENGTH} characters`;
  }
  return null;
}