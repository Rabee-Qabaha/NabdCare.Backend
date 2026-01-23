/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { Clinic } from "./clinic";

export class Product {
  clinicId: string;
  clinic: Clinic;
  name: string = "";
  sku: string;
  isService: boolean;
  trackStock: boolean = true;
  costPrice: number;
  sellPrice: number;
  currentStock: number;
  lowStockThreshold: number = 5;
}
