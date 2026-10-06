export interface Customer {
  id: string;
  fullName: string;
  companyName: string | null;
  email: string | null;
  phoneNumber: string | null;
  whatsAppNumber: string | null;
  hasWhatsApp: boolean;
  address: string | null;
}

export interface QuoteItem {
  id: string;
  productId: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
  currentStock: number;
  reservedQuantity: number;
  availableQuantity: number;
}

export interface Quote {
  id: string;
  quoteNumber: string;
  customer: Customer;
  status: string;
  createdAt: string;
  validUntil: string;
  sentAt: string | null;
  acceptedAt: string | null;
  convertedAt: string | null;
  notes: string | null;
  subtotal: number;
  vatRate: number;
  vatAmount: number;
  total: number;
  createdBy: string | null;
  items: QuoteItem[];
}

export interface PreparedDelivery {
  deliveryLogId: string;
  documentType: string;
  documentId: string;
  channel: string;
  destination: string;
  status: string;
  actionUrl: string;
}

export interface QuoteConversion {
  quoteId: string;
  quoteNumber: string;
  primarySaleId: string;
  receiptNumber: string;
  total: number;
  customer: Customer;
}
