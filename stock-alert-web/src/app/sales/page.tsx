'use client';

import { useEffect, useMemo, useState } from 'react';
import { Loader2, ReceiptText, TrendingUp } from 'lucide-react';
import api from '@/lib/api';

interface Sale {
  id: string;
  productName: string;
  quantity: number;
  totalPrice: number;
  saleDate: string;
}

export default function SalesPage() {
  const [sales, setSales] = useState<Sale[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    const loadSales = async () => {
      try {
        const response = await api.get<Sale[]>('/sales');
        setSales(response.data);
      } catch {
        setError('Unable to load sales history.');
      } finally {
        setLoading(false);
      }
    };

    void loadSales();
  }, []);

  const totalRevenue = useMemo(
    () => sales.reduce((sum, sale) => sum + sale.totalPrice, 0),
    [sales],
  );

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
          Sales History
        </h1>
      </div>

      {error && (
        <div className="mb-5 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
          {error}
        </div>
      )}

      <div className="mb-6 grid gap-4 sm:grid-cols-2">
        <div className="rounded-xl border border-gray-200 bg-white p-5 shadow-sm">
          <p className="text-sm text-gray-500">Recorded sales</p>
          <p className="mt-1 text-2xl font-bold text-gray-900">{sales.length}</p>
        </div>
        <div className="rounded-xl border border-gray-200 bg-white p-5 shadow-sm">
          <div className="flex items-center gap-2 text-sm text-gray-500">
            <TrendingUp size={16} />
            Total revenue
          </div>
          <p className="mt-1 text-2xl font-bold text-gray-900">
            {totalRevenue.toLocaleString(undefined, {
              minimumFractionDigits: 2,
              maximumFractionDigits: 2,
            })}
          </p>
        </div>
      </div>

      <div className="overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm">
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
                <tr key={sale.id} className="border-b border-gray-100 last:border-0">
                  <td className="p-4 text-sm text-gray-500">
                    {new Date(sale.saleDate).toLocaleString()}
                  </td>
                  <td className="p-4 font-medium text-gray-900">{sale.productName}</td>
                  <td className="p-4 text-gray-700">{sale.quantity}</td>
                  <td className="p-4 font-semibold text-gray-900">
                    {sale.totalPrice.toFixed(2)}
                  </td>
                </tr>
              ))}

              {sales.length === 0 && !error && (
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
