'use client';

import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Loader2,
  PlusCircle,
  ReceiptText,
  ShoppingCart,
  TrendingUp,
} from 'lucide-react';
import api from '@/lib/api';
import type { Product } from '@/types/inventory';

interface Sale {
  id: string;
  productName: string;
  quantity: number;
  totalPrice: number;
  saleDate: string;
}

export default function SalesPage() {
  const [sales, setSales] = useState<Sale[]>([]);
  const [products, setProducts] = useState<Product[]>([]);
  const [loading, setLoading] = useState(true);
  const [recording, setRecording] = useState(false);
  const [error, setError] = useState('');
  const [successMessage, setSuccessMessage] = useState('');
  const [productId, setProductId] = useState('');
  const [quantity, setQuantity] = useState('1');

  const loadData = useCallback(async () => {
    try {
      setError('');

      const [salesResponse, productsResponse] = await Promise.all([
        api.get<Sale[]>('/sales'),
        api.get<Product[]>('/products'),
      ]);

      setSales(salesResponse.data);
      setProducts(productsResponse.data);

      if (!productId) {
        const firstAvailable = productsResponse.data.find(
          (product) => product.stockQuantity > 0,
        );

        if (firstAvailable) {
          setProductId(firstAvailable.id);
        }
      }
    } catch {
      setError('Unable to load sales data.');
    } finally {
      setLoading(false);
    }
  }, [productId]);

  useEffect(() => {
    void loadData();
  }, [loadData]);

  const selectedProduct = products.find(
    (product) => product.id === productId,
  );

  const parsedQuantity = Number(quantity);
  const estimatedTotal =
    selectedProduct && Number.isInteger(parsedQuantity) && parsedQuantity > 0
      ? selectedProduct.price * parsedQuantity
      : 0;

  const totalRevenue = useMemo(
    () => sales.reduce((sum, sale) => sum + sale.totalPrice, 0),
    [sales],
  );

  const totalUnitsSold = useMemo(
    () => sales.reduce((sum, sale) => sum + sale.quantity, 0),
    [sales],
  );

  const recordSale = async (event: React.FormEvent<HTMLFormElement>) => {
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

    if (parsedQuantity > selectedProduct.stockQuantity) {
      setError(
        `Only ${selectedProduct.stockQuantity} unit(s) of ${selectedProduct.name} are available.`,
      );
      return;
    }

    try {
      setRecording(true);

      await api.post('/sales', {
        productId: selectedProduct.id,
        quantity: parsedQuantity,
      });

      setQuantity('1');
      setSuccessMessage('Sale recorded and inventory updated.');
      await loadData();

      window.setTimeout(() => setSuccessMessage(''), 3000);
    } catch {
      setError('Unable to record the sale. Check available stock and try again.');
    } finally {
      setRecording(false);
    }
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
      <div className="mb-8">
        <p className="text-sm font-medium text-blue-600">Transactions</p>
        <h1 className="flex items-center gap-2 text-2xl font-bold text-gray-900">
          <ReceiptText className="text-blue-600" />
          Sales
        </h1>
        <p className="mt-1 text-sm text-gray-500">
          Record sales and keep inventory quantities in sync automatically.
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

      <div className="mb-8 grid gap-6 xl:grid-cols-[420px_1fr]">
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
                    disabled={product.stockQuantity <= 0}
                  >
                    {product.name} — {product.stockQuantity} in stock
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
                max={selectedProduct?.stockQuantity}
                value={quantity}
                onChange={(event) => setQuantity(event.target.value)}
                className="w-full rounded-lg border border-gray-300 px-3 py-2.5 text-gray-900 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
              />
            </label>

            {selectedProduct && (
              <div className="rounded-lg bg-gray-50 p-4 text-sm">
                <div className="flex justify-between text-gray-600">
                  <span>Available stock</span>
                  <strong className="text-gray-900">
                    {selectedProduct.stockQuantity}
                  </strong>
                </div>
                <div className="mt-2 flex justify-between text-gray-600">
                  <span>Unit price</span>
                  <strong className="text-gray-900">
                    {selectedProduct.price.toFixed(2)}
                  </strong>
                </div>
                <div className="mt-3 flex justify-between border-t border-gray-200 pt-3 text-gray-700">
                  <span>Sale total</span>
                  <strong className="text-lg text-gray-900">
                    {estimatedTotal.toFixed(2)}
                  </strong>
                </div>
              </div>
            )}

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

        <div className="rounded-xl border border-blue-100 bg-blue-50 p-6">
          <h2 className="font-semibold text-blue-950">How sales affect inventory</h2>
          <p className="mt-2 text-sm leading-6 text-blue-800">
            Recording a sale immediately reduces the selected product&apos;s stock by the
            sold quantity. The backend rejects transactions that exceed available stock,
            so inventory cannot become negative through the sales workflow.
          </p>

          {selectedProduct?.isLowStock && (
            <div className="mt-4 rounded-lg border border-amber-200 bg-amber-50 p-4 text-sm text-amber-800">
              {selectedProduct.name} is already low on stock. Available quantity:{' '}
              <strong>{selectedProduct.stockQuantity}</strong>.
            </div>
          )}
        </div>
      </div>

      <div className="overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm">
        <div className="border-b border-gray-200 px-5 py-4">
          <h2 className="font-semibold text-gray-900">Sales History</h2>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full min-w-[720px] text-left">
            <thead className="border-b border-gray-200 bg-gray-50">
              <tr>
                <th className="p-4 text-sm font-semibold text-gray-600">Date</th>
                <th className="p-4 text-sm font-semibold text-gray-600">Product</th>
                <th className="p-4 text-sm font-semibold text-gray-600">Quantity</th>
                <th className="p-4 text-sm font-semibold text-gray-600">Total</th>
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
                  <td className="p-4 text-gray-700">{sale.quantity}</td>
                  <td className="p-4 font-semibold text-gray-900">
                    {sale.totalPrice.toFixed(2)}
                  </td>
                </tr>
              ))}

              {sales.length === 0 && (
                <tr>
                  <td colSpan={4} className="p-10 text-center text-sm text-gray-500">
                    No sales have been recorded yet.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
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
