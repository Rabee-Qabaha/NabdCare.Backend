export const USER_VALIDATION = {
  EMAIL_REGEX: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
  
  PASSWORD: {
    MIN_LENGTH: 12,
    REQUIREMENTS: {
      minLength: 12,
      uppercase: true,
      lowercase: true,
      digit: true,
      specialChar: true,
    },
  },

  FULL_NAME: {
    MIN_LENGTH: 2,
    MAX_LENGTH: 100,
  },
} as const;

export const PASSWORD_REQUIREMENTS = [
  { key: 'minLength', label: 'Minimum 9 characters', icon: 'pi-check-circle' },
  { key: 'uppercase', label: 'At least one uppercase letter', icon: 'pi-check-circle' },
  { key: 'lowercase', label: 'At least one lowercase letter', icon: 'pi-check-circle' },
  { key: 'digit', label: 'At least one digit (0-9)', icon: 'pi-check-circle' },
  { key: 'specialChar', label: 'At least one special character', icon: 'pi-check-circle' },
] as const;