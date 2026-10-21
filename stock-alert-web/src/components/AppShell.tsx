'use client';

import { useEffect, useState } from 'react';
import { usePathname, useRouter } from 'next/navigation';
import { Loader2 } from 'lucide-react';
import Sidebar from '@/components/Sidebar';
import OfflineSyncManager from '@/components/OfflineSyncManager';

function getTokenExpiry(token: string): number | null {
  try {
    const payload = token.split('.')[1];
    const normalized = payload
      .replace(/-/g, '+')
      .replace(/_/g, '/');
    const decoded = JSON.parse(window.atob(normalized));

    return decoded.exp ? decoded.exp * 1000 : null;
  } catch {
    return null;
  }
}

export default function AppShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const isPublicRoute = pathname === '/login';
  const isPrintRoute = pathname.endsWith('/print');
  const [checkingAuth, setCheckingAuth] = useState(!isPublicRoute);
  const [offlineGrace, setOfflineGrace] = useState(false);

  useEffect(() => {
    if (isPublicRoute) {
      setCheckingAuth(false);
      return;
    }

    const token = window.localStorage.getItem('token');

    if (!token) {
      router.replace('/login');
      return;
    }

    const expiry = getTokenExpiry(token);
    const expired = expiry !== null && expiry <= Date.now();

    if (expired) {
      const offlineGraceUntil =
        (expiry ?? 0) + 24 * 60 * 60 * 1000;

      if (
        !navigator.onLine &&
        Date.now() <= offlineGraceUntil
      ) {
        setOfflineGrace(true);
        setCheckingAuth(false);
      } else {
        window.localStorage.removeItem('token');
        router.replace('/login');
        return;
      }
    } else {
      setOfflineGrace(false);
      setCheckingAuth(false);
    }

    if ('serviceWorker' in navigator) {
      void navigator.serviceWorker.register('/sw.js');
    }
  }, [isPublicRoute, pathname, router]);

  if (checkingAuth) {
    return (
      <div className="flex min-h-screen w-full items-center justify-center bg-gray-50 text-gray-600">
        <div className="flex items-center gap-2">
          <Loader2 className="animate-spin" size={20} />
          <span>Checking session...</span>
        </div>
      </div>
    );
  }

  if (isPublicRoute || isPrintRoute) {
    return <main className="min-h-screen w-full">{children}</main>;
  }

  return (
    <>
      <Sidebar />
      <main className="min-h-screen flex-1 overflow-y-auto bg-gray-50">
        {offlineGrace && (
          <div className="border-b border-orange-200 bg-orange-50 px-4 py-2 text-xs font-semibold text-orange-800">
            Offline session grace is active. Reconnect within 24 hours of token expiry to re-authenticate and sync queued work.
          </div>
        )}
        <OfflineSyncManager />
        {children}
      </main>
    </>
  );
}
