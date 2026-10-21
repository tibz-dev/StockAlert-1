import type { BusinessProfile } from '@/types/business';
import type { Customer } from '@/types/quote';

export interface ReceiptLine {
  productName: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface ReceiptDocument {
  receiptNumber: string;
  saleDate: string;
  customer: Customer;
  business: BusinessProfile;
  salespersonId: string | null;
  salespersonName: string | null;
  total: number;
  lines: ReceiptLine[];
}
