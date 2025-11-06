/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { Payment } from './payment';
import { ChequeStatus } from './cheque-status';

export class ChequePaymentDetail {
  paymentId: string;
  payment: Payment;
  chequeNumber: string = '';
  bankName: string = '';
  branch: string = '';
  issueDate: Date;
  dueDate: Date;
  amount: number;
  status: ChequeStatus;
  clearedDate: Date;
}
