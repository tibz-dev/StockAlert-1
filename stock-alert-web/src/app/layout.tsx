import AppShell from '@/components/AppShell';
import './globals.css';

export const metadata = {
  title: 'StockAlert',
  description: 'Inventory monitoring, reconciliation and low-stock management dashboard',
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body className="flex min-h-screen">
        <AppShell>{children}</AppShell>
      </body>
    </html>
  );
}
