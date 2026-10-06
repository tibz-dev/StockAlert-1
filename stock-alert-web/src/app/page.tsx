'use client';

import Link from 'next/link';
import { useCallback, useEffect, useState, type ReactNode } from 'react';
import {
  AlertTriangle,
  ArrowDown,
  ArrowRight,
  ArrowUp,
  Boxes,
  CircleOff,
  Loader2,
  Package,
  ReceiptText,
  RefreshCw,
  ShoppingCart,
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

interface RecentSale {
  id: string;
  productName: string;
  quantity: number;
  totalPrice: number;
  saleDate: string;
}

interface RecentStockMovement {
  id: string;
  productId: string;
  productName: string;
  previousQuantity: number;
  quantityChange: number;
  newQuantity: number;
  reason: string;
  performedBy: string;
  createdAt: string;
}

interface LowStockSupplierAlert {
  supplierId: string;
  supplierName: string;
  supplierEmail: string | null;
  lowStockProductCount: number;
}

interface DashboardStats {
  totalProducts: number;
  totalInventoryValue: number;
  lowStockAlerts: number;
  totalSalesRevenue: number;
  totalUnitsSold: number;
  totalSuppliers: number;
  topSellingProducts: TopSellingProduct[];
  recentSales: RecentSale[];
  recentStockMovements: RecentStockMovement[];
  lowStockSupplierAlerts: LowStockSupplierAlert[];
  discrepancyCount: number;
  syncLogs: string[];
}

interface StatCardProps {
  title: string;
  value: string | number;
  icon: ReactNode;
  accentClass: string;
  href?: string;
}

export default function DashboardPage() {
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [isSyncing, setIsSyncing] = useState(false);
  const [error, setError] = useState('');
  const [syncMessage, setSyncMessage] = useState('');

  const loadData = useCallback(async () => {
    try {
      setError('');
      const response = await api.get<DashboardStats>('/dashboard/summary');
      setStats(response.data);
    } catch {
      setError('Unable to load dashboard data.');
    }
  }, []);

  useEffect(() => {
    void loadData();
  }, [loadData]);

  const triggerSync = async () => {
    try {
      setIsSyncing(true);
      setSyncMessage('');
      const response = await api.post('/products/sync-smarttrade');
      setSyncMessage(response.data.message);
      await loadData();
    } catch {
      setSyncMessage('SmartTrade sync could not be completed.');
    } finally {
      setIsSyncing(false);
    }
  };

  if (!stats && !error) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center text-gray-500">
        <Loader2 className="mr-2 animate-spin" size={20} />
        Loading dashboard...
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 p-6 lg:p-8">
      <div className="mx-auto max-w-7xl">
        <div className="mb-8 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <p className="text-sm font-medium text-blue-600">Operational overview</p>
            <h1 className="text-3xl font-bold text-gray-900">Dashboard</h1>
            <p className="mt-1 text-sm text-gray-500">
              Live inventory, sales, supplier and stock-movement signals.
            </p>
          </div>

          <button
            onClick={triggerSync}
            disabled={isSyncing}
            className="flex items-center justify-center gap-2 rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-blue-700 disabled:cursor-not-allowed disabled:bg-blue-400"
          >
            <RefreshCw className={isSyncing ? 'animate-spin' : ''} size={18} />
            {isSyncing ? 'Syncing...' : 'Sync SmartTrade'}
          </button>
        </div>

        {error && (
          <div className="mb-6 rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700">
            {error}
          </div>
        )}

        {stats && (
          <>
            <div className="grid gap-5 sm:grid-cols-2 xl:grid-cols-3">
              <StatCard
                title="Inventory Value"
                value={formatAmount(stats.totalInventoryValue)}
                icon={<TrendingUp size={22} />}
                accentClass="text-green-600"
                href="/inventory"
              />
              <StatCard
                title="Total Products"
                value={stats.totalProducts}
                icon={<Package size={22} />}
                accentClass="text-blue-600"
                href="/inventory"
              />
              <StatCard
                title="Low Stock"
                value={stats.lowStockAlerts}
                icon={<AlertTriangle size={22} />}
                accentClass="text-red-600"
                href="/inventory"
              />
              <StatCard
                title="Sales Revenue"
                value={formatAmount(stats.totalSalesRevenue)}
                icon={<ReceiptText size={22} />}
                accentClass="text-violet-600"
                href="/sales"
              />
              <StatCard
                title="Units Sold"
                value={stats.totalUnitsSold}
                icon={<ShoppingCart size={22} />}
                accentClass="text-orange-600"
                href="/sales"
              />
              <StatCard
                title="Suppliers"
                value={stats.totalSuppliers}
                icon={<Truck size={22} />}
                accentClass="text-cyan-600"
                href="/suppliers"
              />
            </div>

            <div className="mt-8 grid gap-6 xl:grid-cols-[1.5fr_1fr]">
              <DashboardPanel
                title="Top selling products"
                description="Ranked by total units sold."
                href="/sales"
                linkLabel="View sales"
              >
                {stats.topSellingProducts.length > 0 ? (
                  <div className="overflow-x-auto">
                    <table className="w-full min-w-[620px] text-left">
                      <thead>
                        <tr className="border-b border-gray-200 text-xs uppercase tracking-wide text-gray-400">
                          <th className="pb-3 font-semibold">Product</th>
                          <th className="pb-3 font-semibold">Units sold</th>
                          <th className="pb-3 font-semibold">Revenue</th>
                          <th className="pb-3 font-semibold">Stock</th>
                        </tr>
                      </thead>
                      <tbody>
                        {stats.topSellingProducts.map((product, index) => (
                          <tr
                            key={product.productId}
                            className="border-b border-gray-100 last:border-0"
                          >
                            <td className="py-3 pr-4">
                              <div className="flex items-center gap-3">
                                <span className="flex h-7 w-7 items-center justify-center rounded-full bg-blue-50 text-xs font-bold text-blue-700">
                                  {index + 1}
                                </span>
                                <span className="font-medium text-gray-900">
                                  {product.productName}
                                </span>
                              </div>
                            </td>
                            <td className="py-3 text-sm font-semibold text-gray-700">
                              {product.unitsSold}
                            </td>
                            <td className="py-3 text-sm text-gray-700">
                              {formatAmount(product.revenue)}
                            </td>
                            <td className="py-3">
                              <span
                                className={
                                  product.stockQuantity < 5
                                    ? 'rounded-full bg-red-50 px-2 py-1 text-xs font-bold text-red-700'
                                    : 'rounded-full bg-green-50 px-2 py-1 text-xs font-bold text-green-700'
                                }
                              >
                                {product.stockQuantity}
                              </span>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                ) : (
                  <EmptyState text="Record sales to build your top-selling product ranking." />
                )}
              </DashboardPanel>

              <DashboardPanel
                title="Supplier reorder pressure"
                description="Suppliers with products currently below the stock threshold."
                href="/suppliers"
                linkLabel="Manage suppliers"
              >
                {stats.lowStockSupplierAlerts.length > 0 ? (
                  <div className="space-y-3">
                    {stats.lowStockSupplierAlerts.map((alert) => (
                      <div
                        key={alert.supplierId}
                        className="rounded-lg border border-gray-200 p-4"
                      >
                        <div className="flex items-start justify-between gap-3">
                          <div>
                            <p className="font-semibold text-gray-900">
                              {alert.supplierName}
                            </p>
                            <p className="mt-1 text-xs text-gray-500">
                              {alert.supplierEmail ?? 'No supplier email configured'}
                            </p>
                          </div>
                          <span className="rounded-full bg-red-50 px-2.5 py-1 text-xs font-bold text-red-700">
                            {alert.lowStockProductCount} low
                          </span>
                        </div>

                        {alert.supplierEmail && (
                          <a
                            href={`mailto:${alert.supplierEmail}?subject=${encodeURIComponent(
                              'StockAlert reorder request',
                            )}`}
                            className="mt-3 inline-flex text-xs font-semibold text-blue-600 hover:underline"
                          >
                            Contact supplier
                          </a>
                        )}
                      </div>
                    ))}
                  </div>
                ) : (
                  <EmptyState text="No supplier currently has low-stock products." />
                )}
              </DashboardPanel>
            </div>

            <div className="mt-6 grid gap-6 xl:grid-cols-2">
              <DashboardPanel
                title="Recent sales"
                description="Latest five recorded transactions."
                href="/sales"
                linkLabel="Open sales"
              >
                {stats.recentSales.length > 0 ? (
                  <div className="space-y-1">
                    {stats.recentSales.map((sale) => (
                      <div
                        key={sale.id}
                        className="flex items-center justify-between gap-4 border-b border-gray-100 py-3 last:border-0"
                      >
                        <div className="min-w-0">
                          <p className="truncate font-medium text-gray-900">
                            {sale.productName}
                          </p>
                          <p className="mt-1 text-xs text-gray-500">
                            {formatDate(sale.saleDate)} · {sale.quantity} unit
                            {sale.quantity === 1 ? '' : 's'}
                          </p>
                        </div>
                        <span className="shrink-0 text-sm font-semibold text-gray-900">
                          {formatAmount(sale.totalPrice)}
                        </span>
                      </div>
                    ))}
                  </div>
                ) : (
                  <EmptyState text="No sales have been recorded yet." />
                )}
              </DashboardPanel>

              <DashboardPanel
                title="Recent stock movements"
                description="Latest five manual inventory adjustments."
                href="/stock-movements"
                linkLabel="View history"
              >
                {stats.recentStockMovements.length > 0 ? (
                  <div className="space-y-1">
                    {stats.recentStockMovements.map((movement) => (
                      <div
                        key={movement.id}
                        className="flex items-center justify-between gap-4 border-b border-gray-100 py-3 last:border-0"
                      >
                        <div className="min-w-0">
                          <div className="flex items-center gap-2">
                            <p className="truncate font-medium text-gray-900">
                              {movement.productName}
                            </p>
                            <span
                              className={
                                movement.quantityChange > 0
                                  ? 'inline-flex items-center gap-1 rounded-full bg-green-50 px-2 py-0.5 text-xs font-bold text-green-700'
                                  : 'inline-flex items-center gap-1 rounded-full bg-red-50 px-2 py-0.5 text-xs font-bold text-red-700'
                              }
                            >
                              {movement.quantityChange > 0 ? (
                                <ArrowUp size={11} />
                              ) : (
                                <ArrowDown size={11} />
                              )}
                              {movement.quantityChange > 0 ? '+' : ''}
                              {movement.quantityChange}
                            </span>
                          </div>
                          <p className="mt-1 truncate text-xs text-gray-500">
                            {movement.reason} · {formatDate(movement.createdAt)}
                          </p>
                        </div>
                        <span className="shrink-0 text-xs font-medium text-gray-500">
                          {movement.previousQuantity} → {movement.newQuantity}
                        </span>
                      </div>
                    ))}
                  </div>
                ) : (
                  <EmptyState text="No manual stock adjustments have been recorded yet." />
                )}
              </DashboardPanel>
            </div>

            <div className="mt-6 rounded-xl border border-amber-200 bg-amber-50 p-6">
              <div className="flex items-start gap-3">
                <CircleOff className="mt-0.5 shrink-0 text-amber-700" size={21} />
                <div>
                  <h2 className="font-semibold text-amber-950">
                    SmartTrade integration pending
                  </h2>
                  <p className="mt-1 text-sm leading-6 text-amber-800">
                    Local inventory, sales and movement metrics above are live. The
                    SmartTrade adapter is still a placeholder, so discrepancy counts and
                    external sync history will remain unavailable until the real API is
                    connected.
                  </p>
                  {syncMessage && (
                    <p className="mt-3 text-sm font-medium text-amber-900">
                      {syncMessage}
                    </p>
                  )}
                </div>
              </div>
            </div>
          </>
        )}
      </div>
    </div>
  );
}

function StatCard({
  title,
  value,
  icon,
  accentClass,
  href,
}: StatCardProps) {
  const content = (
    <div className="group rounded-xl border border-gray-200 bg-white p-5 shadow-sm transition hover:border-gray-300 hover:shadow-md">
      <div className="flex items-center justify-between gap-4">
        <div className={`rounded-lg bg-gray-50 p-3 ${accentClass}`}>{icon}</div>
        {href && (
          <ArrowRight
            size={17}
            className="text-gray-300 transition group-hover:translate-x-0.5 group-hover:text-gray-500"
          />
        )}
      </div>
      <p className="mt-4 text-sm text-gray-500">{title}</p>
      <h2 className="mt-1 text-2xl font-bold text-gray-900">{value}</h2>
    </div>
  );

  return href ? <Link href={href}>{content}</Link> : content;
}

function DashboardPanel({
  title,
  description,
  href,
  linkLabel,
  children,
}: {
  title: string;
  description: string;
  href: string;
  linkLabel: string;
  children: ReactNode;
}) {
  return (
    <section className="rounded-xl border border-gray-200 bg-white p-5 shadow-sm">
      <div className="mb-5 flex items-start justify-between gap-4">
        <div>
          <h2 className="font-semibold text-gray-900">{title}</h2>
          <p className="mt-1 text-xs leading-5 text-gray-500">{description}</p>
        </div>
        <Link
          href={href}
          className="flex shrink-0 items-center gap-1 text-xs font-semibold text-blue-600 hover:underline"
        >
          {linkLabel}
          <ArrowRight size={13} />
        </Link>
      </div>
      {children}
    </section>
  );
}

function EmptyState({ text }: { text: string }) {
  return (
    <div className="flex min-h-32 flex-col items-center justify-center rounded-lg border border-dashed border-gray-200 bg-gray-50 p-5 text-center">
      <Boxes size={20} className="mb-2 text-gray-300" />
      <p className="max-w-sm text-sm text-gray-500">{text}</p>
    </div>
  );
}

function formatAmount(value: number) {
  return value.toLocaleString(undefined, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  });
}

function formatDate(value: string) {
  return new Date(value).toLocaleString();
}
