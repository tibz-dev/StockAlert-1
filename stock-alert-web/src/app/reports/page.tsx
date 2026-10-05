'use client';

import { useEffect, useState } from 'react';
import {
  AlertTriangle,
  Download,
  FileBarChart,
  Loader2,
  Package,
  TrendingUp,
} from 'lucide-react';
import api from '@/lib/api';

interface DashboardStats {
  totalProducts: number;
  totalInventoryValue: number;
  lowStockAlerts: number;
  totalSalesRevenue: number;
}

export default function ReportsPage() {
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    const loadReportSummary = async () => {
      try {
        const response = await api.get<DashboardStats>('/dashboard/summary');
        setStats(response.data);
      } catch {
        setError('Unable to load report summary.');
      } finally {
        setLoading(false);
      }
    };

    void loadReportSummary();
  }, []);

  const downloadInventoryCsv = async () => {
    try {
      const response = await api.get('/products/report/csv', {
        responseType: 'blob',
      });

      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      link.href = url;
      link.download = `StockReport_${new Date().toISOString().slice(0, 10)}.csv`;
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(url);
    } catch {
      setError('Failed to download the stock report.');
    }
  };

  if (loading) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center text-gray-500">
        <Loader2 className="mr-2 animate-spin" size={20} />
        Loading reports...
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 p-6 lg:p-8">
      <div className="mb-8 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <p className="text-sm font-medium text-blue-600">Reporting</p>
          <h1 className="flex items-center gap-2 text-2xl font-bold text-gray-900">
            <FileBarChart className="text-blue-600" />
            Reports
          </h1>
        </div>

        <button
          onClick={downloadInventoryCsv}
          className="flex items-center justify-center gap-2 rounded-lg bg-green-600 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-green-700"
        >
          <Download size={18} />
          Download inventory CSV
        </button>
      </div>

      {error && (
        <div className="mb-5 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
          {error}
        </div>
      )}

      {stats && (
        <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-4">
          <ReportCard icon={<Package />} label="Products" value={stats.totalProducts} />
          <ReportCard
            icon={<TrendingUp />}
            label="Inventory value"
            value={stats.totalInventoryValue.toFixed(2)}
          />
          <ReportCard
            icon={<AlertTriangle />}
            label="Low-stock products"
            value={stats.lowStockAlerts}
          />
          <ReportCard
            icon={<TrendingUp />}
            label="Sales revenue"
            value={stats.totalSalesRevenue.toFixed(2)}
          />
        </div>
      )}

      <div className="mt-8 rounded-xl border border-gray-200 bg-white p-6 shadow-sm">
        <h2 className="font-semibold text-gray-900">Available exports</h2>
        <p className="mt-2 text-sm leading-6 text-gray-500">
          Inventory CSV export is available now. Sales exports, stock movement reports,
          date filters, supplier reports and PDF reporting are the next reporting features
          to complete.
        </p>
      </div>
    </div>
  );
}

function ReportCard({
  icon,
  label,
  value,
}: {
  icon: React.ReactNode;
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
