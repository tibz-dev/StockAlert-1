'use client';

import { useEffect, useState } from 'react';
import { ArrowLeft, Loader2, Printer } from 'lucide-react';
import { useParams, useRouter } from 'next/navigation';
import api from '@/lib/api';
import type { BusinessProfile } from '@/types/business';
import type { Quote } from '@/types/quote';

export default function QuotePrintPage() {
  const params = useParams<{ id: string }>();
  const router = useRouter();
  const [quote, setQuote] = useState<Quote | null>(null);
  const [business, setBusiness] = useState<BusinessProfile | null>(null);
  const [error, setError] = useState('');

  useEffect(() => {
    const load = async () => {
      try {
        const [quoteResponse, businessResponse] = await Promise.all([
          api.get<Quote>(`/quotes/${params.id}`),
          api.get<BusinessProfile>('/business-profile'),
        ]);

        setQuote(quoteResponse.data);
        setBusiness(businessResponse.data);
      } catch {
        setError('Unable to load quote document.');
      }
    };

    void load();
  }, [params.id]);

  if (error) {
    return <div className="p-8 text-sm text-red-600">{error}</div>;
  }

  if (!quote || !business) {
    return (
      <div className="flex min-h-screen items-center justify-center text-gray-500">
        <Loader2 className="mr-2 animate-spin" size={20} />
        Loading quote...
      </div>
    );
  }

  const address = [
    business.addressLine1,
    business.addressLine2,
    business.city,
    business.province,
    business.postalCode,
    business.country,
  ]
    .filter(Boolean)
    .join(', ');

  return (
    <div className="min-h-screen bg-slate-100 px-4 py-8 print:bg-white print:p-0">
      <div className="mx-auto mb-4 flex max-w-4xl items-center justify-between print:hidden">
        <button
          onClick={() => router.back()}
          className="inline-flex items-center gap-2 rounded-lg border border-gray-300 bg-white px-4 py-2 text-sm font-semibold text-gray-700"
        >
          <ArrowLeft size={16} />
          Back
        </button>
        <button
          onClick={() => window.print()}
          className="inline-flex items-center gap-2 rounded-lg bg-blue-600 px-4 py-2 text-sm font-semibold text-white"
        >
          <Printer size={16} />
          Print / Save PDF
        </button>
      </div>

      <main className="mx-auto max-w-4xl bg-white p-8 shadow-sm print:max-w-none print:shadow-none">
        <header className="flex items-start justify-between gap-8 border-b border-gray-200 pb-6">
          <div>
            {business.logoUrl && (
              <img
                src={business.logoUrl}
                alt={business.businessName}
                className="mb-4 h-14 max-w-48 object-contain object-left"
              />
            )}
            <h1 className="text-2xl font-bold text-gray-900">
              {business.businessName}
            </h1>
            {business.tradingName && (
              <p className="mt-1 text-sm text-gray-500">{business.tradingName}</p>
            )}
            {address && (
              <p className="mt-2 max-w-md text-sm leading-6 text-gray-500">
                {address}
              </p>
            )}
            <div className="mt-2 space-y-1 text-sm text-gray-500">
              {business.email && <p>{business.email}</p>}
              {business.phoneNumber && <p>{business.phoneNumber}</p>}
              {business.registrationNumber && (
                <p>Reg: {business.registrationNumber}</p>
              )}
              {business.vatNumber && <p>VAT: {business.vatNumber}</p>}
            </div>
          </div>

          <div className="text-right">
            <p className="text-xs font-semibold uppercase tracking-[0.2em] text-blue-600">
              Quote
            </p>
            <h2 className="mt-2 text-2xl font-bold text-gray-900">
              {quote.quoteNumber}
            </h2>
            <p className="mt-3 text-sm text-gray-500">
              Created {new Date(quote.createdAt).toLocaleDateString()}
            </p>
            <p className="text-sm text-gray-500">
              Valid until {new Date(quote.validUntil).toLocaleDateString()}
            </p>
            {(business.branchName || business.branchNumber) && (
              <p className="mt-2 text-sm font-medium text-gray-700">
                Branch: {[business.branchName, business.branchNumber]
                  .filter(Boolean)
                  .join(' · ')}
              </p>
            )}
          </div>
        </header>

        <section className="grid gap-8 py-6 md:grid-cols-2">
          <div>
            <p className="text-xs font-semibold uppercase tracking-wide text-gray-400">
              Quoted to
            </p>
            <p className="mt-2 font-semibold text-gray-900">
              {quote.customer.fullName}
            </p>
            {quote.customer.companyName && (
              <p className="text-sm text-gray-600">
                {quote.customer.companyName}
              </p>
            )}
            {quote.customer.email && (
              <p className="mt-1 text-sm text-gray-500">{quote.customer.email}</p>
            )}
            {quote.customer.phoneNumber && (
              <p className="text-sm text-gray-500">
                {quote.customer.phoneNumber}
              </p>
            )}
            {quote.customer.address && (
              <p className="mt-2 text-sm text-gray-500">
                {quote.customer.address}
              </p>
            )}
          </div>

          <div className="md:text-right">
            <p className="text-xs font-semibold uppercase tracking-wide text-gray-400">
              Payment
            </p>
            <p className="mt-2 text-sm text-gray-600">
              Status: <strong>{quote.paymentStatus}</strong>
            </p>
            {quote.salespersonName && (
              <p className="text-sm text-gray-600">
                Salesperson: <strong>{quote.salespersonName}</strong>
              </p>
            )}
            {quote.depositRequired > 0 && (
              <p className="text-sm text-gray-600">
                Deposit required ({quote.depositPercentage}%):{' '}
                {money(quote.depositRequired, business.currencyCode)}
              </p>
            )}
            <p className="text-sm text-gray-600">
              Paid: {money(quote.amountPaid, business.currencyCode)}
            </p>
            <p className="text-sm text-gray-600">
              Balance: {money(quote.balanceDue, business.currencyCode)}
            </p>
          </div>
        </section>

        <section className="overflow-hidden rounded-lg border border-gray-200">
          <table className="w-full text-left">
            <thead className="bg-gray-50 text-xs uppercase tracking-wide text-gray-500">
              <tr>
                <th className="p-3">Product</th>
                <th className="p-3 text-right">Qty</th>
                <th className="p-3 text-right">Unit price</th>
                <th className="p-3 text-right">Total</th>
              </tr>
            </thead>
            <tbody>
              {quote.items.map((item) => (
                <tr key={item.id} className="border-t border-gray-100">
                  <td className="p-3 font-medium text-gray-900">
                    {item.productName}
                  </td>
                  <td className="p-3 text-right text-gray-600">
                    {item.quantity}
                  </td>
                  <td className="p-3 text-right text-gray-600">
                    {money(item.unitPrice, business.currencyCode)}
                  </td>
                  <td className="p-3 text-right font-semibold text-gray-900">
                    {money(item.lineTotal, business.currencyCode)}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>

        <section className="mt-6 ml-auto max-w-sm space-y-2 text-sm">
          <MoneyRow label="Subtotal" value={quote.subtotal} currency={business.currencyCode} />
          {quote.vatAmount > 0 && (
            <MoneyRow
              label={`VAT (${quote.vatRate}%)`}
              value={quote.vatAmount}
              currency={business.currencyCode}
            />
          )}
          <div className="flex justify-between border-t border-gray-300 pt-3 text-lg font-bold text-gray-900">
            <span>Total</span>
            <span>{money(quote.total, business.currencyCode)}</span>
          </div>
        </section>

        {(business.bankName ||
          business.bankAccountNumber ||
          business.bankBranchCode) && (
          <section className="mt-8 rounded-lg bg-gray-50 p-5">
            <h3 className="font-semibold text-gray-900">Banking details</h3>
            <div className="mt-3 grid gap-x-8 gap-y-2 text-sm text-gray-600 sm:grid-cols-2">
              {business.bankName && <p>Bank: {business.bankName}</p>}
              {business.bankAccountName && (
                <p>Account name: {business.bankAccountName}</p>
              )}
              {business.bankAccountNumber && (
                <p>Account number: {business.bankAccountNumber}</p>
              )}
              {business.bankBranchCode && (
                <p>Branch code: {business.bankBranchCode}</p>
              )}
              {business.bankAccountType && (
                <p>Account type: {business.bankAccountType}</p>
              )}
            </div>
          </section>
        )}

        {quote.notes && (
          <section className="mt-6">
            <h3 className="font-semibold text-gray-900">Notes</h3>
            <p className="mt-2 whitespace-pre-wrap text-sm leading-6 text-gray-600">
              {quote.notes}
            </p>
          </section>
        )}

        <footer className="mt-10 border-t border-gray-200 pt-5 text-xs text-gray-400">
          <p>
            Quote generated by StockAlert for {business.businessName}.
          </p>
        </footer>
      </main>
    </div>
  );
}

function MoneyRow({
  label,
  value,
  currency,
}: {
  label: string;
  value: number;
  currency: string;
}) {
  return (
    <div className="flex justify-between text-gray-600">
      <span>{label}</span>
      <span>{money(value, currency)}</span>
    </div>
  );
}

function money(value: number, currency: string) {
  return `${currency} ${value.toLocaleString(undefined, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })}`;
}
