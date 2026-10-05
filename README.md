# StockAlert

StockAlert is a full-stack inventory monitoring and reconciliation platform built for businesses that need a clearer view of stock levels, sales activity, low-stock risks, and changes coming from an external inventory or POS system such as SmartTrade.

The project combines an ASP.NET Core 8 API, SQL Server, Entity Framework Core, JWT authentication, background stock reconciliation, audit logging, and a Next.js management dashboard.

> **Project status:** MVP / active development. Core inventory functionality is in place, while production authentication, SmartTrade integration, reporting, and several frontend flows still need to be completed.

## What StockAlert is designed to solve

Stock discrepancies are easy to miss when product quantities are updated in more than one system. StockAlert is intended to provide one operational dashboard where a business can:

- view current inventory and product values;
- identify low-stock products before they run out;
- record sales and reduce stock automatically;
- reconcile local inventory with SmartTrade or another external stock source;
- keep an audit trail of inventory changes;
- export stock data for reporting;
- monitor stock health from a web dashboard.

## Current feature status

| Feature | Status | Notes |
| --- | --- | --- |
| Product inventory | ✅ Implemented | Products, categories, suppliers, pricing and stock quantities are stored in SQL Server. |
| Low-stock detection | ✅ Implemented | Products below 5 units are flagged as low stock. |
| Product creation | ✅ API implemented | Product, category and supplier records can be created through the API. |
| Sales recording | ✅ Implemented | Recording a sale reduces local stock and stores the sale value. |
| Dashboard summary | ⚠️ Partial | Product count, inventory value, low-stock count and sales revenue work. Some dashboard fields are placeholders. |
| CSV stock report | ✅ Implemented | Inventory can be exported as a CSV file. |
| Audit logging | ⚠️ Partial | Entity changes are logged, but the authenticated user is not yet attached to every audit entry. |
| JWT authentication | ⚠️ Partial | Login/register and JWT generation exist, but the service registration and frontend auth flow still need cleanup. |
| SmartTrade integration | 🚧 Scaffolded | Adapter and background sync worker exist, but the SmartTrade adapter currently returns no external products. |
| Automatic reconciliation | 🚧 Scaffolded | Worker runs every 30 minutes and can reconcile discrepancies once the external adapter is connected. |
| Reports UI | 🚧 Not completed | The sidebar links to a reports page that has not been created yet. |
| Supplier reorder action | 🚧 Partial | Supplier email exists in the domain model, but it is not currently included in the product DTO returned to the frontend. |

## Architecture

The backend uses a layered solution structure:

```text
StockAlert-1/
├── StockAlert.API/             # ASP.NET Core API, controllers, JWT and Swagger
├── StockAlert.Application/     # DTOs, interfaces and validation
├── StockAlert.Domain/          # Core entities and domain models
├── StockAlert.Infrastructure/  # EF Core, SQL Server, services, migrations and sync worker
├── stock-alert-web/            # Next.js web dashboard
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

| Method | Endpoint | Purpose |
| --- | --- | --- |
| POST | `/api/auth/register` | Register a user |
| POST | `/api/auth/login` | Authenticate and receive a JWT |
| GET | `/api/products` | Get inventory |
| POST | `/api/products` | Create a product |
| POST | `/api/products/sync-smarttrade` | Trigger an authenticated SmartTrade sync |
| GET | `/api/products/report/csv` | Download an inventory CSV report |
| GET | `/api/dashboard/summary` | Get dashboard inventory/sales summary |
| GET | `/api/sales` | Get sales history — currently needs a controller fix |
| POST | `/api/sales` | Record a sale |
| GET | `/api/audit` | Get the latest audit logs (JWT required) |

## Local development

### Prerequisites

Install:

- .NET 8 SDK
- SQL Server Express or SQL Server
- Node.js 20+ and npm
- EF Core CLI tools

### 1. Clone the repository

```bash
git clone https://github.com/tibz-dev/StockAlert-1.git
cd StockAlert-1
```

### 2. Configure the backend

The current development connection string expects SQL Server Express:

```text
Server=.\SQLEXPRESS;Database=StockAlertDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
```

For production, move connection strings, JWT keys, and external-service credentials into environment variables or a secure secret store.

### 3. Restore and migrate the database

```bash
dotnet restore

dotnet ef database update \
  --project StockAlert.Infrastructure \
  --startup-project StockAlert.API
```

### 4. Run the API

```bash
dotnet run --project StockAlert.API
```

The frontend currently defaults to:

```text
https://localhost:7035/api
```

Swagger is enabled in the Development environment.

### 5. Run the frontend

```bash
cd stock-alert-web
npm install
```

Create `.env.local`:

```env
NEXT_PUBLIC_API_URL=https://localhost:7035/api
```

Then run:

```bash
npm run dev
```

Open `http://localhost:3000`.

## SmartTrade integration

The integration is intentionally separated behind `IExternalStockService`.

`SmartTradeAdapter` is the infrastructure implementation and `StockSyncWorker` is the scheduled background process. The worker is already designed to run every 30 minutes, compare external quantities with local quantities, update mismatches, and create reconciliation audit entries.

The remaining work is to replace the placeholder adapter with the real SmartTrade API contract, authentication method, endpoints, mapping, retries, error handling, and sync-history persistence.

## Known implementation gaps

The following should be completed before calling StockAlert production-ready:

1. Fix the frontend route structure so `/login` is the login page and `/` is the authenticated dashboard.
2. Add a proper frontend authentication guard and session/token handling.
3. Register and configure ASP.NET Core Identity and `AuthService` consistently in dependency injection.
4. Replace the placeholder SmartTrade adapter with the real integration.
5. Fix `GET /api/sales` so it returns `GetAllSalesAsync()` instead of products.
6. Build the missing `/reports` page.
7. Return supplier contact information to the inventory UI where required.
8. Replace placeholder dashboard discrepancy/sync information with real data.
9. Associate audit entries with the authenticated user instead of the hard-coded `System` value.
10. Add product edit/delete, stock adjustments, pagination, searching and filtering.
11. Add validation, global exception handling and production-safe CORS.
12. Add automated backend/frontend tests and CI.
13. Move secrets out of committed configuration before deployment.
14. Add deployment configuration for the API, database and Next.js frontend.

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

The immediate goal is to turn the current MVP into a complete business-ready inventory monitoring system.

**Phase 1 — Stabilise the MVP**
- fix authentication and frontend routing;
- fix sales history;
- complete product/supplier data contracts;
- complete dashboard data;
- add the reports page.

**Phase 2 — Complete inventory operations**
- product CRUD;
- manual stock adjustments;
- supplier management;
- sales history;
- filters and search;
- configurable low-stock thresholds.

**Phase 3 — SmartTrade integration**
- real API authentication;
- stock import and reconciliation;
- scheduled sync;
- failed-sync handling;
- sync history and discrepancy dashboard.

**Phase 4 — Production readiness**
- role-based access;
- notifications;
- automated tests;
- CI/CD;
- secure configuration;
- monitoring and deployment.

## Security note

The repository currently contains development-oriented configuration. Do not reuse the committed JWT key or local database configuration in production. Production secrets must be supplied through environment variables, user secrets, or a managed secret store.

## License

No license has been added yet.

---

Built as an inventory visibility and reconciliation platform with ASP.NET Core, SQL Server and Next.js.
