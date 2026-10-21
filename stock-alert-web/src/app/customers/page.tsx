'use client';

import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Building2,
  Loader2,
  Mail,
  MessageCircle,
  Phone,
  Search,
  UserRound,
  Users,
} from 'lucide-react';
import api from '@/lib/api';
import type { CustomerSummary } from '@/types/customer';

export default function CustomersPage() {
  const [customers, setCustomers] = useState<CustomerSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [error, setError] = useState('');

  const loadCustomers = useCallback(async () => {
    try {
      setError('');
      const response = await api.get<CustomerSummary[]>('/customers');
      setCustomers(response.data);
    } catch {
      setError('Unable to load customers.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadCustomers();
  }, [loadCustomers]);

  const filtered = useMemo(() => {
    const query = search.trim().toLowerCase();

    if (!query) return customers;

    return customers.filter((customer) =>
      [
        customer.fullName,
        customer.companyName,
        customer.email,
        customer.phoneNumber,
      ]
        .filter(Boolean)
        .some((value) => value!.toLowerCase().includes(query)),
    );
  }, [customers, search]);

  const totalLifetimeValue = useMemo(
    () => customers.reduce((sum, customer) => sum + customer.lifetimeValue, 0),
    [customers],
  );

  const customersWithSales = useMemo(
    () => customers.filter((customer) => customer.saleCount > 0).length,
    [customers],
  );

  if (loading) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center text-gray-500">
        <Loader2 className="mr-2 animate-spin" size={20} />
        Loading customers...
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 p-6 lg:p-8">
      <div className="mx-auto max-w-7xl">
        <div className="mb-8">
          <p className="text-sm font-medium text-blue-600">Customer relationships</p>
          <h1 className="flex items-center gap-2 text-2xl font-bold text-gray-900">
            <Users className="text-blue-600" />
            Customers
          </h1>
          <p className="mt-1 text-sm text-gray-500">
            Customers are created automatically from quotes and sales, then reused across the business.
          </p>
        </div>

        {error && (
          <div className="mb-5 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
            {error}
          </div>
        )}

        <div className="mb-6 grid gap-4 sm:grid-cols-3">
          <SummaryCard label="Customers" value={customers.length.toString()} />
          <SummaryCard
            label="Customers with sales"
            value={customersWithSales.toString()}
          />
          <SummaryCard
            label="Customer lifetime value"
            value={formatAmount(totalLifetimeValue)}
          />
        </div>

        <div className="mb-5 rounded-xl border border-gray-200 bg-white p-4 shadow-sm">
          <label className="relative block">
            <Search
              size={18}
              className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400"
            />
            <input
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Search name, company, email or phone"
              className="w-full rounded-lg border border-gray-300 py-2.5 pl-10 pr-3 text-sm text-gray-900 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
            />
          </label>
        </div>

        <div className="grid gap-4 lg:grid-cols-2">
          {filtered.map((customer) => (
            <article
              key={customer.id}
              className="rounded-xl border border-gray-200 bg-white p-5 shadow-sm"
            >
              <div className="flex items-start justify-between gap-4">
                <div className="flex min-w-0 items-start gap-3">
                  <div className="rounded-full bg-blue-50 p-3 text-blue-600">
                    <UserRound size={20} />
                  </div>
                  <div className="min-w-0">
                    <h2 className="truncate font-semibold text-gray-900">
                      {customer.fullName}
                    </h2>
                    {customer.companyName && (
                      <p className="mt-1 flex items-center gap-1 text-sm text-gray-500">
                        <Building2 size={13} />
                        {customer.companyName}
                      </p>
                    )}
                  </div>
                </div>

                <div className="text-right">
                  <p className="text-xs uppercase tracking-wide text-gray-400">
                    Lifetime value
                  </p>
                  <p className="mt-1 font-bold text-gray-900">
                    {formatAmount(customer.lifetimeValue)}
                  </p>
                </div>
              </div>

              <div className="mt-5 grid grid-cols-3 gap-3">
                <Metric label="Quotes" value={customer.quoteCount} />
                <Metric label="Sales" value={customer.saleCount} />
                <Metric
                  label="Last activity"
                  value={
                    customer.lastActivityAt
                      ? new Date(customer.lastActivityAt).toLocaleDateString()
                      : '—'
                  }
                />
              </div>

              <div className="mt-5 space-y-2 border-t border-gray-100 pt-4 text-sm">
                {customer.email && (
                  <a
                    href={`mailto:${customer.email}`}
                    className="flex items-center gap-2 text-gray-600 hover:text-blue-600"
                  >
                    <Mail size={15} />
                    {customer.email}
                  </a>
                )}
                {customer.phoneNumber && (
                  <a
                    href={`tel:${customer.phoneNumber}`}
                    className="flex items-center gap-2 text-gray-600 hover:text-blue-600"
                  >
                    <Phone size={15} />
                    {customer.phoneNumber}
                  </a>
                )}
                {(customer.whatsAppNumber ||
                  (customer.hasWhatsApp && customer.phoneNumber)) && (
                  <div className="flex items-center gap-2 text-gray-600">
                    <MessageCircle size={15} />
                    WhatsApp available
                  </div>
                )}
              </div>
            </article>
          ))}

          {filtered.length === 0 && (
            <div className="col-span-full rounded-xl border border-dashed border-gray-300 bg-white p-12 text-center text-sm text-gray-500">
              No customers match the current search.
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

function SummaryCard({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-xl border border-gray-200 bg-white p-5 shadow-sm">
      <p className="text-sm text-gray-500">{label}</p>
      <p className="mt-1 text-2xl font-bold text-gray-900">{value}</p>
    </div>
  );
}

function Metric({
  label,
  value,
}: {
  label: string;
  value: string | number;
}) {
  return (
    <div className="rounded-lg bg-gray-50 p-3">
      <p className="text-xs text-gray-500">{label}</p>
      <p className="mt-1 text-sm font-semibold text-gray-900">{value}</p>
    </div>
  );
}

function formatAmount(value: number) {
  return value.toLocaleString(undefined, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  });
}
