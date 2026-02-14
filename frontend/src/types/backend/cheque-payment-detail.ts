/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { Payment } from "./payment";
import { Currency } from "./currency";
import { ChequeStatus } from "./cheque-status";

export class ChequePaymentDetail {
  paymentId: string;
  payment: Payment;
  chequeNumber: string = "";
  bankName: string = "";
  branch: string = "";
  issueDate: Date;
  dueDate: Date;
  amount: number;
  currency: Currency = 3;
  status: ChequeStatus;
  clearedDate: Date;
  imageUrl: string = "";
  note: string;
}
