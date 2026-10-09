'use client';

import { Fragment, useCallback, useEffect, useMemo, useState } from 'react';
import {
  CheckCircle2,
  ChevronDown,
  CreditCard,
  ChevronUp,
  FileText,
  Loader2,
  Mail,
  MessageCircle,
  Plus,
  Printer,
  ShoppingCart,
  Smartphone,
  X,
  XCircle,
} from 'lucide-react';
import api from '@/lib/api';
import { getSessionInfo } from '@/lib/auth';
import type { BusinessProfile } from '@/types/business';
import type { Product } from '@/types/inventory';
import type { PreparedDelivery, Quote, QuoteConversion } from '@/types/quote';
import type { StaffMember } from '@/types/staff';

interface QuoteFormItem {
  productId: string;
  quantity: string;
  unitPrice: string;
}

export default function QuotesPage() {
  const session = getSessionInfo();
  const canAssignSalesperson =
    session.roles.includes('Owner') ||
    session.roles.includes('Manager');

  const [quotes, setQuotes] = useState<Quote[]>([]);
  const [products, setProducts] = useState<Product[]>([]);
  const [staff, setStaff] = useState<StaffMember[]>([]);
  const [business, setBusiness] = useState<BusinessProfile | null>(null);
  const [loading, setLoading] = useState(true);
  const [creating, setCreating] = useState(false);
  const [expandedId, setExpandedId] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [convertedReceipt, setConvertedReceipt] =
    useState<QuoteConversion | null>(null);
  const [paymentQuote, setPaymentQuote] = useState<Quote | null>(null);

  const load = useCallback(async () => {
    try {
      setError('');
      const [
        quotesResponse,
        productsResponse,
        staffResponse,
        businessResponse,
      ] = await Promise.all([
        api.get<Quote[]>('/quotes'),
        api.get<Product[]>('/products'),
        api.get<StaffMember[]>('/staff', { params: { activeOnly: true } }),
        api.get<BusinessProfile>('/business-profile'),
      ]);

      setQuotes(quotesResponse.data);
      setProducts(productsResponse.data);
      setStaff(staffResponse.data);
      setBusiness(businessResponse.data);
    } catch {
      setError('Unable to load quotes.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  const totals = useMemo(() => {
    const activeStatuses = new Set(['Draft', 'Sent', 'Accepted']);

    return {
      active: quotes.filter((quote) => activeStatuses.has(quote.status)).length,
      pipelineValue: quotes
        .filter((quote) => activeStatuses.has(quote.status))
        .reduce((sum, quote) => sum + quote.total, 0),
    };
  }, [quotes]);

  const updateStatus = async (quote: Quote, status: string) => {
    try {
      setBusyId(quote.id);
      setError('');
      await api.patch(`/quotes/${quote.id}/status`, { status });
      await load();
      setSuccess(`Quote ${quote.quoteNumber} updated to ${status}.`);
      window.setTimeout(() => setSuccess(''), 3000);
    } catch {
      setError(
        status === 'Accepted'
          ? 'Unable to accept this quote. Check available stock and expiry.'
          : 'Unable to update quote status.',
      );
    } finally {
      setBusyId(null);
    }
  };

  const sendQuote = async (
    quote: Quote,
    channel: 'Email' | 'Sms' | 'WhatsApp',
  ) => {
    try {
      setBusyId(quote.id);
      setError('');

      const response = await api.post<PreparedDelivery>(
        `/quotes/${quote.id}/delivery`,
        { channel },
      );

      await load();

      if (response.data.status === 'Sent') {
        setSuccess(
          `${channel} sent automatically to ${quote.customer.fullName}.`,
        );
      } else if (response.data.actionUrl) {
        window.location.href = response.data.actionUrl;
        setSuccess(
          response.data.status === 'PendingProviderConfiguration'
            ? `${channel} provider is not configured, so the manual fallback was opened.`
            : `${channel} automatic delivery failed; the manual fallback was opened.`,
        );
      } else {
        throw new Error(
          response.data.errorMessage ?? `${channel} delivery failed.`,
        );
      }
      window.setTimeout(() => setSuccess(''), 3000);
    } catch {
      setError(
        `Unable to prepare ${channel} delivery. Check the customer's contact details.`,
      );
    } finally {
      setBusyId(null);
    }
  };

  const convertToSale = async (quote: Quote) => {
    try {
      setBusyId(quote.id);
      setError('');

      const response = await api.post<QuoteConversion>(
        `/quotes/${quote.id}/convert-to-sale`,
      );

      setConvertedReceipt(response.data);
      await load();
      setSuccess(
        `Quote ${quote.quoteNumber} converted to receipt ${response.data.receiptNumber}.`,
      );
      window.setTimeout(() => setSuccess(''), 4000);
    } catch {
      setError(
        'Unable to convert this quote to a sale. It must be accepted and have enough physical stock.',
      );
    } finally {
      setBusyId(null);
    }
  };

  if (loading) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center text-gray-500">
        <Loader2 className="mr-2 animate-spin" size={20} />
        Loading quotes...
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 p-6 lg:p-8">
      <div className="mx-auto max-w-7xl">
        <div className="mb-8 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <p className="text-sm font-medium text-blue-600">Sales pipeline</p>
            <h1 className="flex items-center gap-2 text-2xl font-bold text-gray-900">
              <FileText className="text-blue-600" />
              Quotes
            </h1>
            <p className="mt-1 text-sm text-gray-500">
              Create, send, accept and track product demand before it becomes a sale.
            </p>
          </div>

          <button
            onClick={() => setCreating(true)}
            className="inline-flex items-center justify-center gap-2 rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-blue-700"
          >
            <Plus size={18} />
            New Quote
          </button>
        </div>

        {error && (
          <div className="mb-5 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
            {error}
          </div>
        )}

        {success && (
          <div className="mb-5 rounded-lg border border-green-200 bg-green-50 p-3 text-sm text-green-700">
            {success}
          </div>
        )}

        <div className="mb-6 grid gap-4 sm:grid-cols-3">
          <SummaryCard label="Quotes" value={quotes.length} />
          <SummaryCard label="Active pipeline" value={totals.active} />
          <SummaryCard
            label="Pipeline value"
            value={formatAmount(totals.pipelineValue)}
          />
        </div>

        <div className="overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm">
          <div className="overflow-x-auto">
            <table className="w-full min-w-[1280px] text-left">
              <thead className="border-b border-gray-200 bg-gray-50">
                <tr>
                  <th className="p-4 text-sm font-semibold text-gray-600">Quote</th>
                  <th className="p-4 text-sm font-semibold text-gray-600">Customer</th>
                  <th className="p-4 text-sm font-semibold text-gray-600">Salesperson</th>
                  <th className="p-4 text-sm font-semibold text-gray-600">Valid until</th>
                  <th className="p-4 text-sm font-semibold text-gray-600">Items</th>
                  <th className="p-4 text-sm font-semibold text-gray-600">Total</th>
                  <th className="p-4 text-sm font-semibold text-gray-600">Payment</th>
                  <th className="p-4 text-sm font-semibold text-gray-600">Status</th>
                  <th className="p-4 text-sm font-semibold text-gray-600">Actions</th>
                </tr>
              </thead>
              <tbody>
                {quotes.map((quote) => {
                  const expanded = expandedId === quote.id;
                  const isBusy = busyId === quote.id;

                  return (
                    <Fragment key={quote.id}>
                      <tr
                        className="border-b border-gray-100 align-top hover:bg-gray-50"
                      >
                        <td className="p-4">
                          <button
                            onClick={() =>
                              setExpandedId(expanded ? null : quote.id)
                            }
                            className="flex items-center gap-2 font-semibold text-gray-900"
                          >
                            {expanded ? <ChevronUp size={15} /> : <ChevronDown size={15} />}
                            {quote.quoteNumber}
                          </button>
                          <div className="mt-1 text-xs text-gray-400">
                            {new Date(quote.createdAt).toLocaleDateString()}
                          </div>
                        </td>
                        <td className="p-4">
                          <div className="text-sm font-medium text-gray-900">
                            {quote.customer.fullName}
                          </div>
                          <div className="mt-1 text-xs text-gray-400">
                            {quote.customer.companyName ??
                              quote.customer.email ??
                              quote.customer.phoneNumber ??
                              'No contact'}
                          </div>
                        </td>
                        <td className="p-4">
                          <div className="text-sm font-medium text-gray-800">
                            {quote.salespersonName ?? 'Not assigned'}
                          </div>
                        </td>
                        <td className="p-4 text-sm text-gray-600">
                          {new Date(quote.validUntil).toLocaleDateString()}
                        </td>
                        <td className="p-4 text-sm font-semibold text-gray-700">
                          {quote.items.reduce(
                            (sum, item) => sum + item.quantity,
                            0,
                          )}
                        </td>
                        <td className="p-4 font-semibold text-gray-900">
                          {formatAmount(quote.total)}
                        </td>
                        <td className="p-4">
                          <PaymentBadge status={quote.paymentStatus} />
                          <div className="mt-1 text-xs text-gray-400">
                            {formatAmount(quote.amountPaid)} paid
                          </div>
                        </td>
                        <td className="p-4">
                          <StatusBadge status={quote.status} />
                        </td>
                        <td className="p-4">
                          <div className="flex flex-wrap gap-2">
                            <ActionButton
                              disabled={isBusy}
                              onClick={() =>
                                window.open(
                                  `/quotes/${quote.id}/print`,
                                  '_blank',
                                  'noopener,noreferrer',
                                )
                              }
                              icon={<Printer size={13} />}
                              label="Print"
                            />

                            {quote.status !== 'Cancelled' &&
                              quote.status !== 'Rejected' &&
                              quote.status !== 'Expired' &&
                              quote.balanceDue > 0 && (
                                <ActionButton
                                  disabled={isBusy}
                                  onClick={() => setPaymentQuote(quote)}
                                  icon={<CreditCard size={13} />}
                                  label="Payment"
                                />
                              )}

                            {quote.customer.email && (
                              <ActionButton
                                disabled={isBusy}
                                onClick={() => void sendQuote(quote, 'Email')}
                                icon={<Mail size={13} />}
                                label="Email"
                              />
                            )}

                            {quote.customer.phoneNumber && (
                              <ActionButton
                                disabled={isBusy}
                                onClick={() => void sendQuote(quote, 'Sms')}
                                icon={<Smartphone size={13} />}
                                label="SMS"
                              />
                            )}

                            {(quote.customer.hasWhatsApp ||
                              quote.customer.whatsAppNumber) && (
                              <ActionButton
                                disabled={isBusy}
                                onClick={() => void sendQuote(quote, 'WhatsApp')}
                                icon={<MessageCircle size={13} />}
                                label="WhatsApp"
                              />
                            )}

                            {quote.status !== 'Accepted' &&
                              quote.status !== 'Converted' &&
                              quote.status !== 'Cancelled' &&
                              quote.status !== 'Rejected' && (
                                <ActionButton
                                  disabled={isBusy}
                                  onClick={() => void updateStatus(quote, 'Accepted')}
                                  icon={<CheckCircle2 size={13} />}
                                  label="Accept"
                                />
                              )}

                            {quote.status === 'Accepted' && (
                              <ActionButton
                                disabled={isBusy}
                                onClick={() => void convertToSale(quote)}
                                icon={<ShoppingCart size={13} />}
                                label="Convert Sale"
                              />
                            )}

                            {quote.status !== 'Converted' &&
                              quote.status !== 'Cancelled' &&
                              quote.status !== 'Rejected' && (
                                <ActionButton
                                  disabled={isBusy}
                                  onClick={() => void updateStatus(quote, 'Cancelled')}
                                  icon={<XCircle size={13} />}
                                  label="Cancel"
                                  danger
                                />
                              )}
                          </div>
                        </td>
                      </tr>

                      {expanded && (
                        <tr key={`${quote.id}-details`}>
                          <td colSpan={9} className="bg-slate-50 p-5">
                            <QuoteDetails quote={quote} />
                          </td>
                        </tr>
                      )}
                    </Fragment>
                  );
                })}

                {quotes.length === 0 && (
                  <tr>
                    <td colSpan={9} className="p-12 text-center text-sm text-gray-500">
                      No quotes yet. Create the first quote to start tracking pipeline demand.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      </div>

      {paymentQuote && (
        <RecordPaymentModal
          quote={paymentQuote}
          onClose={() => setPaymentQuote(null)}
          onRecorded={async () => {
            setPaymentQuote(null);
            await load();
            setSuccess('Payment recorded successfully.');
            window.setTimeout(() => setSuccess(''), 3000);
          }}
        />
      )}

      {convertedReceipt && (
        <ConvertedReceiptModal
          conversion={convertedReceipt}
          onClose={() => setConvertedReceipt(null)}
        />
      )}

      {creating && (
        <CreateQuoteModal
          products={products}
          staff={staff}
          business={business}
          canAssignSalesperson={canAssignSalesperson}
          currentSalespersonName={
            session.name ?? session.email ?? 'Signed-in staff member'
          }
          onClose={() => setCreating(false)}
          onCreated={async (message) => {
            setCreating(false);
            await load();
            setSuccess(message);
            window.setTimeout(() => setSuccess(''), 4500);
          }}
        />
      )}
    </div>
  );
}

function RecordPaymentModal({
  quote,
  onClose,
  onRecorded,
}: {
  quote: Quote;
  onClose: () => void;
  onRecorded: () => Promise<void>;
}) {
  const [amount, setAmount] = useState(
    quote.depositRequired > quote.amountPaid
      ? String(quote.depositRequired - quote.amountPaid)
      : String(quote.balanceDue),
  );
  const [method, setMethod] = useState('Bank Transfer');
  const [reference, setReference] = useState('');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  const submit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const parsedAmount = Number(amount);

    if (!Number.isFinite(parsedAmount) || parsedAmount <= 0) {
      setError('Enter a valid payment amount.');
      return;
    }

    try {
      setSaving(true);
      setError('');

      await api.post(`/quotes/${quote.id}/payments`, {
        amount: parsedAmount,
        method,
        reference: reference.trim() || null,
        paidAt: null,
      });

      await onRecorded();
    } catch {
      setError('Unable to record payment. Check the outstanding balance.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="fixed inset-0 z-[70] flex items-center justify-center bg-black/40 p-4">
      <div className="w-full max-w-md rounded-2xl bg-white shadow-2xl">
        <div className="flex items-start justify-between border-b border-gray-200 px-6 py-5">
          <div>
            <p className="text-sm font-medium text-blue-600">Quote payment</p>
            <h2 className="mt-1 text-xl font-bold text-gray-900">
              Record Payment
            </h2>
            <p className="mt-1 text-sm text-gray-500">
              {quote.quoteNumber} · Balance {formatAmount(quote.balanceDue)}
            </p>
          </div>
          <button onClick={onClose} className="rounded-lg p-2 text-gray-400 hover:bg-gray-100">
            <X size={20} />
          </button>
        </div>

        <form onSubmit={submit} className="space-y-4 p-6">
          {error && (
            <div className="rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
              {error}
            </div>
          )}

          <Input
            label="Amount"
            type="number"
            value={amount}
            onChange={setAmount}
            required
          />

          <label className="block">
            <span className="mb-2 block text-sm font-medium text-gray-700">
              Method
            </span>
            <select
              value={method}
              onChange={(event) => setMethod(event.target.value)}
              className="w-full rounded-lg border border-gray-300 px-3 py-2.5 text-sm text-gray-900"
            >
              <option>Bank Transfer</option>
              <option>Cash</option>
              <option>Card</option>
              <option>Mobile Payment</option>
              <option>Other</option>
            </select>
          </label>

          <Input
            label="Reference"
            value={reference}
            onChange={setReference}
          />

          <button
            type="submit"
            disabled={saving}
            className="flex w-full items-center justify-center gap-2 rounded-lg bg-blue-600 px-4 py-3 text-sm font-semibold text-white disabled:bg-blue-400"
          >
            {saving ? <Loader2 className="animate-spin" size={17} /> : <CreditCard size={17} />}
            {saving ? 'Recording...' : 'Record Payment'}
          </button>
        </form>
      </div>
    </div>
  );
}

function ConvertedReceiptModal({
  conversion,
  onClose,
}: {
  conversion: QuoteConversion;
  onClose: () => void;
}) {
  const [busy, setBusy] = useState('');
  const [error, setError] = useState('');

  const prepare = async (channel: 'Email' | 'Sms' | 'WhatsApp') => {
    try {
      setBusy(channel);
      setError('');

      const response = await api.post<PreparedDelivery>(
        `/sales/${conversion.primarySaleId}/receipt/delivery`,
        { channel },
      );

      if (response.data.actionUrl) {
        window.location.href = response.data.actionUrl;
      }
    } catch {
      setError(
        `Unable to prepare ${channel} receipt. Check the customer contact details.`,
      );
    } finally {
      setBusy('');
    }
  };

  const customer = conversion.customer;

  return (
    <div className="fixed inset-0 z-[60] flex items-center justify-center bg-black/40 p-4">
      <div className="w-full max-w-lg rounded-2xl bg-white shadow-2xl">
        <div className="flex items-start justify-between border-b border-gray-200 px-6 py-5">
          <div>
            <p className="text-sm font-medium text-green-600">Quote converted</p>
            <h2 className="mt-1 text-xl font-bold text-gray-900">
              Send sale receipt?
            </h2>
            <p className="mt-1 text-sm text-gray-500">
              {conversion.receiptNumber} · {customer.fullName}
            </p>
          </div>
          <button onClick={onClose} className="rounded-lg p-2 text-gray-400 hover:bg-gray-100">
            <X size={20} />
          </button>
        </div>

        <div className="p-6">
          {error && (
            <div className="mb-4 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
              {error}
            </div>
          )}

          <div className="grid gap-3 sm:grid-cols-3">
            <ActionButton
              label="Email"
              icon={<Mail size={16} />}
              disabled={!customer.email || Boolean(busy)}
              onClick={() => void prepare('Email')}
            />
            <ActionButton
              label="SMS"
              icon={<Smartphone size={16} />}
              disabled={!customer.phoneNumber || Boolean(busy)}
              onClick={() => void prepare('Sms')}
            />
            <ActionButton
              label="WhatsApp"
              icon={<MessageCircle size={16} />}
              disabled={
                (!customer.whatsAppNumber &&
                  !(customer.hasWhatsApp && customer.phoneNumber)) ||
                Boolean(busy)
              }
              onClick={() => void prepare('WhatsApp')}
            />
          </div>

          <div className="mt-5 grid gap-3 sm:grid-cols-2">
            <button
              onClick={() =>
                window.open(
                  `/receipts/${conversion.primarySaleId}/print`,
                  '_blank',
                  'noopener,noreferrer',
                )
              }
              className="inline-flex items-center justify-center gap-2 rounded-lg border border-blue-200 bg-blue-50 px-4 py-2.5 text-sm font-semibold text-blue-700"
            >
              <Printer size={16} />
              Print Receipt
            </button>
            <button
              onClick={onClose}
              className="rounded-lg border border-gray-300 px-4 py-2.5 text-sm font-semibold text-gray-700"
            >
              Done
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

function CreateQuoteModal({
  products,
  staff,
  business,
  canAssignSalesperson,
  currentSalespersonName,
  onClose,
  onCreated,
}: {
  products: Product[];
  staff: StaffMember[];
  business: BusinessProfile | null;
  canAssignSalesperson: boolean;
  currentSalespersonName: string;
  onClose: () => void;
  onCreated: (message: string) => Promise<void>;
}) {
  const [customerName, setCustomerName] = useState('');
  const [customerCompanyName, setCustomerCompanyName] = useState('');
  const [customerEmail, setCustomerEmail] = useState('');
  const [customerPhoneNumber, setCustomerPhoneNumber] = useState('');
  const [customerWhatsAppNumber, setCustomerWhatsAppNumber] = useState('');
  const [customerHasWhatsApp, setCustomerHasWhatsApp] = useState(false);
  const [customerAddress, setCustomerAddress] = useState('');
  const [salespersonId, setSalespersonId] = useState('');
  const [validUntil, setValidUntil] = useState('');
  const [notes, setNotes] = useState('');
  const [depositPercentage, setDepositPercentage] = useState('0');
  const [sendEmail, setSendEmail] = useState(false);
  const [sendSms, setSendSms] = useState(false);
  const [sendWhatsApp, setSendWhatsApp] = useState(false);
  const [items, setItems] = useState<QuoteFormItem[]>([
    { productId: '', quantity: '1', unitPrice: '' },
  ]);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  const currency = business?.currencyCode ?? 'ZAR';

  const pricing = useMemo(() => {
    const subtotal = items.reduce((sum, item) => {
      const product = products.find(
        (entry) => entry.id === item.productId,
      );
      const quantity = Number(item.quantity);
      const enteredPrice = Number(item.unitPrice);
      const unitPrice = item.unitPrice
        ? enteredPrice
        : product?.price ?? 0;

      if (
        !product ||
        !Number.isFinite(quantity) ||
        quantity <= 0 ||
        !Number.isFinite(unitPrice) ||
        unitPrice <= 0
      ) {
        return sum;
      }

      return sum + quantity * unitPrice;
    }, 0);

    const vatRate = business?.isVatRegistered
      ? business.defaultVatRate
      : 0;
    const vatAmount = roundMoney(subtotal * (vatRate / 100));
    const total = roundMoney(subtotal + vatAmount);

    const parsedPercentage = Number(depositPercentage);
    const safePercentage = Number.isFinite(parsedPercentage)
      ? Math.min(100, Math.max(0, parsedPercentage))
      : 0;

    const depositAmount = roundMoney(
      total * (safePercentage / 100),
    );

    return {
      subtotal,
      vatRate,
      vatAmount,
      total,
      depositPercentage: safePercentage,
      depositAmount,
      balanceAfterDeposit: roundMoney(total - depositAmount),
    };
  }, [business, depositPercentage, items, products]);

  const addItem = () => {
    setItems((current) => [
      ...current,
      { productId: '', quantity: '1', unitPrice: '' },
    ]);
  };

  const updateItem = (
    index: number,
    field: keyof QuoteFormItem,
    value: string,
  ) => {
    setItems((current) =>
      current.map((item, itemIndex) =>
        itemIndex === index ? { ...item, [field]: value } : item,
      ),
    );
  };

  const removeItem = (index: number) => {
    setItems((current) =>
      current.filter((_, itemIndex) => itemIndex !== index),
    );
  };

  const submit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError('');

    const preparedItems = items.map((item) => ({
      productId: item.productId,
      quantity: Number(item.quantity),
      unitPrice: item.unitPrice ? Number(item.unitPrice) : null,
    }));

    if (
      !customerName.trim() ||
      preparedItems.length === 0 ||
      preparedItems.some(
        (item) =>
          !item.productId ||
          !Number.isInteger(item.quantity) ||
          item.quantity <= 0,
      )
    ) {
      setError('Customer name and valid quote items are required.');
      return;
    }

    const rawDepositPercentage = Number(depositPercentage);

    if (
      !Number.isFinite(rawDepositPercentage) ||
      rawDepositPercentage < 0 ||
      rawDepositPercentage > 100
    ) {
      setError('Deposit percentage must be between 0% and 100%.');
      return;
    }

    if (sendEmail && !customerEmail.trim()) {
      setError('Add a customer email address before selecting automatic email.');
      return;
    }

    if (sendSms && !customerPhoneNumber.trim()) {
      setError('Add a customer phone number before selecting automatic SMS.');
      return;
    }

    if (
      sendWhatsApp &&
      !customerWhatsAppNumber.trim() &&
      !(customerHasWhatsApp && customerPhoneNumber.trim())
    ) {
      setError(
        'Add a WhatsApp number, or mark the customer as using WhatsApp with a phone number.',
      );
      return;
    }

    try {
      setSaving(true);

      const createResponse = await api.post<{ id: string }>('/quotes', {
        customerId: null,
        customerName: customerName.trim(),
        customerCompanyName: customerCompanyName.trim() || null,
        customerEmail: customerEmail.trim() || null,
        customerPhoneNumber: customerPhoneNumber.trim() || null,
        customerWhatsAppNumber:
          customerWhatsAppNumber.trim() || null,
        customerHasWhatsApp,
        customerAddress: customerAddress.trim() || null,
        salespersonId: salespersonId || null,
        validUntil: validUntil || null,
        notes: notes.trim() || null,
        depositPercentage: pricing.depositPercentage,
        depositRequired: null,
        items: preparedItems,
      });

      const channels: Array<'Email' | 'Sms' | 'WhatsApp'> = [];
      if (sendEmail) channels.push('Email');
      if (sendSms) channels.push('Sms');
      if (sendWhatsApp) channels.push('WhatsApp');

      const deliveryMessages: string[] = [];

      for (const channel of channels) {
        try {
          const delivery = await api.post<PreparedDelivery>(
            `/quotes/${createResponse.data.id}/delivery`,
            { channel },
          );

          if (delivery.data.status === 'Sent') {
            deliveryMessages.push(`${channel}: sent`);
          } else if (
            delivery.data.status === 'PendingProviderConfiguration'
          ) {
            deliveryMessages.push(
              `${channel}: provider setup required`,
            );
          } else {
            deliveryMessages.push(`${channel}: failed`);
          }
        } catch {
          deliveryMessages.push(`${channel}: failed`);
        }
      }

      const deliverySummary =
        deliveryMessages.length > 0
          ? ` · ${deliveryMessages.join(' · ')}`
          : '';

      await onCreated(
        `Quote created successfully${deliverySummary}`,
      );
    } catch {
      setError(
        'Unable to create quote. Check the customer, products, deposit and quantities.',
      );
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto bg-black/40 p-4">
      <div className="mx-auto my-6 w-full max-w-5xl rounded-2xl bg-white shadow-2xl">
        <div className="flex items-center justify-between border-b border-gray-200 px-6 py-5">
          <div>
            <p className="text-sm font-medium text-blue-600">
              Sales pipeline
            </p>
            <h2 className="mt-1 text-xl font-bold text-gray-900">
              Create Quote
            </h2>
          </div>
          <button
            onClick={onClose}
            type="button"
            className="rounded-lg p-2 text-gray-400 hover:bg-gray-100 hover:text-gray-700"
          >
            <X size={20} />
          </button>
        </div>

        <form onSubmit={submit} className="space-y-6 p-6">
          {error && (
            <div className="rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
              {error}
            </div>
          )}

          <section>
            <div className="mb-3 flex items-center justify-between gap-4">
              <h3 className="font-semibold text-gray-900">
                Customer & salesperson
              </h3>
              <span className="text-xs text-gray-400">
                Salesperson is stored on the quote and eventual sale.
              </span>
            </div>

            <div className="grid gap-4 md:grid-cols-2">
              <Input
                label="Customer name"
                required
                value={customerName}
                onChange={setCustomerName}
              />
              <Input
                label="Company"
                value={customerCompanyName}
                onChange={setCustomerCompanyName}
              />

              {canAssignSalesperson ? (
                <label className="block">
                  <span className="mb-2 block text-sm font-medium text-gray-700">
                    Salesperson
                  </span>
                  <select
                    value={salespersonId}
                    onChange={(event) =>
                      setSalespersonId(event.target.value)
                    }
                    className="w-full rounded-lg border border-gray-300 px-3 py-2.5 text-sm text-gray-900 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
                  >
                    <option value="">Current signed-in user</option>
                    {staff.map((member) => (
                      <option key={member.id} value={member.id}>
                        {member.fullName} — {member.role}
                      </option>
                    ))}
                  </select>
                </label>
              ) : (
                <div className="rounded-lg border border-gray-200 bg-gray-50 px-4 py-3">
                  <p className="text-xs font-semibold uppercase tracking-wide text-gray-400">
                    Salesperson
                  </p>
                  <p className="mt-1 text-sm font-semibold text-gray-900">
                    {currentSalespersonName}
                  </p>
                </div>
              )}

              <Input
                label="Email"
                type="email"
                value={customerEmail}
                onChange={setCustomerEmail}
              />
              <Input
                label="Phone"
                value={customerPhoneNumber}
                onChange={setCustomerPhoneNumber}
              />
              <Input
                label="WhatsApp number"
                value={customerWhatsAppNumber}
                onChange={setCustomerWhatsAppNumber}
              />

              <label className="flex items-center gap-3 rounded-lg border border-gray-200 px-4 py-3">
                <input
                  type="checkbox"
                  checked={customerHasWhatsApp}
                  onChange={(event) =>
                    setCustomerHasWhatsApp(event.target.checked)
                  }
                />
                <span className="text-sm font-medium text-gray-700">
                  Customer uses WhatsApp
                </span>
              </label>

              <div className="md:col-span-2">
                <Input
                  label="Customer address"
                  value={customerAddress}
                  onChange={setCustomerAddress}
                />
              </div>
            </div>
          </section>

          <section>
            <div className="mb-3 flex items-center justify-between">
              <h3 className="font-semibold text-gray-900">Products</h3>
              <button
                type="button"
                onClick={addItem}
                className="inline-flex items-center gap-1 text-sm font-semibold text-blue-600"
              >
                <Plus size={15} />
                Add item
              </button>
            </div>

            <div className="space-y-3">
              {items.map((item, index) => {
                const product = products.find(
                  (entry) => entry.id === item.productId,
                );

                return (
                  <div
                    key={index}
                    className="grid gap-3 rounded-lg border border-gray-200 p-4 md:grid-cols-[1fr_120px_170px_auto]"
                  >
                    <select
                      required
                      value={item.productId}
                      onChange={(event) =>
                        updateItem(
                          index,
                          'productId',
                          event.target.value,
                        )
                      }
                      className="rounded-lg border border-gray-300 px-3 py-2.5 text-sm text-gray-900"
                    >
                      <option value="">Select product</option>
                      {products.map((entry) => (
                        <option key={entry.id} value={entry.id}>
                          {entry.name} — {entry.availableQuantity} available
                        </option>
                      ))}
                    </select>

                    <input
                      required
                      type="number"
                      min="1"
                      step="1"
                      value={item.quantity}
                      onChange={(event) =>
                        updateItem(
                          index,
                          'quantity',
                          event.target.value,
                        )
                      }
                      className="rounded-lg border border-gray-300 px-3 py-2.5 text-sm text-gray-900"
                      placeholder="Qty"
                    />

                    <input
                      type="number"
                      min="0.01"
                      step="0.01"
                      value={item.unitPrice}
                      onChange={(event) =>
                        updateItem(
                          index,
                          'unitPrice',
                          event.target.value,
                        )
                      }
                      className="rounded-lg border border-gray-300 px-3 py-2.5 text-sm text-gray-900"
                      placeholder={
                        product
                          ? `Default ${currency} ${product.price.toFixed(2)}`
                          : 'Unit price'
                      }
                    />

                    <button
                      type="button"
                      onClick={() => removeItem(index)}
                      disabled={items.length === 1}
                      className="rounded-lg border border-red-200 px-3 text-red-600 disabled:opacity-30"
                    >
                      <X size={16} />
                    </button>
                  </div>
                );
              })}
            </div>
          </section>

          <section className="grid gap-5 lg:grid-cols-[1fr_360px]">
            <div className="space-y-4">
              <div className="grid gap-4 md:grid-cols-2">
                <Input
                  label="Valid until"
                  type="date"
                  value={validUntil}
                  onChange={setValidUntil}
                />

                <label className="block">
                  <span className="mb-2 block text-sm font-medium text-gray-700">
                    Deposit percentage
                  </span>
                  <div className="relative">
                    <input
                      type="number"
                      min="0"
                      max="100"
                      step="0.01"
                      value={depositPercentage}
                      onChange={(event) =>
                        setDepositPercentage(event.target.value)
                      }
                      className="w-full rounded-lg border border-gray-300 px-3 py-2.5 pr-9 text-sm text-gray-900 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
                    />
                    <span className="absolute right-3 top-1/2 -translate-y-1/2 text-sm text-gray-400">
                      %
                    </span>
                  </div>
                  <div className="mt-2 flex flex-wrap gap-2">
                    {[20, 30, 50].map((percentage) => (
                      <button
                        key={percentage}
                        type="button"
                        onClick={() =>
                          setDepositPercentage(String(percentage))
                        }
                        className="rounded-full border border-gray-200 px-2.5 py-1 text-xs font-semibold text-gray-600 hover:border-blue-300 hover:text-blue-700"
                      >
                        {percentage}%
                      </button>
                    ))}
                  </div>
                </label>
              </div>

              <label className="block">
                <span className="mb-2 block text-sm font-medium text-gray-700">
                  Notes
                </span>
                <textarea
                  rows={4}
                  value={notes}
                  onChange={(event) => setNotes(event.target.value)}
                  className="w-full rounded-lg border border-gray-300 px-3 py-2.5 text-sm text-gray-900 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
                />
              </label>

              <div className="rounded-xl border border-gray-200 bg-gray-50 p-4">
                <p className="text-sm font-semibold text-gray-900">
                  Send immediately after creation
                </p>
                <p className="mt-1 text-xs text-gray-500">
                  Configured providers send automatically. If a provider is not
                  configured, the quote is still created and marked accordingly.
                </p>

                <div className="mt-4 grid gap-3 sm:grid-cols-3">
                  <ChannelCheck
                    label="Email"
                    checked={sendEmail}
                    onChange={setSendEmail}
                    disabled={!customerEmail.trim()}
                  />
                  <ChannelCheck
                    label="SMS"
                    checked={sendSms}
                    onChange={setSendSms}
                    disabled={!customerPhoneNumber.trim()}
                  />
                  <ChannelCheck
                    label="WhatsApp"
                    checked={sendWhatsApp}
                    onChange={setSendWhatsApp}
                    disabled={
                      !customerWhatsAppNumber.trim() &&
                      !(customerHasWhatsApp &&
                        customerPhoneNumber.trim())
                    }
                  />
                </div>
              </div>
            </div>

            <div className="h-fit rounded-xl border border-blue-100 bg-blue-50 p-5">
              <p className="text-sm font-semibold text-blue-950">
                Quote total
              </p>

              <div className="mt-4 space-y-3 text-sm">
                <QuoteMoneyRow
                  label="Subtotal"
                  value={pricing.subtotal}
                  currency={currency}
                />
                {pricing.vatRate > 0 && (
                  <QuoteMoneyRow
                    label={`VAT (${pricing.vatRate}%)`}
                    value={pricing.vatAmount}
                    currency={currency}
                  />
                )}
                <div className="flex items-center justify-between border-t border-blue-200 pt-3">
                  <span className="font-semibold text-blue-900">
                    Total
                  </span>
                  <span className="text-xl font-bold text-blue-950">
                    {money(pricing.total, currency)}
                  </span>
                </div>
                <QuoteMoneyRow
                  label={`Deposit (${pricing.depositPercentage}%)`}
                  value={pricing.depositAmount}
                  currency={currency}
                  strong
                />
                <QuoteMoneyRow
                  label="Balance after deposit"
                  value={pricing.balanceAfterDeposit}
                  currency={currency}
                  strong
                />
              </div>
            </div>
          </section>

          <div className="flex justify-end gap-3 border-t border-gray-100 pt-5">
            <button
              type="button"
              onClick={onClose}
              className="rounded-lg border border-gray-300 px-4 py-2.5 text-sm font-semibold text-gray-700"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={saving || pricing.total <= 0}
              className="inline-flex items-center gap-2 rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white disabled:bg-blue-400"
            >
              {saving ? (
                <Loader2 className="animate-spin" size={17} />
              ) : (
                <FileText size={17} />
              )}
              {saving ? 'Creating...' : 'Create Quote'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

function ChannelCheck({
  label,
  checked,
  onChange,
  disabled,
}: {
  label: string;
  checked: boolean;
  onChange: (value: boolean) => void;
  disabled: boolean;
}) {
  return (
    <label
      className={
        disabled
          ? 'flex cursor-not-allowed items-center gap-2 rounded-lg border border-gray-200 bg-white px-3 py-2.5 text-sm text-gray-300'
          : 'flex cursor-pointer items-center gap-2 rounded-lg border border-gray-200 bg-white px-3 py-2.5 text-sm font-medium text-gray-700'
      }
    >
      <input
        type="checkbox"
        checked={checked}
        disabled={disabled}
        onChange={(event) => onChange(event.target.checked)}
      />
      {label}
    </label>
  );
}

function QuoteMoneyRow({
  label,
  value,
  currency,
  strong = false,
}: {
  label: string;
  value: number;
  currency: string;
  strong?: boolean;
}) {
  return (
    <div className="flex items-center justify-between gap-4">
      <span className={strong ? 'font-semibold text-blue-900' : 'text-blue-800'}>
        {label}
      </span>
      <span className={strong ? 'font-bold text-blue-950' : 'font-medium text-blue-950'}>
        {money(value, currency)}
      </span>
    </div>
  );
}

function roundMoney(value: number) {
  return Math.round((value + Number.EPSILON) * 100) / 100;
}

function money(value: number, currency: string) {
  return `${currency} ${value.toLocaleString(undefined, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })}`;
}

function QuoteDetails({ quote }: { quote: Quote }) {
  return (
    <div className="grid gap-5 lg:grid-cols-[1.5fr_1fr]">
      <div>
        <h3 className="mb-3 text-sm font-semibold text-gray-900">Quote items</h3>
        <div className="overflow-hidden rounded-lg border border-gray-200 bg-white">
          <table className="w-full text-left text-sm">
            <thead className="bg-gray-50 text-gray-500">
              <tr>
                <th className="p-3">Product</th>
                <th className="p-3">Qty</th>
                <th className="p-3">Price</th>
                <th className="p-3">Line total</th>
                <th className="p-3">Available</th>
              </tr>
            </thead>
            <tbody>
              {quote.items.map((item) => (
                <tr key={item.id} className="border-t border-gray-100">
                  <td className="p-3 font-medium text-gray-900">{item.productName}</td>
                  <td className="p-3">{item.quantity}</td>
                  <td className="p-3">{formatAmount(item.unitPrice)}</td>
                  <td className="p-3 font-semibold">{formatAmount(item.lineTotal)}</td>
                  <td className="p-3">{item.availableQuantity}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      <div className="rounded-lg border border-gray-200 bg-white p-4">
        <div className="space-y-2 text-sm">
          <MoneyRow label="Subtotal" value={quote.subtotal} />
          {quote.vatAmount > 0 && (
            <MoneyRow label={`VAT (${quote.vatRate}%)`} value={quote.vatAmount} />
          )}
          <div className="flex justify-between border-t border-gray-200 pt-3 text-base font-bold text-gray-900">
            <span>Total</span>
            <span>{formatAmount(quote.total)}</span>
          </div>
          {quote.depositRequired > 0 && (
            <MoneyRow
              label={`Deposit required (${quote.depositPercentage}%)`}
              value={quote.depositRequired}
            />
          )}
          <MoneyRow label="Amount paid" value={quote.amountPaid} />
          <MoneyRow label="Balance due" value={quote.balanceDue} />
          <div className="pt-2">
            <PaymentBadge status={quote.paymentStatus} />
          </div>
        </div>

        {quote.salespersonName && (
          <div className="mt-4 rounded-lg bg-blue-50 p-3 text-sm text-blue-800">
            Salesperson: <strong>{quote.salespersonName}</strong>
          </div>
        )}

        {quote.payments.length > 0 && (
          <div className="mt-4 border-t border-gray-100 pt-4">
            <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-gray-400">
              Payments
            </p>
            <div className="space-y-2">
              {quote.payments.map((payment) => (
                <div
                  key={payment.id}
                  className="flex items-center justify-between rounded-lg bg-gray-50 px-3 py-2 text-xs"
                >
                  <div>
                    <span className="font-semibold text-gray-800">
                      {payment.method}
                    </span>
                    {payment.reference && (
                      <span className="ml-2 text-gray-400">
                        {payment.reference}
                      </span>
                    )}
                  </div>
                  <span className="font-bold text-gray-900">
                    {formatAmount(payment.amount)}
                  </span>
                </div>
              ))}
            </div>
          </div>
        )}

        {quote.notes && (
          <div className="mt-4 rounded-lg bg-gray-50 p-3 text-sm text-gray-600">
            {quote.notes}
          </div>
        )}
      </div>
    </div>
  );
}

function PaymentBadge({ status }: { status: string }) {
  const styles: Record<string, string> = {
    Paid: 'bg-green-50 text-green-700',
    'Deposit Paid': 'bg-blue-50 text-blue-700',
    'Partially Paid': 'bg-amber-50 text-amber-700',
    'Deposit Outstanding': 'bg-red-50 text-red-700',
    Unpaid: 'bg-gray-100 text-gray-600',
  };

  return (
    <span
      className={`rounded-full px-2.5 py-1 text-xs font-bold ${styles[status] ?? styles.Unpaid}`}
    >
      {status}
    </span>
  );
}

function StatusBadge({ status }: { status: string }) {
  const styles: Record<string, string> = {
    Draft: 'bg-gray-100 text-gray-700',
    Sent: 'bg-blue-50 text-blue-700',
    Accepted: 'bg-green-50 text-green-700',
    Rejected: 'bg-red-50 text-red-700',
    Expired: 'bg-amber-50 text-amber-700',
    Cancelled: 'bg-gray-100 text-gray-500',
    Converted: 'bg-violet-50 text-violet-700',
  };

  return (
    <span className={`rounded-full px-2.5 py-1 text-xs font-bold ${styles[status] ?? styles.Draft}`}>
      {status}
    </span>
  );
}

function ActionButton({
  label,
  icon,
  onClick,
  disabled,
  danger = false,
}: {
  label: string;
  icon: React.ReactNode;
  onClick: () => void;
  disabled: boolean;
  danger?: boolean;
}) {
  return (
    <button
      type="button"
      disabled={disabled}
      onClick={onClick}
      className={
        danger
          ? 'inline-flex items-center gap-1 rounded-md border border-red-200 bg-red-50 px-2.5 py-1.5 text-xs font-semibold text-red-700 disabled:opacity-40'
          : 'inline-flex items-center gap-1 rounded-md border border-gray-300 bg-white px-2.5 py-1.5 text-xs font-semibold text-gray-700 hover:bg-gray-50 disabled:opacity-40'
      }
    >
      {icon}
      {label}
    </button>
  );
}

function Input({
  label,
  value,
  onChange,
  type = 'text',
  required = false,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  type?: string;
  required?: boolean;
}) {
  return (
    <label className="block">
      <span className="mb-2 block text-sm font-medium text-gray-700">{label}</span>
      <input
        type={type}
        required={required}
        value={value}
        onChange={(event) => onChange(event.target.value)}
        className="w-full rounded-lg border border-gray-300 px-3 py-2.5 text-sm text-gray-900 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
      />
    </label>
  );
}

function SummaryCard({
  label,
  value,
}: {
  label: string;
  value: string | number;
}) {
  return (
    <div className="rounded-xl border border-gray-200 bg-white p-5 shadow-sm">
      <p className="text-sm text-gray-500">{label}</p>
      <p className="mt-1 text-2xl font-bold text-gray-900">{value}</p>
    </div>
  );
}

function MoneyRow({ label, value }: { label: string; value: number }) {
  return (
    <div className="flex justify-between text-gray-600">
      <span>{label}</span>
      <span className="font-medium text-gray-900">{formatAmount(value)}</span>
    </div>
  );
}

function formatAmount(value: number) {
  return value.toLocaleString(undefined, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  });
}
