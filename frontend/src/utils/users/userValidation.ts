// src/utils/users/userValidation.ts
import { PASSWORD_REQUIREMENTS, USER_VALIDATION } from '@/types/domain/users';

export interface PasswordStrength {
  minLength: boolean;
  uppercase: boolean;
  lowercase: boolean;
  digit: boolean;
  specialChar: boolean;
}

export function validatePassword(password: string): PasswordStrength {
  return {
    minLength: password.length >= USER_VALIDATION.PASSWORD.MIN_LENGTH,
    uppercase: /[A-Z]/.test(password),
    lowercase: /[a-z]/.test(password),
    digit: /\d/.test(password),
    specialChar: /[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/.test(password),
  };
}

export function isPasswordSecure(password: string): boolean {
  const strength = validatePassword(password);
  return Object.values(strength).every((val) => val === true);
}

export function getPasswordStrengthPercentage(strength: PasswordStrength): number {
  const met = Object.values(strength).filter((val) => val === true).length;
  return (met / Object.keys(strength).length) * 100;
}

export function validateEmailFormat(email: string): boolean {
  return USER_VALIDATION.EMAIL_REGEX.test(email);
}

export function validateUserFullName(name: string): boolean {
  if (!name) return false;
  const trimmed = name.trim();
  return (
    trimmed.length >= USER_VALIDATION.FULL_NAME.MIN_LENGTH &&
    trimmed.length <= USER_VALIDATION.FULL_NAME.MAX_LENGTH
  );
}

export function getPasswordRequirementStatus(strength: PasswordStrength) {
  return PASSWORD_REQUIREMENTS.map((req) => ({
    ...req,
    met: strength[req.key as keyof PasswordStrength],
  }));
}
