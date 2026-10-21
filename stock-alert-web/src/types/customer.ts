export interface CustomerSummary {
  id: string;
  fullName: string;
  companyName: string | null;
  email: string | null;
  phoneNumber: string | null;
  whatsAppNumber: string | null;
  hasWhatsApp: boolean;
  address: string | null;
  quoteCount: number;
  saleCount: number;
  lifetimeValue: number;
  lastActivityAt: string | null;
}
