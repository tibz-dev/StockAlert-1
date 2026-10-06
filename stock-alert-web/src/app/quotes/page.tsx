'use client';

import { Fragment, useCallback, useEffect, useMemo, useState } from 'react';
import {
  CheckCircle2,
  ChevronDown,
  ChevronUp,
  FileText,
  Loader2,
  Mail,
  MessageCircle,
  Plus,
  ShoppingCart,
  Smartphone,
  X,
  XCircle,
} from 'lucide-react';
import api from '@/lib/api';
import type { Product } from '@/types/inventory';
import type { PreparedDelivery, Quote, QuoteConversion } from '@/types/quote';

interface QuoteFormItem {
  productId: string;
  quantity: string;
  unitPrice: string;
}

export default function QuotesPage() {
  const [quotes, setQuotes] = useState<Quote[]>([]);
  const [products, setProducts] = useState<Product[]>([]);
  const [loading, setLoading] = useState(true);
  const [creating, setCreating] = useState(false);
  const [expandedId, setExpandedId] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [convertedReceipt, setConvertedReceipt] =
    useState<QuoteConversion | null>(null);

  const load = useCallback(async () => {
    try {
      setError('');
      const [quotesResponse, productsResponse] = await Promise.all([
        api.get<Quote[]>('/quotes'),
        api.get<Product[]>('/products'),
      ]);

      setQuotes(quotesResponse.data);
      setProducts(productsResponse.data);
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

      if (response.data.actionUrl) {
        window.location.href = response.data.actionUrl;
      }

      setSuccess(
        `${channel} message prepared for ${quote.customer.fullName}.`,
      );
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
            <table className="w-full min-w-[1100px] text-left">
              <thead className="border-b border-gray-200 bg-gray-50">
                <tr>
                  <th className="p-4 text-sm font-semibold text-gray-600">Quote</th>
                  <th className="p-4 text-sm font-semibold text-gray-600">Customer</th>
                  <th className="p-4 text-sm font-semibold text-gray-600">Valid until</th>
                  <th className="p-4 text-sm font-semibold text-gray-600">Items</th>
                  <th className="p-4 text-sm font-semibold text-gray-600">Total</th>
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
                          <StatusBadge status={quote.status} />
                        </td>
                        <td className="p-4">
                          <div className="flex flex-wrap gap-2">
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
                          <td colSpan={7} className="bg-slate-50 p-5">
                            <QuoteDetails quote={quote} />
                          </td>
                        </tr>
                      )}
                    </Fragment>
                  );
                })}

                {quotes.length === 0 && (
                  <tr>
                    <td colSpan={7} className="p-12 text-center text-sm text-gray-500">
                      No quotes yet. Create the first quote to start tracking pipeline demand.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      </div>

      {convertedReceipt && (
        <ConvertedReceiptModal
          conversion={convertedReceipt}
          onClose={() => setConvertedReceipt(null)}
        />
      )}

      {creating && (
        <CreateQuoteModal
          products={products}
          onClose={() => setCreating(false)}
          onCreated={async () => {
            setCreating(false);
            await load();
            setSuccess('Quote created successfully.');
            window.setTimeout(() => setSuccess(''), 3000);
          }}
        />
      )}
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

          <button
            onClick={onClose}
            className="mt-5 w-full rounded-lg border border-gray-300 px-4 py-2.5 text-sm font-semibold text-gray-700"
          >
            Done
          </button>
        </div>
      </div>
    </div>
  );
}

function CreateQuoteModal({
  products,
  onClose,
  onCreated,
}: {
  products: Product[];
  onClose: () => void;
  onCreated: () => Promise<void>;
}) {
  const [customerName, setCustomerName] = useState('');
  const [customerCompanyName, setCustomerCompanyName] = useState('');
  const [customerEmail, setCustomerEmail] = useState('');
  const [customerPhoneNumber, setCustomerPhoneNumber] = useState('');
  const [customerWhatsAppNumber, setCustomerWhatsAppNumber] = useState('');
  const [customerHasWhatsApp, setCustomerHasWhatsApp] = useState(false);
  const [customerAddress, setCustomerAddress] = useState('');
  const [validUntil, setValidUntil] = useState('');
  const [notes, setNotes] = useState('');
  const [items, setItems] = useState<QuoteFormItem[]>([
    { productId: '', quantity: '1', unitPrice: '' },
  ]);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

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
    setItems((current) => current.filter((_, itemIndex) => itemIndex !== index));
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

    try {
      setSaving(true);

      await api.post('/quotes', {
        customerId: null,
        customerName: customerName.trim(),
        customerCompanyName: customerCompanyName.trim() || null,
        customerEmail: customerEmail.trim() || null,
        customerPhoneNumber: customerPhoneNumber.trim() || null,
        customerWhatsAppNumber: customerWhatsAppNumber.trim() || null,
        customerHasWhatsApp,
        customerAddress: customerAddress.trim() || null,
        validUntil: validUntil || null,
        notes: notes.trim() || null,
        items: preparedItems,
      });

      await onCreated();
    } catch {
      setError(
        'Unable to create quote. Check the customer, products and quantities.',
      );
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto bg-black/40 p-4">
      <div className="mx-auto my-6 w-full max-w-4xl rounded-2xl bg-white shadow-2xl">
        <div className="flex items-center justify-between border-b border-gray-200 px-6 py-5">
          <div>
            <p className="text-sm font-medium text-blue-600">Sales pipeline</p>
            <h2 className="mt-1 text-xl font-bold text-gray-900">Create Quote</h2>
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
            <h3 className="mb-3 font-semibold text-gray-900">Customer</h3>
            <div className="grid gap-4 md:grid-cols-2">
              <Input label="Customer name" required value={customerName} onChange={setCustomerName} />
              <Input label="Company" value={customerCompanyName} onChange={setCustomerCompanyName} />
              <Input label="Email" type="email" value={customerEmail} onChange={setCustomerEmail} />
              <Input label="Phone" value={customerPhoneNumber} onChange={setCustomerPhoneNumber} />
              <Input label="WhatsApp number" value={customerWhatsAppNumber} onChange={setCustomerWhatsAppNumber} />
              <label className="flex items-center gap-3 rounded-lg border border-gray-200 px-4 py-3">
                <input
                  type="checkbox"
                  checked={customerHasWhatsApp}
                  onChange={(event) => setCustomerHasWhatsApp(event.target.checked)}
                />
                <span className="text-sm font-medium text-gray-700">
                  Customer uses WhatsApp
                </span>
              </label>
              <div className="md:col-span-2">
                <Input label="Customer address" value={customerAddress} onChange={setCustomerAddress} />
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
                    className="grid gap-3 rounded-lg border border-gray-200 p-4 md:grid-cols-[1fr_130px_160px_auto]"
                  >
                    <select
                      required
                      value={item.productId}
                      onChange={(event) =>
                        updateItem(index, 'productId', event.target.value)
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
                        updateItem(index, 'quantity', event.target.value)
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
                        updateItem(index, 'unitPrice', event.target.value)
                      }
                      className="rounded-lg border border-gray-300 px-3 py-2.5 text-sm text-gray-900"
                      placeholder={
                        product ? `Default ${product.price.toFixed(2)}` : 'Unit price'
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

          <section className="grid gap-4 md:grid-cols-2">
            <Input
              label="Valid until"
              type="date"
              value={validUntil}
              onChange={setValidUntil}
            />
            <label className="block">
              <span className="mb-2 block text-sm font-medium text-gray-700">
                Notes
              </span>
              <textarea
                rows={3}
                value={notes}
                onChange={(event) => setNotes(event.target.value)}
                className="w-full rounded-lg border border-gray-300 px-3 py-2.5 text-sm text-gray-900"
              />
            </label>
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
              disabled={saving}
              className="inline-flex items-center gap-2 rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white disabled:bg-blue-400"
            >
              {saving ? <Loader2 className="animate-spin" size={17} /> : <FileText size={17} />}
              {saving ? 'Creating...' : 'Create Quote'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
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
        </div>

        {quote.notes && (
          <div className="mt-4 rounded-lg bg-gray-50 p-3 text-sm text-gray-600">
            {quote.notes}
          </div>
        )}
      </div>
    </div>
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
