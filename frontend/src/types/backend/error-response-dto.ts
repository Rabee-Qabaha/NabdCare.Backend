/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

export class ErrorResponseDto {
  message: string = "";
  code: string = "";
  type: string = "";
  statusCode: number;
  traceId: string = "";
  details: { [key: string]: string[]; };
  stackTrace: string;
}
