/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { ClinicSettingsDto } from "./clinic-settings-dto";

export class CreateClinicRequestDto {
  name: string = "";
  slug: string = "";
  email: string = "";
  phone: string;
  address: string;
  logoUrl: string;
  website: string;
  taxNumber: string;
  registrationNumber: string;
  settings: ClinicSettingsDto;
}
