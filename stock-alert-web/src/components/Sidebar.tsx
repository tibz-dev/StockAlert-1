'use client';

import Link from 'next/link';
import { usePathname, useRouter } from 'next/navigation';
import {
  BarChart3,
  FileText,
  History,
  ListRestart,
  LayoutDashboard,
  LogOut,
  Package,
  ReceiptText,
  Settings2,
  Truck,
  Users,
} from 'lucide-react';
import { clsx } from 'clsx';

const menuItems = [
  { name: 'Dashboard', href: '/', icon: LayoutDashboard },
  { name: 'Inventory', href: '/inventory', icon: Package },
  { name: 'Stock Movements', href: '/stock-movements', icon: ListRestart },
  { name: 'Suppliers', href: '/suppliers', icon: Truck },
  { name: 'Quotes', href: '/quotes', icon: FileText },
  { name: 'Customers', href: '/customers', icon: Users },
  { name: 'Sales', href: '/sales', icon: ReceiptText },
  { name: 'Reports', href: '/reports', icon: BarChart3 },
  { name: 'Business Settings', href: '/settings/business', icon: Settings2 },
  { name: 'Audit Logs', href: '/audit', icon: History },
];

export default function Sidebar() {
  const pathname = usePathname();
  const router = useRouter();

  const handleLogout = () => {
    window.localStorage.removeItem('token');
    router.replace('/login');
  };

  return (
    <aside className="sticky top-0 flex h-screen w-64 shrink-0 flex-col bg-slate-900 text-white">
      <div className="p-6">
        <h2 className="flex items-center gap-2 text-2xl font-bold text-blue-400">
          StockAlert
          <span className="rounded bg-blue-900 px-2 py-1 text-xs font-semibold text-blue-200">
            MVP
          </span>
        </h2>
        <p className="mt-2 text-xs text-slate-500">Inventory control & reconciliation</p>
      </div>

      <nav className="flex-1 space-y-2 px-4">
        {menuItems.map((item) => {
          const Icon = item.icon;
          const isActive = pathname === item.href;

          return (
            <Link
              key={item.name}
              href={item.href}
              className={clsx(
                'flex items-center gap-3 rounded-lg px-4 py-3 transition-colors',
                isActive
                  ? 'bg-blue-600 text-white'
                  : 'text-slate-400 hover:bg-slate-800 hover:text-white',
              )}
            >
              <Icon size={20} />
              {item.name}
            </Link>
          );
        })}
      </nav>

      <div className="border-t border-slate-800 p-4">
        <button
          onClick={handleLogout}
          className="flex w-full items-center gap-3 rounded-lg px-4 py-3 text-slate-400 transition-colors hover:bg-slate-800 hover:text-red-400"
        >
          <LogOut size={20} />
          Logout
        </button>
      </div>
    </aside>
  );
}
