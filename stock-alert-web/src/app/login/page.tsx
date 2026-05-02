'use client';

import { useEffect, useState } from 'react';
import api from '@/lib/api';
import { Package, AlertTriangle, TrendingUp, RefreshCw, Loader2 } from 'lucide-react';

export default function Dashboard() {
  const [stats, setStats] = useState<any>(null);
  const [isSyncing, setIsSyncing] = useState(false);

  const loadData = async () => {
    const res = await api.get('/dashboard/summary');
    setStats(res.data);
  };

  useEffect(() => { loadData(); }, []);

  const triggerSync = async () => {
    setIsSyncing(true);
    await api.post('/products/sync-smarttrade');
    await loadData();
    setIsSyncing(false);
  };

  if (!stats) return <div className="p-20 text-center"><Loader2 className="animate-spin inline" /> Loading...</div>;

  return (
    <div className="p-8 max-w-7xl mx-auto">
      <div className="flex justify-between items-center mb-8">
        <h1 className="text-3xl font-bold">Dashboard</h1>
        <button 
          onClick={triggerSync}
          disabled={isSyncing}
          className="bg-blue-600 text-white px-4 py-2 rounded-lg flex gap-2 items-center hover:bg-blue-700 disabled:bg-blue-400"
        >
          <RefreshCw className={isSyncing ? 'animate-spin' : ''} size={18} />
          {isSyncing ? 'Syncing...' : 'Sync SmartTrade'}
        </button>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <StatCard title="Inventory Value" value={`$${stats.totalInventoryValue}`} icon={<TrendingUp />} color="text-green-600" />
        <StatCard title="Total Items" value={stats.totalProducts} icon={<Package />} color="text-blue-600" />
        <StatCard title="Low Stock" value={stats.lowStockAlerts} icon={<AlertTriangle />} color="text-red-600" />
      </div>

      <div className="mt-8 bg-white p-6 rounded-xl shadow-sm border border-gray-200">
        <h3 className="font-bold mb-4 flex items-center gap-2">
          <div className="w-2 h-2 bg-green-500 rounded-full animate-pulse" />
          Integration Health
        </h3>
        <div className="text-sm text-gray-600 space-y-2">
          <div className="flex justify-between border-b pb-2"><span>Source</span><span className="font-bold">SmartTrade API</span></div>
          <div className="flex justify-between"><span>Last Sync</span><span className="font-medium">Success (Just now)</span></div>
        </div>
      </div>
    </div>
  );
}

function StatCard({ title, value, icon, color }: any) {
  return (
    <div className="bg-white p-6 rounded-xl shadow-md border border-gray-100">
      <div className="flex items-center gap-4">
        <div className={`p-3 rounded-lg bg-gray-50 ${color}`}>{icon}</div>
        <div>
          <p className="text-gray-500 text-sm">{title}</p>
          <h2 className="text-2xl font-bold">{value}</h2>
        </div>
      </div>
    </div>
  );
}