'use client';

import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react';
import {
  AlertTriangle,
  BriefcaseBusiness,
  CalendarDays,
  Download,
  FileBarChart,
  Loader2,
  Package,
  ReceiptText,
  RefreshCw,
  TrendingDown,
  TrendingUp,
  Truck,
} from 'lucide-react';
import api from '@/lib/api';

interface TopSellingProduct {
  productId: string;
  productName: string;
  unitsSold: number;
  revenue: number;
  stockQuantity: number;
}

interface SalespersonPerformance {
  salespersonName: string;
  saleCount: number;
  unitsSold: number;
  revenue: number;
}

interface ReportSummary {
  fromDate: string | null;
  toDate: string | null;
  totalProducts: number;
  totalInventoryValue: number;
  lowStockProducts: number;
  totalSuppliers: number;
  saleCount: number;
  unitsSold: number;
  salesRevenue: number;
  stockMovementCount: number;
  unitsAdded: number;
  unitsRemoved: number;
  topSellingProducts: TopSellingProduct[];
  salespeople: SalespersonPerformance[];
}

type QuickRange = 'today' | '7d' | '30d' | '90d' | 'all' | 'custom';

export default function ReportsPage() {
  const [summary, setSummary] = useState<ReportSummary | null>(null);
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');
  const [quickRange, setQuickRange] = useState<QuickRange>('30d');
  const [loading, setLoading] = useState(true);
  const [downloading, setDownloading] = useState('');
  const [error, setError] = useState('');

  const queryParams = useMemo(
    () => ({
      ...(fromDate ? { fromDate } : {}),
      ...(toDate ? { toDate } : {}),
    }),
    [fromDate, toDate],
  );

  const loadSummary = useCallback(async () => {
    try {
      setLoading(true);
      setError('');

      const response = await api.get<ReportSummary>('/reports/summary', {
        params: queryParams,
      });

      setSummary(response.data);
    } catch {
      setError('Unable to load the selected reporting period.');
    } finally {
      setLoading(false);
    }
  }, [queryParams]);

  useEffect(() => {
    applyQuickRange('30d');
  }, []);

  useEffect(() => {
    void loadSummary();
  }, [loadSummary]);

  const applyQuickRange = (range: QuickRange) => {
    setQuickRange(range);

    if (range === 'all') {
      setFromDate('');
      setToDate('');
      return;
    }

    const today = new Date();
    const from = new Date(today);

    if (range === 'today') {
      // Keep today's date.
    } else if (range === '7d') {
      from.setDate(today.getDate() - 6);
    } else if (range === '30d') {
      from.setDate(today.getDate() - 29);
    } else {
      from.setDate(today.getDate() - 89);
    }

    setFromDate(toDateInput(from));
    setToDate(toDateInput(today));
  };

  const downloadCsv = async (
    endpoint: string,
    fileName: string,
    key: string,
    useDateRange = true,
  ) => {
    try {
      setDownloading(key);
      setError('');

      const response = await api.get(endpoint, {
        params: useDateRange ? queryParams : undefined,
        responseType: 'blob',
      });

      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      link.href = url;
      link.download = fileName;
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(url);
    } catch {
      setError('Failed to download the selected report.');
    } finally {
      setDownloading('');
    }
  };

  const setCustomRange = (field: 'from' | 'to', value: string) => {
    setQuickRange('custom');

    if (field === 'from') {
      setFromDate(value);
    } else {
      setToDate(value);
    }
  };

  return (
    <div className="min-h-screen bg-gray-50 p-6 lg:p-8">
      <div className="mx-auto max-w-7xl">
        <div className="mb-8 flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
          <div>
            <p className="text-sm font-medium text-blue-600">Operational reporting</p>
            <h1 className="flex items-center gap-2 text-2xl font-bold text-gray-900">
              <FileBarChart className="text-blue-600" />
              Reports
            </h1>
            <p className="mt-1 text-sm text-gray-500">
              Analyse sales and stock activity, then export the underlying data.
            </p>
          </div>

          <button
            onClick={() => void loadSummary()}
            disabled={loading}
            className="inline-flex items-center justify-center gap-2 rounded-lg border border-gray-300 bg-white px-4 py-2.5 text-sm font-semibold text-gray-700 transition hover:bg-gray-50 disabled:opacity-50"
          >
            <RefreshCw className={loading ? 'animate-spin' : ''} size={17} />
            Refresh
          </button>
        </div>

        <section className="mb-6 rounded-xl border border-gray-200 bg-white p-5 shadow-sm">
          <div className="mb-4 flex items-center gap-2 text-sm font-semibold text-gray-900">
            <CalendarDays size={18} className="text-blue-600" />
            Reporting period
          </div>

          <div className="flex flex-wrap gap-2">
            {([
              ['today', 'Today'],
              ['7d', '7 days'],
              ['30d', '30 days'],
              ['90d', '90 days'],
              ['all', 'All time'],
            ] as const).map(([value, label]) => (
              <button
                key={value}
                onClick={() => applyQuickRange(value)}
                className={
                  quickRange === value
                    ? 'rounded-lg bg-blue-600 px-3 py-2 text-sm font-semibold text-white'
                    : 'rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm font-semibold text-gray-600 hover:bg-gray-50'
                }
              >
                {label}
              </button>
            ))}
          </div>

          <div className="mt-4 grid gap-4 sm:grid-cols-2 lg:max-w-xl">
            <label>
              <span className="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-500">
                From
              </span>
              <input
                type="date"
                value={fromDate}
                max={toDate || undefined}
                onChange={(event) => setCustomRange('from', event.target.value)}
                className="w-full rounded-lg border border-gray-300 px-3 py-2.5 text-sm text-gray-900 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
              />
            </label>

            <label>
              <span className="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-500">
                To
              </span>
              <input
                type="date"
                value={toDate}
                min={fromDate || undefined}
                onChange={(event) => setCustomRange('to', event.target.value)}
                className="w-full rounded-lg border border-gray-300 px-3 py-2.5 text-sm text-gray-900 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
              />
            </label>
          </div>
        </section>

        {error && (
          <div className="mb-6 rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700">
            {error}
          </div>
        )}

        {loading && !summary ? (
          <div className="flex min-h-72 items-center justify-center text-gray-500">
            <Loader2 className="mr-2 animate-spin" size={20} />
            Loading reports...
          </div>
        ) : (
          summary && (
            <>
              <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
                <ReportCard
                  icon={<ReceiptText size={18} />}
                  label="Sales revenue"
                  value={formatAmount(summary.salesRevenue)}
                />
                <ReportCard
                  icon={<TrendingUp size={18} />}
                  label="Units sold"
                  value={summary.unitsSold}
                />
                <ReportCard
                  icon={<TrendingUp size={18} />}
                  label="Stock added"
                  value={summary.unitsAdded}
                />
                <ReportCard
                  icon={<TrendingDown size={18} />}
                  label="Stock removed"
                  value={summary.unitsRemoved}
                />
                <ReportCard
                  icon={<Package size={18} />}
                  label="Current products"
                  value={summary.totalProducts}
                />
                <ReportCard
                  icon={<AlertTriangle size={18} />}
                  label="Current low stock"
                  value={summary.lowStockProducts}
                />
                <ReportCard
                  icon={<Truck size={18} />}
                  label="Suppliers"
                  value={summary.totalSuppliers}
                />
                <ReportCard
                  icon={<TrendingUp size={18} />}
                  label="Inventory value"
                  value={formatAmount(summary.totalInventoryValue)}
                />
              </div>

              <div className="mt-6 grid gap-6 xl:grid-cols-[1.2fr_1fr]">
                <section className="rounded-xl border border-gray-200 bg-white p-5 shadow-sm">
                  <div className="mb-5">
                    <h2 className="font-semibold text-gray-900">
                      Top products in this period
                    </h2>
                    <p className="mt-1 text-xs text-gray-500">
                      Ranked by units sold within the selected dates.
                    </p>
                  </div>

                  {summary.topSellingProducts.length > 0 ? (
                    <div className="overflow-x-auto">
                      <table className="w-full min-w-[580px] text-left">
                        <thead className="border-b border-gray-200 text-xs uppercase tracking-wide text-gray-400">
                          <tr>
                            <th className="pb-3 font-semibold">Product</th>
                            <th className="pb-3 font-semibold">Units</th>
                            <th className="pb-3 font-semibold">Revenue</th>
                            <th className="pb-3 font-semibold">Current stock</th>
                          </tr>
                        </thead>
                        <tbody>
                          {summary.topSellingProducts.map((product) => (
                            <tr
                              key={product.productId}
                              className="border-b border-gray-100 last:border-0"
                            >
                              <td className="py-3 font-medium text-gray-900">
                                {product.productName}
                              </td>
                              <td className="py-3 text-sm font-semibold text-gray-700">
                                {product.unitsSold}
                              </td>
                              <td className="py-3 text-sm text-gray-700">
                                {formatAmount(product.revenue)}
                              </td>
                              <td className="py-3 text-sm text-gray-700">
                                {product.stockQuantity}
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  ) : (
                    <p className="rounded-lg bg-gray-50 p-6 text-center text-sm text-gray-500">
                      No sales were recorded in this period.
                    </p>
                  )}
                </section>

                <section className="rounded-xl border border-gray-200 bg-white p-5 shadow-sm">
                  <h2 className="font-semibold text-gray-900">Period activity</h2>
                  <p className="mt-1 text-xs text-gray-500">
                    Transaction counts for the selected dates.
                  </p>

                  <div className="mt-5 space-y-3">
                    <ActivityRow label="Sales transactions" value={summary.saleCount} />
                    <ActivityRow
                      label="Stock adjustments"
                      value={summary.stockMovementCount}
                    />
                  </div>
                </section>
              </div>

              <section className="mt-6 rounded-xl border border-gray-200 bg-white p-5 shadow-sm">
                <div className="mb-5 flex items-start gap-3">
                  <div className="rounded-lg bg-blue-50 p-2 text-blue-600">
                    <BriefcaseBusiness size={18} />
                  </div>
                  <div>
                    <h2 className="font-semibold text-gray-900">
                      Salesperson performance
                    </h2>
                    <p className="mt-1 text-sm text-gray-500">
                      Performance for the selected reporting period.
                    </p>
                  </div>
                </div>

                {summary.salespeople.length > 0 ? (
                  <div className="overflow-x-auto">
                    <table className="w-full min-w-[640px] text-left">
                      <thead className="border-b border-gray-200 text-xs uppercase tracking-wide text-gray-400">
                        <tr>
                          <th className="pb-3 font-semibold">Salesperson</th>
                          <th className="pb-3 font-semibold">Transactions</th>
                          <th className="pb-3 font-semibold">Units sold</th>
                          <th className="pb-3 font-semibold">Revenue</th>
                        </tr>
                      </thead>
                      <tbody>
                        {summary.salespeople.map((salesperson) => (
                          <tr
                            key={salesperson.salespersonName}
                            className="border-b border-gray-100 last:border-0"
                          >
                            <td className="py-3 font-medium text-gray-900">
                              {salesperson.salespersonName}
                            </td>
                            <td className="py-3 text-sm text-gray-700">
                              {salesperson.saleCount}
                            </td>
                            <td className="py-3 text-sm text-gray-700">
                              {salesperson.unitsSold}
                            </td>
                            <td className="py-3 text-sm font-semibold text-gray-900">
                              {formatAmount(salesperson.revenue)}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                ) : (
                  <div className="rounded-lg bg-gray-50 p-6 text-center text-sm text-gray-500">
                    No salesperson activity was recorded in this period.
                  </div>
                )}
              </section>

              <section className="mt-6 rounded-xl border border-gray-200 bg-white p-5 shadow-sm">
                <div className="mb-5">
                  <h2 className="font-semibold text-gray-900">CSV exports</h2>
                  <p className="mt-1 text-sm text-gray-500">
                    Sales and stock movement exports use the selected reporting period.
                    Inventory and supplier exports are current snapshots.
                  </p>
                </div>

                <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
                  <ExportButton
                    label="Inventory CSV"
                    loading={downloading === 'inventory'}
                    onClick={() =>
                      void downloadCsv(
                        '/products/report/csv',
                        `Inventory_${todayStamp()}.csv`,
                        'inventory',
                        false,
                      )
                    }
                  />
                  <ExportButton
                    label="Sales CSV"
                    loading={downloading === 'sales'}
                    onClick={() =>
                      void downloadCsv(
                        '/reports/sales/csv',
                        `Sales_${todayStamp()}.csv`,
                        'sales',
                      )
                    }
                  />
                  <ExportButton
                    label="Stock Movements CSV"
                    loading={downloading === 'movements'}
                    onClick={() =>
                      void downloadCsv(
                        '/reports/stock-movements/csv',
                        `StockMovements_${todayStamp()}.csv`,
                        'movements',
                      )
                    }
                  />
                  <ExportButton
                    label="Suppliers CSV"
                    loading={downloading === 'suppliers'}
                    onClick={() =>
                      void downloadCsv(
                        '/reports/suppliers/csv',
                        `Suppliers_${todayStamp()}.csv`,
                        'suppliers',
                        false,
                      )
                    }
                  />
                </div>
              </section>
            </>
          )
        )}
      </div>
    </div>
  );
}

function ReportCard({
  icon,
  label,
  value,
}: {
  icon: ReactNode;
  label: string;
  value: string | number;
}) {
  return (
    <div className="rounded-xl border border-gray-200 bg-white p-5 shadow-sm">
      <div className="mb-3 w-fit rounded-lg bg-blue-50 p-2 text-blue-600">{icon}</div>
      <p className="text-sm text-gray-500">{label}</p>
      <p className="mt-1 text-2xl font-bold text-gray-900">{value}</p>
    </div>
  );
}

function ActivityRow({ label, value }: { label: string; value: number }) {
  return (
    <div className="flex items-center justify-between rounded-lg bg-gray-50 px-4 py-3">
      <span className="text-sm text-gray-600">{label}</span>
      <span className="font-bold text-gray-900">{value}</span>
    </div>
  );
}

function ExportButton({
  label,
  loading,
  onClick,
}: {
  label: string;
  loading: boolean;
  onClick: () => void;
}) {
  return (
    <button
      onClick={onClick}
      disabled={loading}
      className="flex items-center justify-center gap-2 rounded-lg border border-gray-300 bg-white px-4 py-3 text-sm font-semibold text-gray-700 transition hover:border-blue-300 hover:bg-blue-50 hover:text-blue-700 disabled:opacity-50"
    >
      {loading ? <Loader2 className="animate-spin" size={17} /> : <Download size={17} />}
      {label}
    </button>
  );
}

function formatAmount(value: number) {
  return value.toLocaleString(undefined, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  });
}

function toDateInput(value: Date) {
  const year = value.getFullYear();
  const month = String(value.getMonth() + 1).padStart(2, '0');
  const day = String(value.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

function todayStamp() {
  return toDateInput(new Date());
}
