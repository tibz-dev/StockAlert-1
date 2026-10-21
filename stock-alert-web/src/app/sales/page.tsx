'use client';

import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Barcode,
  Loader2,
  Mail,
  MessageCircle,
  PlusCircle,
  Printer,
  ReceiptText,
  ShoppingCart,
  Smartphone,
  TrendingUp,
  X,
} from 'lucide-react';
import api from '@/lib/api';
import { getSessionInfo } from '@/lib/auth';
import {
  enqueueOfflineSale,
  getOfflineCache,
  getOrCreateDeviceId,
  setOfflineCache,
} from '@/lib/offline';
import type { Product } from '@/types/inventory';
import type { BusinessProfile } from '@/types/business';
import type { Customer, PreparedDelivery } from '@/types/quote';
import type { StaffMember } from '@/types/staff';

interface Sale {
  id: string;
  receiptNumber: string | null;
  productName: string;
  quantity: number;
  totalPrice: number;
  saleDate: string;
  salespersonId: string | null;
  salespersonName: string | null;
}

interface SaleReceipt {
  saleId: string;
  receiptNumber: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
  saleDate: string;
  salespersonId: string | null;
  salespersonName: string | null;
  customer: Customer | null;
}

export default function SalesPage() {
  const session = getSessionInfo();
  const canAssignSalesperson =
    session.roles.includes('Owner') ||
    session.roles.includes('Manager');

  const [sales, setSales] = useState<Sale[]>([]);
  const [products, setProducts] = useState<Product[]>([]);
  const [business, setBusiness] = useState<BusinessProfile | null>(null);
  const [staff, setStaff] = useState<StaffMember[]>([]);
  const [loading, setLoading] = useState(true);
  const [recording, setRecording] = useState(false);
  const [error, setError] = useState('');
  const [successMessage, setSuccessMessage] = useState('');
  const [productId, setProductId] = useState('');
  const [barcode, setBarcode] = useState('');
  const [lookingUpBarcode, setLookingUpBarcode] = useState(false);
  const [quantity, setQuantity] = useState('1');
  const [salespersonId, setSalespersonId] = useState('');

  const [customerName, setCustomerName] = useState('');
  const [customerCompanyName, setCustomerCompanyName] = useState('');
  const [customerEmail, setCustomerEmail] = useState('');
  const [customerPhoneNumber, setCustomerPhoneNumber] = useState('');
  const [customerWhatsAppNumber, setCustomerWhatsAppNumber] = useState('');
  const [customerHasWhatsApp, setCustomerHasWhatsApp] = useState(false);
  const [customerAddress, setCustomerAddress] = useState('');
  const [receiptToSend, setReceiptToSend] = useState<SaleReceipt | null>(null);

  const loadData = useCallback(async () => {
    try {
      setError('');

      const [
        salesResponse,
        productsResponse,
        businessResponse,
        staffResponse,
      ] = await Promise.all([
        api.get<Sale[]>('/sales'),
        api.get<Product[]>('/products'),
        api.get<BusinessProfile>('/business-profile'),
        api.get<StaffMember[]>('/staff', {
          params: { activeOnly: true },
        }),
      ]);

      setSales(salesResponse.data);
      setProducts(productsResponse.data);
      setBusiness(businessResponse.data);
      setStaff(staffResponse.data);

      await Promise.all([
        setOfflineCache('products', productsResponse.data),
        setOfflineCache('business', businessResponse.data),
        setOfflineCache('staff', staffResponse.data),
      ]);

      setProductId((current) => {
        if (
          current &&
          productsResponse.data.some(
            (product) =>
              product.id === current &&
              product.availableQuantity > 0,
          )
        ) {
          return current;
        }

        return (
          productsResponse.data.find(
            (product) => product.availableQuantity > 0,
          )?.id ?? ''
        );
      });
    } catch {
      if (
        typeof navigator !== 'undefined' &&
        !navigator.onLine
      ) {
        const [
          cachedProducts,
          cachedBusiness,
          cachedStaff,
        ] = await Promise.all([
          getOfflineCache<Product[]>('products'),
          getOfflineCache<BusinessProfile>('business'),
          getOfflineCache<StaffMember[]>('staff'),
        ]);

        if (cachedProducts?.value) {
          setProducts(cachedProducts.value);
          setBusiness(cachedBusiness?.value ?? null);
          setStaff(cachedStaff?.value ?? []);
          setSales([]);

          setProductId((current) => {
            if (
              current &&
              cachedProducts.value.some(
                (product) =>
                  product.id === current &&
                  product.availableQuantity > 0,
              )
            ) {
              return current;
            }

            return (
              cachedProducts.value.find(
                (product) =>
                  product.availableQuantity > 0,
              )?.id ?? ''
            );
          });

          setSuccessMessage(
            `Offline catalogue loaded from ${new Date(
              cachedProducts.cachedAt,
            ).toLocaleString()}.`,
          );
        } else {
          setError(
            'No offline product cache is available yet. Connect once and open Sales before relying on offline mode.',
          );
        }
      } else {
        setError('Unable to load sales data.');
      }
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadData();
  }, [loadData]);

  const selectedProduct = products.find(
    (product) => product.id === productId,
  );

  const lookupBarcode = async () => {
    const value = barcode.trim();
    const normalized = value.replace(/\D/g, '');

    if (!normalized) return;

    const localProduct = products.find(
      (product) => product.barcode === normalized,
    );

    if (localProduct) {
      setProductId(localProduct.id);
      setBarcode(normalized);
      setError('');
      setSuccessMessage(
        `Scanned ${localProduct.name} successfully${navigator.onLine ? '' : ' from the offline catalogue'}.`,
      );
      window.setTimeout(() => setSuccessMessage(''), 2200);
      return;
    }

    if (!navigator.onLine) {
      setError(
        'This barcode is not in the cached StockAlert catalogue. Unknown products require internet or must be added when connectivity returns.',
      );
      return;
    }

    try {
      setLookingUpBarcode(true);
      setError('');
      setSuccessMessage('');

      const response = await api.get<{
        found: boolean;
        isLocalProduct: boolean;
        productId: string | null;
        productName: string | null;
      }>(`/barcodes/${normalized}`);

      if (response.data.isLocalProduct && response.data.productId) {
        setProductId(response.data.productId);
        setSuccessMessage(
          `Scanned ${response.data.productName ?? 'product'} successfully.`,
        );
        window.setTimeout(() => setSuccessMessage(''), 2200);
        return;
      }

      if (response.data.found) {
        setError(
          `${response.data.productName ?? 'This barcode'} exists in the external catalogue but has not been added to your inventory yet.`,
        );
      } else {
        setError(
          'Barcode is not in StockAlert yet. Add the product to inventory first.',
        );
      }
    } catch {
      setError('Unable to look up this barcode.');
    } finally {
      setLookingUpBarcode(false);
    }
  };

  const parsedQuantity = Number(quantity);
  const estimatedSubtotal =
    selectedProduct && Number.isInteger(parsedQuantity) && parsedQuantity > 0
      ? selectedProduct.price * parsedQuantity
      : 0;
  const estimatedVat =
    business?.isVatRegistered
      ? estimatedSubtotal * (business.defaultVatRate / 100)
      : 0;
  const estimatedTotal = estimatedSubtotal + estimatedVat;

  const totalRevenue = useMemo(
    () => sales.reduce((sum, sale) => sum + sale.totalPrice, 0),
    [sales],
  );

  const totalUnitsSold = useMemo(
    () => sales.reduce((sum, sale) => sum + sale.quantity, 0),
    [sales],
  );

  const recordSale = async (
    event: React.FormEvent<HTMLFormElement>,
  ) => {
    event.preventDefault();
    setError('');
    setSuccessMessage('');

    if (!selectedProduct) {
      setError('Select a product.');
      return;
    }

    if (!Number.isInteger(parsedQuantity) || parsedQuantity <= 0) {
      setError('Quantity must be a whole number greater than zero.');
      return;
    }

    if (parsedQuantity > selectedProduct.availableQuantity) {
      setError(
        `Only ${selectedProduct.availableQuantity} unreserved unit(s) of ${selectedProduct.name} are available.`,
      );
      return;
    }

    const operationId = crypto.randomUUID();
    const clientCreatedAt = new Date().toISOString();
    const deviceId = getOrCreateDeviceId();

    const baseRequest = {
      productId: selectedProduct.id,
      quantity: parsedQuantity,
      salespersonId: salespersonId || null,
      customerName: customerName.trim() || null,
      customerCompanyName:
        customerCompanyName.trim() || null,
      customerEmail: customerEmail.trim() || null,
      customerPhoneNumber:
        customerPhoneNumber.trim() || null,
      customerWhatsAppNumber:
        customerWhatsAppNumber.trim() || null,
      customerHasWhatsApp,
      customerAddress: customerAddress.trim() || null,
      clientOperationId: operationId,
      deviceId,
      clientCreatedAt,
      offlineUnitPrice: selectedProduct.price,
      offlineExpectedTotal: estimatedTotal,
    };

    const queueSale = async () => {
      const queuedRequest = {
        ...baseRequest,
        wasQueuedOffline: true,
      };

      await enqueueOfflineSale({
        operationId,
        request: queuedRequest,
        productName: selectedProduct.name,
        estimatedTotal,
        queuedAt: clientCreatedAt,
        status: 'pending',
        error: null,
      });

      const updatedProducts = products.map((product) =>
        product.id === selectedProduct.id
          ? {
              ...product,
              stockQuantity: Math.max(
                0,
                product.stockQuantity - parsedQuantity,
              ),
              availableQuantity: Math.max(
                0,
                product.availableQuantity - parsedQuantity,
              ),
              soldQuantity:
                product.soldQuantity + parsedQuantity,
            }
          : product,
      );

      setProducts(updatedProducts);
      await setOfflineCache('products', updatedProducts);
      setQuantity('1');
      setSuccessMessage(
        `Sale queued offline for ${selectedProduct.name}. It will sync automatically when connectivity returns.`,
      );
    };

    if (
      typeof navigator !== 'undefined' &&
      !navigator.onLine
    ) {
      try {
        setRecording(true);
        await queueSale();
      } catch {
        setError('Unable to store the offline sale on this device.');
      } finally {
        setRecording(false);
      }

      return;
    }

    try {
      setRecording(true);

      const response = await api.post<SaleReceipt>('/sales', {
        ...baseRequest,
        wasQueuedOffline: false,
      });

      setQuantity('1');
      setSuccessMessage(
        `Sale recorded. Receipt ${response.data.receiptNumber} created.`,
      );
      setReceiptToSend(response.data);

      await loadData();

      window.setTimeout(() => setSuccessMessage(''), 4000);
    } catch (requestError: unknown) {
      const response =
        typeof requestError === 'object' &&
        requestError !== null &&
        'response' in requestError
          ? (
              requestError as {
                response?: {
                  status?: number;
                  data?: { message?: string };
                };
              }
            ).response
          : undefined;

      if (!response) {
        try {
          await queueSale();
          return;
        } catch {
          setError(
            'Connection was lost and the sale could not be saved to the offline queue.',
          );
          return;
        }
      }

      setError(
        response.data?.message ??
          'Unable to record the sale. Check available unreserved stock and try again.',
      );
    } finally {
      setRecording(false);
    }
  };

  const clearCustomer = () => {
    setCustomerName('');
    setCustomerCompanyName('');
    setCustomerEmail('');
    setCustomerPhoneNumber('');
    setCustomerWhatsAppNumber('');
    setCustomerHasWhatsApp(false);
    setCustomerAddress('');
  };

  if (loading) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center text-gray-500">
        <Loader2 className="mr-2 animate-spin" size={20} />
        Loading sales...
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 p-6 lg:p-8">
      <div className="mx-auto max-w-7xl">
        <div className="mb-8">
          <p className="text-sm font-medium text-blue-600">Transactions</p>
          <h1 className="flex items-center gap-2 text-2xl font-bold text-gray-900">
            <ReceiptText className="text-blue-600" />
            Sales
          </h1>
          <p className="mt-1 text-sm text-gray-500">
            Record sales, protect quote reservations and optionally send the customer a receipt.
          </p>
        </div>

        {error && (
          <div className="mb-5 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
            {error}
          </div>
        )}

        {successMessage && (
          <div className="mb-5 rounded-lg border border-green-200 bg-green-50 p-3 text-sm text-green-700">
            {successMessage}
          </div>
        )}

        <div className="mb-6 grid gap-4 md:grid-cols-3">
          <SummaryCard label="Recorded sales" value={sales.length.toString()} />
          <SummaryCard label="Units sold" value={totalUnitsSold.toString()} />
          <SummaryCard
            label="Total revenue"
            value={totalRevenue.toLocaleString(undefined, {
              minimumFractionDigits: 2,
              maximumFractionDigits: 2,
            })}
            icon={<TrendingUp size={16} />}
          />
        </div>

        <div className="mb-8 grid gap-6 xl:grid-cols-[520px_1fr]">
          <form
            onSubmit={recordSale}
            className="rounded-xl border border-gray-200 bg-white p-6 shadow-sm"
          >
            <div className="mb-5">
              <p className="text-sm font-medium text-blue-600">New transaction</p>
              <h2 className="mt-1 flex items-center gap-2 text-lg font-bold text-gray-900">
                <ShoppingCart size={19} />
                Record Sale
              </h2>
            </div>

            <div className="space-y-5">
              <label className="block">
                <span className="mb-2 block text-sm font-medium text-gray-700">
                  Scan barcode
                </span>
                <div className="relative">
                  <Barcode
                    size={18}
                    className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400"
                  />
                  <input
                    value={barcode}
                    onChange={(event) => setBarcode(event.target.value)}
                    onKeyDown={(event) => {
                      if (event.key === 'Enter') {
                        event.preventDefault();
                        void lookupBarcode();
                      }
                    }}
                    inputMode="numeric"
                    autoComplete="off"
                    placeholder="Focus here and scan, or type the barcode"
                    className="w-full rounded-lg border border-gray-300 py-2.5 pl-10 pr-12 text-sm text-gray-900 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
                  />
                  {lookingUpBarcode && (
                    <Loader2
                      size={17}
                      className="absolute right-3 top-1/2 -translate-y-1/2 animate-spin text-blue-600"
                    />
                  )}
                </div>
                <p className="mt-1 text-xs text-gray-400">
                  USB scanners and phone scanners using keyboard emulation work here.
                </p>
              </label>

              {canAssignSalesperson ? (
                <label className="block">
                  <span className="mb-2 block text-sm font-medium text-gray-700">
                    Salesperson
                  </span>
                  <select
                    value={salespersonId}
                    onChange={(event) => setSalespersonId(event.target.value)}
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
                    Sold by
                  </p>
                  <p className="mt-1 text-sm font-semibold text-gray-900">
                    {session.name ?? session.email ?? 'Signed-in staff member'}
                  </p>
                </div>
              )}

              <label className="block">
                <span className="mb-2 block text-sm font-medium text-gray-700">
                  Product
                </span>
                <select
                  required
                  value={productId}
                  onChange={(event) => setProductId(event.target.value)}
                  className="w-full rounded-lg border border-gray-300 px-3 py-2.5 text-sm text-gray-900 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
                >
                  <option value="">Select a product</option>
                  {products.map((product) => (
                    <option
                      key={product.id}
                      value={product.id}
                      disabled={product.availableQuantity <= 0}
                    >
                      {product.name} — {product.availableQuantity} available
                    </option>
                  ))}
                </select>
              </label>

              <label className="block">
                <span className="mb-2 block text-sm font-medium text-gray-700">
                  Quantity
                </span>
                <input
                  required
                  type="number"
                  min="1"
                  step="1"
                  max={selectedProduct?.availableQuantity}
                  value={quantity}
                  onChange={(event) => setQuantity(event.target.value)}
                  className="w-full rounded-lg border border-gray-300 px-3 py-2.5 text-gray-900 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
                />
              </label>

              {selectedProduct && (
                <div className="rounded-lg bg-gray-50 p-4 text-sm">
                  <StockRow label="On hand" value={selectedProduct.stockQuantity} />
                  <StockRow label="Under quote" value={selectedProduct.quotedQuantity} />
                  <StockRow label="Reserved" value={selectedProduct.reservedQuantity} />
                  <StockRow
                    label="Available to sell"
                    value={selectedProduct.availableQuantity}
                    strong
                  />
                  <div className="mt-3 border-t border-gray-200 pt-3">
                    <div className="flex justify-between text-gray-600">
                      <span>Subtotal</span>
                      <strong className="text-gray-900">
                        {estimatedSubtotal.toFixed(2)}
                      </strong>
                    </div>
                    {business?.isVatRegistered && (
                      <div className="mt-2 flex justify-between text-gray-600">
                        <span>VAT ({business.defaultVatRate}%)</span>
                        <strong className="text-gray-900">
                          {estimatedVat.toFixed(2)}
                        </strong>
                      </div>
                    )}
                    <div className="mt-3 flex justify-between border-t border-gray-200 pt-3 text-gray-700">
                      <span>Sale total</span>
                      <strong className="text-lg text-gray-900">
                        {business?.currencyCode ?? ''} {estimatedTotal.toFixed(2)}
                      </strong>
                    </div>
                  </div>
                </div>
              )}

              <div className="border-t border-gray-200 pt-5">
                <div className="mb-4 flex items-center justify-between gap-3">
                  <div>
                    <h3 className="font-semibold text-gray-900">
                      Customer & receipt
                    </h3>
                    <p className="mt-1 text-xs text-gray-500">
                      Optional. Add contact details if you want to send a receipt.
                    </p>
                  </div>
                  <button
                    type="button"
                    onClick={clearCustomer}
                    className="text-xs font-semibold text-gray-500 hover:text-gray-800"
                  >
                    Clear
                  </button>
                </div>

                <div className="grid gap-3 sm:grid-cols-2">
                  <Input label="Customer name" value={customerName} onChange={setCustomerName} />
                  <Input label="Company" value={customerCompanyName} onChange={setCustomerCompanyName} />
                  <Input label="Email" type="email" value={customerEmail} onChange={setCustomerEmail} />
                  <Input label="Phone" value={customerPhoneNumber} onChange={setCustomerPhoneNumber} />
                  <Input
                    label="WhatsApp number"
                    value={customerWhatsAppNumber}
                    onChange={setCustomerWhatsAppNumber}
                  />
                  <label className="flex items-center gap-3 rounded-lg border border-gray-200 px-3 py-2.5">
                    <input
                      type="checkbox"
                      checked={customerHasWhatsApp}
                      onChange={(event) =>
                        setCustomerHasWhatsApp(event.target.checked)
                      }
                    />
                    <span className="text-sm text-gray-700">Uses WhatsApp</span>
                  </label>
                  <div className="sm:col-span-2">
                    <Input
                      label="Customer address"
                      value={customerAddress}
                      onChange={setCustomerAddress}
                    />
                  </div>
                </div>
              </div>

              <button
                type="submit"
                disabled={recording || !selectedProduct}
                className="flex w-full items-center justify-center gap-2 rounded-lg bg-blue-600 px-4 py-3 text-sm font-semibold text-white transition hover:bg-blue-700 disabled:cursor-not-allowed disabled:bg-blue-400"
              >
                {recording ? (
                  <Loader2 className="animate-spin" size={18} />
                ) : (
                  <PlusCircle size={18} />
                )}
                {recording ? 'Recording...' : 'Record Sale'}
              </button>
            </div>
          </form>

          <div className="space-y-5">
            <div className="rounded-xl border border-blue-100 bg-blue-50 p-6">
              <h2 className="font-semibold text-blue-950">
                Quote-aware availability
              </h2>
              <p className="mt-2 text-sm leading-6 text-blue-800">
                Sent quotes are shown as pipeline demand. Accepted quotes reserve stock,
                so ordinary sales cannot consume units already promised to a customer.
              </p>

              {selectedProduct && selectedProduct.reservedQuantity > 0 && (
                <div className="mt-4 rounded-lg border border-violet-200 bg-violet-50 p-4 text-sm text-violet-800">
                  <strong>{selectedProduct.reservedQuantity}</strong> unit(s) of{' '}
                  {selectedProduct.name} are protected by accepted quotes.
                </div>
              )}
            </div>

            <div className="overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm">
              <div className="border-b border-gray-200 px-5 py-4">
                <h2 className="font-semibold text-gray-900">Sales History</h2>
              </div>

              <div className="max-h-[620px] overflow-auto">
                <table className="w-full min-w-[620px] text-left">
                  <thead className="sticky top-0 border-b border-gray-200 bg-gray-50">
                    <tr>
                      <th className="p-4 text-sm font-semibold text-gray-600">Date</th>
                      <th className="p-4 text-sm font-semibold text-gray-600">Product</th>
                      <th className="p-4 text-sm font-semibold text-gray-600">Salesperson</th>
                      <th className="p-4 text-sm font-semibold text-gray-600">Qty</th>
                      <th className="p-4 text-sm font-semibold text-gray-600">Total</th>
                      <th className="p-4 text-sm font-semibold text-gray-600">Receipt</th>
                    </tr>
                  </thead>
                  <tbody>
                    {sales.map((sale) => (
                      <tr
                        key={sale.id}
                        className="border-b border-gray-100 last:border-0 hover:bg-gray-50"
                      >
                        <td className="p-4 text-sm text-gray-500">
                          {new Date(sale.saleDate).toLocaleString()}
                        </td>
                        <td className="p-4 font-medium text-gray-900">
                          {sale.productName}
                        </td>
                        <td className="p-4 text-sm text-gray-600">
                          {sale.salespersonName ?? 'Not assigned'}
                        </td>
                        <td className="p-4 text-gray-700">{sale.quantity}</td>
                        <td className="p-4 font-semibold text-gray-900">
                          {sale.totalPrice.toFixed(2)}
                        </td>
                        <td className="p-4">
                          <button
                            onClick={() =>
                              window.open(
                                `/receipts/${sale.id}/print`,
                                '_blank',
                                'noopener,noreferrer',
                              )
                            }
                            className="inline-flex items-center gap-1 rounded-md border border-gray-300 bg-white px-2.5 py-1.5 text-xs font-semibold text-gray-700 hover:bg-gray-50"
                          >
                            <Printer size={13} />
                            Print
                          </button>
                        </td>
                      </tr>
                    ))}

                    {sales.length === 0 && (
                      <tr>
                        <td colSpan={6} className="p-10 text-center text-sm text-gray-500">
                          No sales have been recorded yet.
                        </td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        </div>
      </div>

      {receiptToSend && (
        <ReceiptDeliveryModal
          receipt={receiptToSend}
          onClose={() => setReceiptToSend(null)}
        />
      )}
    </div>
  );
}

function ReceiptDeliveryModal({
  receipt,
  onClose,
}: {
  receipt: SaleReceipt;
  onClose: () => void;
}) {
  const [sending, setSending] = useState('');
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  const prepare = async (channel: 'Email' | 'Sms' | 'WhatsApp') => {
    try {
      setSending(channel);
      setError('');
      setSuccess('');

      const response = await api.post<PreparedDelivery>(
        `/sales/${receipt.saleId}/receipt/delivery`,
        { channel },
      );

      if (response.data.status === 'Sent') {
        setSuccess(`${channel} receipt sent automatically.`);
      } else if (response.data.actionUrl) {
        window.location.href = response.data.actionUrl;
        setSuccess(
          response.data.status === 'PendingProviderConfiguration'
            ? `${channel} provider needs setup; manual fallback opened.`
            : `${channel} automatic delivery failed; manual fallback opened.`,
        );
      } else {
        setError(
          response.data.errorMessage ??
            `${channel} delivery failed.`,
        );
      }
    } catch {
      setError(
        `Unable to prepare ${channel} receipt. Check the customer's contact details.`,
      );
    } finally {
      setSending('');
    }
  };

  const customer = receipt.customer;
  const canEmail = Boolean(customer?.email);
  const canSms = Boolean(customer?.phoneNumber);
  const canWhatsApp = Boolean(
    customer?.whatsAppNumber ||
      (customer?.hasWhatsApp && customer?.phoneNumber),
  );

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
      <div className="w-full max-w-lg rounded-2xl bg-white shadow-2xl">
        <div className="flex items-start justify-between border-b border-gray-200 px-6 py-5">
          <div>
            <p className="text-sm font-medium text-green-600">Sale completed</p>
            <h2 className="mt-1 text-xl font-bold text-gray-900">
              Send receipt?
            </h2>
            <p className="mt-1 text-sm text-gray-500">
              Receipt {receipt.receiptNumber} · {receipt.productName} x {receipt.quantity}
              {receipt.salespersonName ? ` · Sold by ${receipt.salespersonName}` : ''}
            </p>
          </div>
          <button
            onClick={onClose}
            className="rounded-lg p-2 text-gray-400 hover:bg-gray-100"
          >
            <X size={20} />
          </button>
        </div>

        <div className="p-6">
          {error && (
            <div className="mb-4 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
              {error}
            </div>
          )}

          {success && (
            <div className="mb-4 rounded-lg border border-green-200 bg-green-50 p-3 text-sm text-green-700">
              {success}
            </div>
          )}

          {!customer ? (
            <div className="rounded-lg bg-gray-50 p-5 text-sm text-gray-600">
              No customer contact details were attached to this sale. The receipt
              has still been created and the sale is complete.
            </div>
          ) : (
            <>
              <div className="mb-5 rounded-lg bg-gray-50 p-4">
                <p className="font-semibold text-gray-900">{customer.fullName}</p>
                <p className="mt-1 text-sm text-gray-500">
                  Choose how you want to share the receipt.
                </p>
              </div>

              <div className="grid gap-3 sm:grid-cols-3">
                <DeliveryButton
                  label="Email"
                  icon={<Mail size={18} />}
                  disabled={!canEmail || Boolean(sending)}
                  loading={sending === 'Email'}
                  onClick={() => void prepare('Email')}
                />
                <DeliveryButton
                  label="SMS"
                  icon={<Smartphone size={18} />}
                  disabled={!canSms || Boolean(sending)}
                  loading={sending === 'Sms'}
                  onClick={() => void prepare('Sms')}
                />
                <DeliveryButton
                  label="WhatsApp"
                  icon={<MessageCircle size={18} />}
                  disabled={!canWhatsApp || Boolean(sending)}
                  loading={sending === 'WhatsApp'}
                  onClick={() => void prepare('WhatsApp')}
                />
              </div>
            </>
          )}

          <div className="mt-5 grid gap-3 sm:grid-cols-2">
            <button
              onClick={() =>
                window.open(
                  `/receipts/${receipt.saleId}/print`,
                  '_blank',
                  'noopener,noreferrer',
                )
              }
              className="inline-flex items-center justify-center gap-2 rounded-lg border border-blue-200 bg-blue-50 px-4 py-2.5 text-sm font-semibold text-blue-700 hover:bg-blue-100"
            >
              <Printer size={16} />
              Print Receipt
            </button>
            <button
              onClick={onClose}
              className="rounded-lg border border-gray-300 px-4 py-2.5 text-sm font-semibold text-gray-700 hover:bg-gray-50"
            >
              Done
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

function DeliveryButton({
  label,
  icon,
  disabled,
  loading,
  onClick,
}: {
  label: string;
  icon: React.ReactNode;
  disabled: boolean;
  loading: boolean;
  onClick: () => void;
}) {
  return (
    <button
      onClick={onClick}
      disabled={disabled}
      className="flex flex-col items-center gap-2 rounded-lg border border-gray-200 p-4 text-sm font-semibold text-gray-700 transition hover:border-blue-300 hover:bg-blue-50 disabled:cursor-not-allowed disabled:opacity-40"
    >
      {loading ? <Loader2 className="animate-spin" size={18} /> : icon}
      {label}
    </button>
  );
}

function Input({
  label,
  value,
  onChange,
  type = 'text',
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  type?: string;
}) {
  return (
    <label className="block">
      <span className="mb-1.5 block text-xs font-medium text-gray-600">
        {label}
      </span>
      <input
        type={type}
        value={value}
        onChange={(event) => onChange(event.target.value)}
        className="w-full rounded-lg border border-gray-300 px-3 py-2.5 text-sm text-gray-900 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
      />
    </label>
  );
}

function StockRow({
  label,
  value,
  strong = false,
}: {
  label: string;
  value: number;
  strong?: boolean;
}) {
  return (
    <div className="mb-2 flex justify-between text-gray-600 last:mb-0">
      <span>{label}</span>
      <span className={strong ? 'font-bold text-gray-900' : 'font-semibold text-gray-800'}>
        {value}
      </span>
    </div>
  );
}

function SummaryCard({
  label,
  value,
  icon,
}: {
  label: string;
  value: string;
  icon?: React.ReactNode;
}) {
  return (
    <div className="rounded-xl border border-gray-200 bg-white p-5 shadow-sm">
      <div className="flex items-center gap-2 text-sm text-gray-500">
        {icon}
        {label}
      </div>
      <p className="mt-1 text-2xl font-bold text-gray-900">{value}</p>
    </div>
  );
}
