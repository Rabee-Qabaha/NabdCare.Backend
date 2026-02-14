/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { Currency } from "./currency";
import { MarkupType } from "./markup-type";

export class ClinicSettings {
  timeZone: string = "UTC";
  currency: Currency;
  dateFormat: string = "dd/MM/yyyy";
  locale: string = "en-US";
  exchangeRateMarkupType: MarkupType;
  exchangeRateMarkupValue: number;
  enablePatientPortal: boolean = true;
}
