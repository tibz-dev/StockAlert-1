# StockAlert

StockAlert is a full-stack inventory monitoring and reconciliation platform for businesses that need a clearer view of stock levels, sales activity, low-stock risks, supplier actions, and changes coming from an external inventory or POS system such as SmartTrade.

The solution combines an ASP.NET Core 8 API, SQL Server, Entity Framework Core, ASP.NET Core Identity, JWT authentication, background stock reconciliation, audit logging, and a Next.js management dashboard.

> **Project status:** MVP / active development. Authentication, inventory, sales history, reporting foundations and audit viewing are implemented. The main remaining work is full inventory CRUD, richer reporting, production hardening and the real SmartTrade integration.

## Core capabilities

StockAlert is designed to provide one operational dashboard where a business can:

- view inventory, categories, suppliers, quantities and product values;
- identify low-stock products;
- contact suppliers for low-stock products when supplier email is available;
- record sales and reduce stock automatically;
- view sales history and total sales revenue;
- export inventory to CSV;
- review audit logs;
- manually trigger external-stock reconciliation;
- run scheduled reconciliation in the background;
- integrate with SmartTrade through an external-service adapter.

## Current feature status

| Feature | Status | Notes |
| --- | --- | --- |
| Product inventory | ✅ Implemented | Inventory, categories, suppliers, pricing and quantities are stored in SQL Server. |
| Product details API | ✅ Implemented | Individual products can be retrieved by ID. |
| Product creation | ✅ API implemented | Product, category and supplier data can be created through the API. |
| Low-stock detection | ✅ Implemented | Products below 5 units are flagged. |
| Supplier reorder action | ✅ Implemented | Supplier name/email now flow to the inventory UI. |
| Sales recording | ✅ Implemented | Sales reduce stock and store transaction value/date. |
| Sales history | ✅ Implemented | Protected API and Next.js sales screen are available. |
| Dashboard summary | ⚠️ Partial | Product count, inventory value, low-stock count and sales revenue work. SmartTrade discrepancy metrics are pending. |
| Reports UI | ✅ Basic | Report summary and inventory CSV download are available. |
| CSV stock report | ✅ Implemented | Product/category/supplier/stock data can be exported. |
| JWT authentication | ✅ MVP | Identity registration, JWT login, protected API routes and frontend route guarding are wired. |
| Audit logging | ⚠️ Partial | Audit entries are generated and viewable, but authenticated-user attribution needs improvement. |
| SmartTrade integration | 🚧 Scaffolded | Adapter and workers exist, but the adapter does not yet call the real SmartTrade API. |
| Automatic reconciliation | 🚧 Scaffolded | Worker runs every 30 minutes and can reconcile once the external adapter is connected. |

## Architecture

```text
StockAlert-1/
├── StockAlert.API/             # ASP.NET Core API, controllers, JWT and Swagger
├── StockAlert.Application/     # DTOs, interfaces and validation
├── StockAlert.Domain/          # Core entities and domain models
├── StockAlert.Infrastructure/  # EF Core, SQL Server, services, migrations and sync worker
├── stock-alert-web/            # Next.js management dashboard
└── StockAlert.sln
```

### Backend

- .NET 8 / ASP.NET Core Web API
- Entity Framework Core 8
- SQL Server / SQL Server Express
- ASP.NET Core Identity
- JWT Bearer authentication
- FluentValidation
- Swagger / OpenAPI
- BackgroundService for scheduled stock reconciliation

### Frontend

- Next.js 16
- React 19
- TypeScript
- Tailwind CSS 4
- Axios
- Lucide React

## Main frontend routes

| Route | Purpose |
| --- | --- |
| `/login` | Sign in |
| `/` | Dashboard |
| `/inventory` | Inventory and low-stock supplier actions |
| `/sales` | Sales history |
| `/reports` | Reporting summary and CSV export |
| `/audit` | Audit logs |

Protected frontend routes require a non-expired JWT in the current MVP. API responses returning HTTP 401 clear the local token and return the user to `/login`.

## Domain model

The current domain includes:

- **Product** — name, description, price, quantity, category, supplier and external-system identifiers.
- **Category** — product grouping.
- **Supplier** — supplier company and contact email.
- **Sale** — product, quantity, date and total sale value.
- **AuditLog** — entity, action, user, timestamp and captured changes.
- **ApplicationUser** — authenticated StockAlert users.
- **SyncHistory** — model intended for integration/synchronisation history.

## API overview

Authentication endpoints are public. Inventory, sales, dashboard, reports and audit endpoints require a JWT.

| Method | Endpoint | Purpose |
| --- | --- | --- |
| POST | `/api/auth/register` | Register a user |
| POST | `/api/auth/login` | Authenticate and receive a JWT |
| GET | `/api/products` | Get inventory |
| GET | `/api/products/{id}` | Get one product |
| POST | `/api/products` | Create a product |
| POST | `/api/products/sync-smarttrade` | Trigger SmartTrade reconciliation |
| GET | `/api/products/report/csv` | Download inventory CSV |
| GET | `/api/dashboard/summary` | Get dashboard summary |
| GET | `/api/sales` | Get sales history |
| POST | `/api/sales` | Record a sale |
| GET | `/api/audit` | Get the latest audit logs |

## Local development

### Prerequisites

- .NET 8 SDK
- SQL Server Express or SQL Server
- Node.js 20+ and npm
- EF Core CLI tools

### Clone

```bash
git clone https://github.com/tibz-dev/StockAlert-1.git
cd StockAlert-1
```

### Database

The current development configuration expects SQL Server Express:

```text
Server=.\SQLEXPRESS;Database=StockAlertDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
```

Apply migrations:

```bash
dotnet restore

dotnet ef database update \
  --project StockAlert.Infrastructure \
  --startup-project StockAlert.API
```

### Run the API

```bash
dotnet run --project StockAlert.API
```

Swagger is enabled in the Development environment.

### Run the frontend

```bash
cd stock-alert-web
npm install
```

Create `.env.local`:

```env
NEXT_PUBLIC_API_URL=https://localhost:7035/api
```

Run:

```bash
npm run dev
```

Open `http://localhost:3000`.

## SmartTrade integration

External inventory integration is isolated behind `IExternalStockService`.

`SmartTradeAdapter` is the integration implementation and `StockSyncWorker` is the scheduled reconciliation process. The worker runs every 30 minutes, compares external and local quantities, updates mismatches and records reconciliation activity.

At present, `SmartTradeAdapter.SyncFromExternalAsync()` is intentionally a placeholder and returns no products. Live SmartTrade connection status is therefore not presented as active in the frontend.

To complete the integration we still need:

- confirmed SmartTrade API documentation;
- authentication/credential handling;
- product/stock endpoints;
- external-to-local mapping;
- retry and timeout rules;
- failed-sync handling;
- sync-history persistence;
- discrepancy metrics and reconciliation reporting.

## Remaining work

Before StockAlert is production-ready, the major items are:

1. Add full product CRUD: edit, delete and product-management forms.
2. Add manual stock adjustments with reasons and audit history.
3. Add supplier-management screens.
4. Add a sale-entry/POS-style screen, not only sales history.
5. Add search, filters, sorting and pagination to inventory/sales/audit.
6. Make low-stock thresholds configurable by product/business.
7. Replace placeholder SmartTrade integration with the real API.
8. Persist real sync history and discrepancy counts.
9. Attribute audit entries to the authenticated user and avoid logging sensitive Identity values.
10. Add richer reports, date filtering and PDF exports.
11. Add role-based access control.
12. Add global exception handling, structured logging and production-safe CORS.
13. Move all production secrets to environment variables/secret storage.
14. Add automated backend/frontend tests and CI/CD.
15. Add production deployment configuration and monitoring.

## Target production flow

```text
SmartTrade / External POS
          │
          ▼
  IExternalStockService
          │
          ▼
   StockSyncWorker
          │
          ▼
 ASP.NET Core API
          │
     ┌────┴────┐
     ▼         ▼
SQL Server   Audit Logs
     │
     ▼
 Next.js Dashboard
     │
     ├── Inventory
     ├── Low-stock alerts
     ├── Sales
     ├── Reports
     └── Reconciliation history
```

## Roadmap

**Phase 1 — MVP stabilisation**
- ✅ authentication and protected routes;
- ✅ correct dashboard/login routing;
- ✅ sales history;
- ✅ supplier data contract;
- ✅ initial reports page;
- 🚧 audit-user attribution and validation cleanup.

**Phase 2 — Inventory operations**
- product CRUD;
- stock adjustments;
- supplier management;
- sale-entry workflow;
- search/filter/pagination;
- configurable low-stock thresholds.

**Phase 3 — SmartTrade**
- real API authentication;
- product/stock sync;
- reconciliation;
- failed-sync handling;
- sync history;
- discrepancy dashboard.

**Phase 4 — Production readiness**
- roles/permissions;
- notifications;
- automated tests;
- CI/CD;
- secure configuration;
- monitoring and deployment.

## Security

The repository still contains development-oriented configuration. Do not reuse the committed JWT key or local database configuration in production. Production secrets must be supplied through environment variables, .NET user secrets during local development, or a managed secret store.

## License

No license has been added yet.

---

Built as an inventory visibility, sales monitoring and reconciliation platform with ASP.NET Core, SQL Server and Next.js.


## Pitch-ready MVP flow

The current MVP is designed to demonstrate one complete small-business workflow:

1. Configure the business profile, branch details, VAT, banking details, currency, quote validity, logo, and receipt footer.
2. Add suppliers and products with physical stock.
3. Create a customer quote with multiple products and an optional required deposit.
4. Share the quote through prepared Email, SMS, or WhatsApp actions.
5. Accept the quote to reserve inventory so ordinary sales cannot consume promised stock.
6. Record deposits and other payments against the quote.
7. Convert an accepted quote to a sale once the required deposit is satisfied.
8. Generate a grouped receipt, share it with the customer, or print/save it as PDF.
9. Track customers, sales, quote pipeline value, outstanding quote balances, reserved stock, low stock, suppliers, and reports from the dashboard.

### MVP document handling

Quotes and receipts have clean print views that use the configured business identity, branch information, address, VAT details, banking details, customer information, and document totals. Browser Print can be used to save these as PDF immediately.

### Communication channels

The MVP prepares and logs customer communication, then opens the appropriate Email, SMS, or WhatsApp action with the message pre-filled. Direct server-side delivery through SMTP, Twilio, or the WhatsApp Business API is intentionally left as the production integration layer so provider credentials are not hard-coded into the application.

### Inventory terminology

- **On Hand**: physical stock currently held by the business.
- **Under Quote**: stock included on active sent or accepted quotes.
- **Reserved**: quantities protected by accepted quotes.
- **Available**: on-hand stock minus accepted-quote reservations.
- **Sold**: cumulative units already recorded as sales.

This distinction prevents accepted quotes from silently competing with ordinary sales for the same units.

## Production next

The MVP intentionally leaves these for the production hardening phase:

- role-based permissions for Admin, Manager, Sales, and Stock users;
- real Email/SMS/WhatsApp Business providers;
- password reset and account administration;
- hosted file/logo storage rather than URL-only branding;
- payment gateway integration and automated reconciliation;
- automated quote expiry reminders and follow-ups;
- deployment secrets, production CORS, structured logging, monitoring, tests, and CI/CD;
- optional SmartTrade/POS integration after confirmed API documentation and credentials.
