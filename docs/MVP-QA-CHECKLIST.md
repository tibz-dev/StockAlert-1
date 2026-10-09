# StockAlert MVP QA Checklist

Branch: `feature/business-quotes-receipts`

Use this checklist for the current MVP verification pass. Do not add new feature scope until the critical path below is green.

## 1. Build and database

- [ ] Backend `dotnet build` succeeds
- [ ] Frontend `npm run build` succeeds
- [ ] Latest EF Core migration scaffolds successfully
- [ ] Database update succeeds
- [ ] API starts without runtime exceptions
- [ ] Frontend starts and reaches login

## 2. Authentication and roles

- [ ] Owner can sign in
- [ ] JWT contains Owner role after re-login
- [ ] Owner can open Business Settings
- [ ] Owner can open Staff
- [ ] Owner can open Audit Trail
- [ ] Owner can open Reports
- [ ] Non-owner cannot update Business Settings through API
- [ ] Non-owner cannot create/update Staff through API
- [ ] Sales/Stock/Staff cannot view Owner-only reporting configuration
- [ ] Sales/Stock/Staff cannot impersonate another salesperson

## 3. Business settings

- [ ] Save business name
- [ ] Save branch number
- [ ] Save address
- [ ] Save VAT settings
- [ ] Save banking details
- [ ] Save currency
- [ ] Save quote defaults
- [ ] Communication readiness shows configured/setup-required accurately

## 4. Staff

- [ ] Create staff member
- [ ] Edit staff member
- [ ] Deactivate staff member
- [ ] Inactive staff member disappears from active salesperson selector
- [ ] Quote/sale retains salesperson name after staff edit/deactivation

## 5. Inventory and barcode

- [ ] Create product without barcode
- [ ] Create product with barcode
- [ ] Duplicate active barcode is rejected
- [ ] Scan/type known barcode in Sales and product is selected
- [ ] Unknown barcode lookup gives external result when available
- [ ] Unknown lookup can still be captured manually
- [ ] Edit product barcode
- [ ] Adjust stock up
- [ ] Adjust stock down
- [ ] Cannot reduce below accepted-quote reservation
- [ ] Archive product
- [ ] Archived product disappears from active inventory/sales
- [ ] Archived product remains visible in historical sales/audit
- [ ] Archived barcode can be reused on a new active product

## 6. Suppliers

- [ ] Create supplier
- [ ] Edit supplier
- [ ] Search supplier by supplier name
- [ ] Search supplier by email
- [ ] Search supplier by supplied product name
- [ ] View Products shows supplier catalogue
- [ ] Supplier catalogue shows barcode/on-hand/reserved/available
- [ ] Supplier with linked active products cannot be deleted

## 7. Quotes

- [ ] Create multi-product quote
- [ ] Select salesperson as Owner/Manager
- [ ] Normal staff cannot assign another salesperson
- [ ] VAT total calculates correctly
- [ ] Deposit percentage calculates deposit amount correctly
- [ ] 20%, 30%, 50% shortcuts work
- [ ] Invalid deposit percentage is rejected
- [ ] Send quote by configured email
- [ ] Send quote by configured SMS
- [ ] Send quote by configured WhatsApp
- [ ] Unconfigured provider reports setup-required/manual fallback
- [ ] Accept quote
- [ ] Accepted quote reserves stock
- [ ] Record deposit/payment
- [ ] Convert eligible quote to sale
- [ ] Converted sale inherits salesperson

## 8. Direct sales and receipts

- [ ] Record direct sale
- [ ] Sale reduces available stock
- [ ] Cannot oversell reserved stock
- [ ] Sales history shows salesperson
- [ ] Receipt shows business/customer/salesperson details
- [ ] Print/Save receipt as PDF
- [ ] Email receipt
- [ ] SMS receipt
- [ ] WhatsApp receipt
- [ ] Delivery logs show Sent/Failed/PendingProviderConfiguration correctly

## 9. Audit trail

- [ ] Product creation is audited
- [ ] Product edit is audited
- [ ] Product archive is audited with user
- [ ] Stock adjustment is audited
- [ ] Sale is audited as Sold
- [ ] Quote creation is audited
- [ ] Payment is audited
- [ ] User attribution is correct
- [ ] IP address appears where available
- [ ] Filter by date
- [ ] Filter by action
- [ ] Search by user/product/details
- [ ] Pagination works
- [ ] CSV export respects selected filters
- [ ] Print/Save PDF works for selected date range

## 10. Reports and owner protection

- [ ] Reports exclude archived products from operational totals
- [ ] Salesperson performance totals are correct
- [ ] Sales CSV includes salesperson and receipt number
- [ ] Supplier CSV uses active products and reservation-aware low stock
- [ ] Owner report settings are Owner-only
- [ ] Daily schedule can be saved
- [ ] Weekly schedule can be saved
- [ ] Monthly schedule can be saved
- [ ] Yearly schedule can be saved
- [ ] Owner recipient cannot be changed by staff
- [ ] Report worker sends only after provider is configured
- [ ] Missed report period is sent after API comes back online

## 11. Offline / loadshedding

- [ ] Authenticated app registers service worker
- [ ] Products/barcodes/business/staff are cached while online
- [ ] Disconnect internet
- [ ] Sales page still opens from cached app shell
- [ ] Known barcode resolves locally while offline
- [ ] Offline sale is stored as Pending Sync
- [ ] Cached available stock reduces on the device
- [ ] Offline Queue shows pending transaction
- [ ] Reconnect internet
- [ ] Queue syncs automatically
- [ ] Synced operation is not duplicated on retry
- [ ] Stock conflict becomes Conflict instead of silently changing stock
- [ ] Price conflict becomes Conflict
- [ ] VAT/total conflict becomes Conflict
- [ ] Successful sync refreshes cached stock
- [ ] Expired JWT allows only bounded offline grace
- [ ] Reconnection requires normal authentication if session is expired

## 12. Pitch critical path

- [ ] Configure business
- [ ] Add supplier
- [ ] Add product / scan barcode
- [ ] Add staff salesperson
- [ ] Create customer quote
- [ ] Negotiate deposit percentage
- [ ] Send quote
- [ ] Accept quote
- [ ] Stock becomes reserved
- [ ] Record payment
- [ ] Convert quote to sale
- [ ] Generate/send/print receipt
- [ ] Dashboard updates
- [ ] Audit trail shows full activity
- [ ] Owner operational report reflects activity
- [ ] Offline sale queues and syncs successfully

## Release gate

Do not pitch as production-ready until:

- [ ] Backend build green
- [ ] Frontend production build green
- [ ] Database migration green
- [ ] Pitch critical path green
- [ ] Owner/security tests green
- [ ] Offline duplicate/conflict tests green

