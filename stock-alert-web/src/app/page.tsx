'use client';

import { useCallback, useEffect, useState } from 'react';
import {
  AlertTriangle,
  CircleOff,
  Loader2,
  Package,
  RefreshCw,
  TrendingUp,
} from 'lucide-react';
import api from '@/lib/api';

interface DashboardStats {
  totalProducts: number;
  totalInventoryValue: number;
  lowStockAlerts: number;
  totalSalesRevenue: number;
  discrepancyCount: number;
  syncLogs: string[];
}

interface StatCardProps {
  title: string;
  value: string | number;
  icon: React.ReactNode;
  accentClass: string;
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
    <div className="mx-auto max-w-7xl p-6 lg:p-8">
      <div className="mb-8 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <p className="text-sm font-medium text-blue-600">Inventory overview</p>
          <h1 className="text-3xl font-bold text-gray-900">Dashboard</h1>
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
          <div className="grid grid-cols-1 gap-6 md:grid-cols-2 xl:grid-cols-4">
            <StatCard
              title="Inventory Value"
              value={stats.totalInventoryValue.toLocaleString(undefined, {
                minimumFractionDigits: 2,
                maximumFractionDigits: 2,
              })}
              icon={<TrendingUp size={22} />}
              accentClass="text-green-600"
            />
            <StatCard
              title="Total Products"
              value={stats.totalProducts}
              icon={<Package size={22} />}
              accentClass="text-blue-600"
            />
            <StatCard
              title="Low Stock"
              value={stats.lowStockAlerts}
              icon={<AlertTriangle size={22} />}
              accentClass="text-red-600"
            />
            <StatCard
              title="Sales Revenue"
              value={stats.totalSalesRevenue.toLocaleString(undefined, {
                minimumFractionDigits: 2,
                maximumFractionDigits: 2,
              })}
              icon={<TrendingUp size={22} />}
              accentClass="text-violet-600"
            />
          </div>

          <div className="mt-8 rounded-xl border border-amber-200 bg-amber-50 p-6">
            <div className="flex items-start gap-3">
              <CircleOff className="mt-0.5 text-amber-700" size={21} />
              <div>
                <h2 className="font-semibold text-amber-950">SmartTrade integration pending</h2>
                <p className="mt-1 text-sm leading-6 text-amber-800">
                  The sync workflow and background worker are in place, but the SmartTrade
                  adapter is still using a placeholder implementation. Live connection status
                  will appear here once the real external API is configured.
                </p>
                {syncMessage && (
                  <p className="mt-3 text-sm font-medium text-amber-900">{syncMessage}</p>
                )}
              </div>
            </div>
          </div>
        </>
      )}
    </div>
  );
}

function StatCard({ title, value, icon, accentClass }: StatCardProps) {
  return (
    <div className="rounded-xl border border-gray-100 bg-white p-6 shadow-sm">
      <div className="flex items-center gap-4">
        <div className={`rounded-lg bg-gray-50 p-3 ${accentClass}`}>{icon}</div>
        <div>
          <p className="text-sm text-gray-500">{title}</p>
          <h2 className="text-2xl font-bold text-gray-900">{value}</h2>
        </div>
      </div>
    </div>
  );
}
