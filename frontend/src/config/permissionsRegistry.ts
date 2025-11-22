/** AUTO-GENERATED FILE — DO NOT EDIT MANUALLY */
import { ErrorCodes } from '@/types/backend';
import { AppPermissions } from '@/types/backend/constants/app-permissions';
import { Appointments } from '@/types/backend/constants/appointments';
import { AuditLogs } from '@/types/backend/constants/audit-logs';
import { Clinic } from '@/types/backend/constants/clinic';
import { Clinics } from '@/types/backend/constants/clinics';
import { Invoices } from '@/types/backend/constants/invoices';
import { MedicalRecords } from '@/types/backend/constants/medical-records';
import { Patients } from '@/types/backend/constants/patients';
import { Payments } from '@/types/backend/constants/payments';
import { Reports } from '@/types/backend/constants/reports';
import { Roles } from '@/types/backend/constants/roles';
import { Settings } from '@/types/backend/constants/settings';
import { Subscriptions } from '@/types/backend/constants/subscriptions';
import { System } from '@/types/backend/constants/system';
import { Users } from '@/types/backend/constants/users';

export const PermissionRegistry = {
  AppPermissions,
  Clinics,
  Clinic,
  Subscriptions,
  Users,
  Roles,
  Patients,
  Appointments,
  MedicalRecords,
  Payments,
  Invoices,
  Reports,
  Settings,
  AuditLogs,
  System,
} as const;

export { ErrorCodes };
