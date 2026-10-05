# StockAlert Web

The StockAlert web application is the Next.js management dashboard for the StockAlert inventory monitoring and reconciliation platform.

It consumes the ASP.NET Core API in the repository root and provides authenticated access to dashboard metrics, inventory, low-stock supplier actions, sales history, reports and audit logs.

> **Status:** Active MVP development. Core routing and JWT route protection are in place. Product management, sale entry, richer reporting and SmartTrade integration remain under development.

## Stack

- Next.js 16
- React 19
- TypeScript
- Tailwind CSS 4
- Axios
- Lucide React

## Routes

| Route | Purpose |
| --- | --- |
| `/login` | User sign in |
| `/` | Inventory dashboard |
| `/inventory` | Inventory and supplier reorder actions |
| `/sales` | Sales history |
| `/reports` | Summary reporting and inventory CSV export |
| `/audit` | Audit history |

The application shell checks the JWT before showing protected pages. API 401 responses clear the token and redirect back to login.

## API configuration

Create `.env.local`:

```env
NEXT_PUBLIC_API_URL=https://localhost:7035/api
```

## Run locally

```bash
npm install
npm run dev
```

Open `http://localhost:3000`.

## Scripts

```bash
npm run dev
npm run build
npm run start
npm run lint
```

## Frontend work still required

- product create/edit/delete UI;
- supplier management;
- sale-entry workflow;
- inventory/sales/audit searching, filtering and pagination;
- configurable low-stock thresholds;
- richer reports and PDF export;
- real SmartTrade health/sync data;
- responsive mobile navigation;
- stronger production session strategy;
- automated frontend tests.

See the repository-level `README.md` for backend setup, API endpoints, architecture and the full roadmap.
