/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

export class AuditLogResponseDto {
  id: string;
  userId: string;
  userEmail: string = "";
  clinicId: string;
  entityType: string = "";
  entityId: string;
  action: string = "";
  changes: string;
  reason: string;
  timestamp: Date;
  ipAddress: string;
  userAgent: string;
}
