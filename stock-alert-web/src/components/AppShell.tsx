'use client';

import { useEffect, useState } from 'react';
import { usePathname, useRouter } from 'next/navigation';
import { Loader2 } from 'lucide-react';
import Sidebar from '@/components/Sidebar';

function isTokenExpired(token: string) {
  try {
    const payload = token.split('.')[1];
    const normalized = payload.replace(/-/g, '+').replace(/_/g, '/');
    const decoded = JSON.parse(window.atob(normalized));

    if (!decoded.exp) {
      return false;
    }

    return decoded.exp * 1000 <= Date.now();
  } catch {
    return true;
  }
}

export default function AppShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const isPublicRoute = pathname === '/login';
  const [checkingAuth, setCheckingAuth] = useState(!isPublicRoute);

  useEffect(() => {
    if (isPublicRoute) {
      setCheckingAuth(false);
      return;
    }

    const token = window.localStorage.getItem('token');

    if (!token || isTokenExpired(token)) {
      window.localStorage.removeItem('token');
      router.replace('/login');
      return;
    }

    setCheckingAuth(false);
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

  if (isPublicRoute) {
    return <main className="min-h-screen w-full">{children}</main>;
  }

  return (
    <>
      <Sidebar />
      <main className="min-h-screen flex-1 overflow-y-auto bg-gray-50">
        {children}
      </main>
    </>
  );
}
