'use client';

import { useEffect, useState } from 'react';
import { ArrowLeft, Loader2, Printer } from 'lucide-react';
import { useParams, useRouter } from 'next/navigation';
import api from '@/lib/api';
import type { ReceiptDocument } from '@/types/receipt';

export default function ReceiptPrintPage() {
  const params = useParams<{ saleId: string }>();
  const router = useRouter();
  const [document, setDocument] = useState<ReceiptDocument | null>(null);
  const [error, setError] = useState('');

  useEffect(() => {
    const load = async () => {
      try {
        const response = await api.get<ReceiptDocument>(
          `/sales/${params.saleId}/receipt`,
        );
        setDocument(response.data);
      } catch {
        setError('Unable to load receipt.');
      }
    };

    void load();
  }, [params.saleId]);

  if (error) {
    return <div className="p-8 text-sm text-red-600">{error}</div>;
  }

  if (!document) {
    return (
      <div className="flex min-h-screen items-center justify-center text-gray-500">
        <Loader2 className="mr-2 animate-spin" size={20} />
        Loading receipt...
      </div>
    );
  }

  const { business, customer } = document;
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
      <div className="mx-auto mb-4 flex max-w-3xl items-center justify-between print:hidden">
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

      <main className="mx-auto max-w-3xl bg-white p-8 shadow-sm print:max-w-none print:shadow-none">
        <header className="flex items-start justify-between gap-6 border-b border-gray-200 pb-6">
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
            {address && (
              <p className="mt-2 max-w-sm text-sm leading-6 text-gray-500">
                {address}
              </p>
            )}
            {business.phoneNumber && (
              <p className="mt-1 text-sm text-gray-500">{business.phoneNumber}</p>
            )}
            {business.email && (
              <p className="text-sm text-gray-500">{business.email}</p>
            )}
          </div>

          <div className="text-right">
            <p className="text-xs font-semibold uppercase tracking-[0.2em] text-green-600">
              Receipt
            </p>
            <h2 className="mt-2 text-xl font-bold text-gray-900">
              {document.receiptNumber}
            </h2>
            <p className="mt-2 text-sm text-gray-500">
              {new Date(document.saleDate).toLocaleString()}
            </p>
            {(business.branchName || business.branchNumber) && (
              <p className="mt-2 text-sm font-medium text-gray-700">
                {[business.branchName, business.branchNumber]
                  .filter(Boolean)
                  .join(' · ')}
              </p>
            )}
          </div>
        </header>

        <section className="py-6">
          <p className="text-xs font-semibold uppercase tracking-wide text-gray-400">
            Customer
          </p>
          <p className="mt-2 font-semibold text-gray-900">
            {customer.fullName}
          </p>
          {customer.companyName && (
            <p className="text-sm text-gray-600">{customer.companyName}</p>
          )}
          {customer.email && (
            <p className="mt-1 text-sm text-gray-500">{customer.email}</p>
          )}
          {customer.phoneNumber && (
            <p className="text-sm text-gray-500">{customer.phoneNumber}</p>
          )}
        </section>

        <section className="overflow-hidden rounded-lg border border-gray-200">
          <table className="w-full text-left">
            <thead className="bg-gray-50 text-xs uppercase tracking-wide text-gray-500">
              <tr>
                <th className="p-3">Item</th>
                <th className="p-3 text-right">Qty</th>
                <th className="p-3 text-right">Unit price</th>
                <th className="p-3 text-right">Total</th>
              </tr>
            </thead>
            <tbody>
              {document.lines.map((line, index) => (
                <tr
                  key={`${line.productName}-${index}`}
                  className="border-t border-gray-100"
                >
                  <td className="p-3 font-medium text-gray-900">
                    {line.productName}
                  </td>
                  <td className="p-3 text-right text-gray-600">
                    {line.quantity}
                  </td>
                  <td className="p-3 text-right text-gray-600">
                    {money(line.unitPrice, business.currencyCode)}
                  </td>
                  <td className="p-3 text-right font-semibold text-gray-900">
                    {money(line.lineTotal, business.currencyCode)}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>

        <div className="mt-6 ml-auto flex max-w-sm justify-between border-t border-gray-300 pt-4 text-xl font-bold text-gray-900">
          <span>Total</span>
          <span>{money(document.total, business.currencyCode)}</span>
        </div>

        {business.receiptFooter && (
          <p className="mt-10 text-center text-sm text-gray-500">
            {business.receiptFooter}
          </p>
        )}

        <footer className="mt-8 border-t border-gray-200 pt-4 text-center text-xs text-gray-400">
          Receipt generated by StockAlert.
        </footer>
      </main>
    </div>
  );
}

function money(value: number, currency: string) {
  return `${currency} ${value.toLocaleString(undefined, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })}`;
}
