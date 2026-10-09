import type { MetadataRoute } from 'next';

export default function manifest(): MetadataRoute.Manifest {
  return {
    name: 'StockAlert',
    short_name: 'StockAlert',
    description:
      'Inventory, sales, quotes, customers and operational reporting.',
    start_url: '/',
    display: 'standalone',
    background_color: '#f9fafb',
    theme_color: '#2563eb',
  };
}
