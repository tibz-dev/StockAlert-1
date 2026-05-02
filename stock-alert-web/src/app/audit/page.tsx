'use client';

import { useEffect, useState } from 'react';
import api from '@/lib/api';
import { History, User, Activity, Clock, Search } from 'lucide-react';

export default function AuditPage() {
  const [logs, setLogs] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchLogs = async () => {
      try {
        const res = await api.get('/audit');
        setLogs(res.data);
      } catch (err) {
        console.error("Failed to fetch logs", err);
      } finally {
        setLoading(false);
      }
    };
    fetchLogs();
  }, []);

  return (
    <div className="p-8 bg-gray-50 min-h-screen">
      <div className="flex justify-between items-center mb-8">
        <h1 className="text-2xl font-bold flex items-center gap-2">
          <History className="text-blue-600" /> Security Audit Logs
        </h1>
      </div>

      <div className="bg-white rounded-xl shadow-sm border border-gray-200">
        <div className="overflow-x-auto">
          <table className="w-full text-left">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="p-4 text-sm font-semibold text-gray-600">Timestamp</th>
                <th className="p-4 text-sm font-semibold text-gray-600">User</th>
                <th className="p-4 text-sm font-semibold text-gray-600">Entity</th>
                <th className="p-4 text-sm font-semibold text-gray-600">Action</th>
                <th className="p-4 text-sm font-semibold text-gray-600">Details</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {logs.map((log) => (
                <tr key={log.id} className="hover:bg-gray-50 transition">
                  <td className="p-4 text-sm text-gray-500">
                    {new Date(log.timestamp).toLocaleString()}
                  </td>
                  <td className="p-4">
                    <div className="flex items-center gap-2">
                      <div className="p-1.5 bg-slate-100 rounded-full"><User size={14}/></div>
                      <span className="text-sm font-medium">{log.userId}</span>
                    </div>
                  </td>
                  <td className="p-4 text-sm font-semibold">{log.entityName}</td>
                  <td className="p-4">
                    <span className={`px-2 py-1 rounded-full text-xs font-bold ${
                      log.action === 'Modified' ? 'bg-amber-50 text-amber-700' : 
                      log.action === 'Added' ? 'bg-green-50 text-green-700' : 'bg-red-50 text-red-700'
                    }`}>
                      {log.action}
                    </span>
                  </td>
                  <td className="p-4 text-sm text-gray-600 max-w-xs truncate" title={log.changes}>
                    {log.changes}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}