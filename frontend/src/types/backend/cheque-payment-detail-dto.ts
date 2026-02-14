/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { Currency } from "./currency";
import { ChequeStatus } from "./cheque-status";

export class ChequePaymentDetailDto {
  chequeNumber: string = "";
  bankName: string = "";
  branch: string = "";
  issueDate: Date;
  dueDate: Date;
  amount: number;
  currency: Currency;
  status: ChequeStatus;
  clearedDate: Date;
  imageUrl: string;
  note: string;
}
