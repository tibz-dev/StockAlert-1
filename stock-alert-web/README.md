# StockAlert Web

The StockAlert web application is the Next.js management dashboard for the StockAlert inventory monitoring and reconciliation platform.

It consumes the ASP.NET Core API in the repository root and is intended to provide authenticated access to inventory, low-stock alerts, dashboard metrics, audit logs, reports, and SmartTrade reconciliation controls.

> **Status:** Active MVP development. The UI is partially implemented and the authentication/routing flow still needs to be corrected before production deployment.

## Stack

- Next.js 16
- React 19
- TypeScript
- Tailwind CSS 4
- Axios
- Lucide React

## Current pages

- `/` — currently contains the login UI and needs to become the authenticated dashboard route.
- `/login` — currently contains the dashboard UI and needs to become the login route.
- `/inventory` — product inventory, low-stock status, and CSV export.
- `/audit` — authenticated audit-log view.
- `/reports` — referenced in the sidebar but not implemented yet.

## API configuration

The Axios client reads:

```env
NEXT_PUBLIC_API_URL=https://localhost:7035/api
```

Create a `.env.local` file if the API is running on a different URL.

## Run locally

```bash
npm install
npm run dev
```

Open `http://localhost:3000`.

## Available scripts

```bash
npm run dev
npm run build
npm run start
npm run lint
```

## Frontend work still required

- correct the login/dashboard routes;
- add an authenticated route guard;
- improve JWT storage/session handling;
- build the reports page;
- add sales screens;
- add product create/edit/delete screens;
- add supplier management;
- connect supplier contact actions to API data;
- replace placeholder integration-health values with live data;
- add loading, empty and error states consistently;
- add responsive/mobile navigation;
- add automated tests.

See the repository-level `README.md` for the full architecture, API endpoints, backend setup, known gaps, and roadmap.
