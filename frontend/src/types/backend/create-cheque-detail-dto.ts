/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { Currency } from "./currency";

export class CreateChequeDetailDto {
  chequeNumber: string = "";
  bankName: string = "";
  branch: string = "";
  issueDate: Date;
  dueDate: Date;
  amount: number;
  currency: Currency;
  imageUrl: string;
  note: string;
}
