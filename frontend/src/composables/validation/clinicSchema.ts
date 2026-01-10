// src/composables/validation/clinicSchema.ts
import { z } from 'zod';

export const RESERVED_SLUGS = [
  'www',
  'api',
  'admin',
  'superadmin',
  'auth',
  'mail',
  'dashboard',
  'support',
  'billing',
  'status',
  'portal',
  'help',
  'clinic',
  'system',
  'root',
  'login',
  'register',
];

export const clinicSchema = z.object({
  name: z.string().trim().min(1, 'Clinic name is required').max(100, 'Name is too long'),

  slug: z
    .string()
    .trim()
    .min(3, 'Subdomain must be at least 3 characters')
    .max(60, 'Subdomain is too long')
    .regex(/^[a-z0-9-]+$/, 'Only lowercase letters, numbers, and hyphens allowed')
    .refine((val) => !RESERVED_SLUGS.includes(val.toLowerCase()), {
      message: 'This subdomain is reserved',
    }),

  email: z.string().trim().email('Invalid email format').min(1, 'Email is required'),
  phone: z.string().trim().min(1, 'Phone number is required').max(20, 'Phone is too long'),
  // address: z.string().trim().min(5, 'Full address is required').max(500, 'Address is too long'),
  website: z.string().trim().url('Invalid URL').optional().or(z.literal('')),
  logoUrl: z.string().trim().url('Invalid Logo URL').optional().or(z.literal('')),

  settings: z
    .object({
      timeZone: z.string().min(1, 'Timezone is required'),
      currency: z.string().length(3, 'Must be 3-letter ISO code'),
    })
    .optional(),
});
