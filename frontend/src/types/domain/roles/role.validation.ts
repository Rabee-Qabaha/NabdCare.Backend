export const ROLE_VALIDATION = {
  NAME: {
    MIN_LENGTH: 2,
    MAX_LENGTH: 100,
  },
  DESCRIPTION: {
    MAX_LENGTH: 500,
  },
} as const;

export const ROLE_CATEGORIES = [
  { label: 'System Role', value: 'system' },
  { label: 'Clinic Role', value: 'clinic' },
] as const;