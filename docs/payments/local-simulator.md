# Local payment testing without merchant credentials

This lab runs Amaara's real checkout, Identity, SQL persistence, inventory, payment verification, retry, refund and reconciliation services with the existing fake gateway. No company registration, merchant account, deployment, real customer or card is needed. It does **not** contact Zahls or Datatrans or prove that either provider will approve a transaction.

## Start on Windows

Prerequisites: .NET 8 SDK, Node.js 22+, Chrome, and a running local SQL Server accepting your Windows account. Your account needs permission to create a disposable database. The repository's portable Node 22 is used automatically when present. LocalDB also works with `-SqlServer '(localdb)\MSSQLLocalDB'`. No SQL password is used.

Open PowerShell:

```powershell
cd C:\Nexaura
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\start-payment-simulator.ps1 -Currency LKR
```

The script builds a separate, nonpublishable development host and opens **http://127.0.0.1:5208/simulator**. Allow the first build/database creation to finish. Use `127.0.0.1` consistently, because the security guard rejects other Host values. The separate shop uses port **5188** and test admin uses **5189**. Existing ports 5173/5179 and the business API are untouched. If any lab port is already occupied, startup refuses to take it over.

Every start creates a **new**, migrated, uniquely named database:

```text
AmaaraPhase6Tests_LocalPayments_LKR_<random identifier>
```

The exact name appears in PowerShell, the lab header, and `.runtime\local-payments\session.json`. Startup accepts a local server name, never an existing database/connection string. `AmaaraCreationsDB` is neither migrated nor used. All products, prices, accounts and addresses in the lab are fictional fixtures. Do not enter personal information or actual credentials anywhere in this shop.

## First successful payment

1. On the lab page select **Open shop as test customer**. This signs in a generated synthetic customer through the normal backend. No password is needed.
2. Open **SIMULATED LKR sticker 1**. This sample has no variants; the other fixtures retain variants. Add one item to the cart.
3. Go to Cart → Checkout. Keep the supplied fictional delivery address and choose **SIMULATED delivery**.
4. Review the server total: one sticker **LKR 101.00**, delivery **LKR 25.00**, total **LKR 126.00**. Select **Pay securely**.
5. The development redirect returns you to the local lab. Confirm the selected attempt is **Pending**, then select **Simulate success**.
6. The lab should show payment **Paid** and order **Confirmed**. Select **Return to storefront payment status**, then view your orders.
7. Return to the lab to inspect the SQL-backed payment table and expanded inventory/reservations/attempts/refunds/events. **Replay last event** should not charge again, duplicate the event, or remove more inventory.

The browser sends an outcome and identifier, not a trusted amount or a Paid update. The server loads the amount, currency and references from SQL, signs a simulated event with a fresh private in-memory key, verifies it through `PaymentWebhookService`, and applies the normal business workflow. The reserved fake hosted URL is intercepted only by the development Vite module; no external payment page is visited.

## Decline, cancellation, pending and retry

Start a new checkout after completing the first order. Choose **Simulate decline**, then return to the storefront payment status page: it should show **Payment failed**. Choose **Retry secure payment** there. It creates a new payment and checkout attempt for the **same order**, rather than creating a duplicate order.

Choose **Simulate cancellation** on that attempt. Return to its status page and retry again. Use **Keep payment pending** to verify that Pending never means Paid. The lab must show the current attempt; select it in the payment dropdown when inspecting earlier attempts.

For missed-webhook recovery, select **Provider state only — reconcile separately** under Event delivery, then **Simulate success**. SQL remains Pending. Select **Reconcile selected payment** to run the normal protected reconciliation endpoint; it reads the fake provider state and verifies the payment before updating SQL.

Stock starts at 100 per sample. The existing workflow subtracts available stock when it reserves inventory, restores it on decline/cancellation, and marks the reservation Consumed on success without subtracting stock twice. Payment success clears the purchased cart items. Failed/cancelled payments retain the cart.

## Refunds

On the lab select a paid attempt. Enter **20.00**, keep refund outcome **Completed**, and select **Request simulated refund**. The payment becomes **PartiallyRefunded** and refunded amount becomes 20.00 in the same currency. **Replay last refund request** returns that same refund; it does not create another credit.

Submit another refund with outcome **Pending**, then choose **Complete selected payment's pending refund**. Completion passes through the signed-event workflow. Outcome **Failed** exercises a rejected credit. An amount above the remaining settled amount is rejected. Refunding the exact remaining amount changes the payment to **Refunded**. Financial refunds deliberately do not restock physical goods; that is the existing business behavior.

Refund/reconciliation controls sign in the fixture administrator through normal Identity/MFA and use the existing protected APIs. **Open test admin** also opens the real administration UI with a synthetic identity. Repeated admin sign-ins within one authenticator period can require waiting up to 30 seconds; no actual authenticator or real administrator account is needed for this lab.

## Verify directly in SQL Server

From a second PowerShell window:

```powershell
cd C:\Nexaura
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\inspect-payment-simulator.ps1
```

This read-only script connects with Windows authentication, checks the database name and database-level `AmaaraLocalPaymentSimulator` safety marker, then prints Orders, Payments, CheckoutAttempts, InventoryReservations, Refunds, PaymentWebhookEvents and Products. It never queries the business database or account credential tables.

Alternatively connect SQL Server Management Studio to `localhost` using Windows Authentication and choose the **exact printed disposable database**. Run:

```sql
SELECT Id, OrderNumber, Status, Total, Currency FROM Orders;
SELECT Id, OrderId, Provider, ProviderMode, Status, Amount, Currency,
       SettledAmount, RefundedAmount FROM Payments;
SELECT Id, OrderId, PaymentId, Provider FROM CheckoutAttempts;
SELECT OrderId, ProductId, Quantity, Status FROM InventoryReservations;
SELECT Id, PaymentId, Amount, Currency, Status, IdempotencyKey FROM Refunds;
SELECT PaymentId, ProviderStatus, ProcessedAt FROM PaymentWebhookEvents;
SELECT Id, Name, Stock, Currency FROM Products;
```

Payments must say **LocalSimulator / SIMULATED**. Payment Status values are Pending=0, Authorized=1, Paid=2, Failed=3, Refunded=4, Cancelled=5 and PartiallyRefunded=6. The relevant order statuses are PendingPayment=5 and Confirmed=6; the lab displays their names. Compare attempt count, order IDs, amounts, inventory and refund amounts before and after replay. SQL is the source of these inspection results.

## Stop, use CHF, and retain results

```powershell
cd C:\Nexaura
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\stop-payment-simulator.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\start-payment-simulator.ps1 -Currency CHF
```

Stopping terminates only the recorded, command-verified lab processes. SQL records remain for inspection. Restarting creates another fresh catalog; it does not resume the previous fake provider ledger. The CHF catalog uses separately seeded fictional CHF values (101 + 25 = 126 CHF). **No exchange rate or automatic conversion occurs**; earlier LKR and historical provider records are unchanged.

When finished, stop the lab. You may manually delete only the disposable catalog(s) in SSMS after checking the exact name and safety marker; no script automatically deletes retained manual records. The private `.runtime` directory contains local process metadata and generated Data Protection keys and is ignored by Git.

## Automated verification commands

Stop a manually running lab before the browser runner, which refuses occupied ports:

```powershell
cd C:\Nexaura
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\stop-payment-simulator.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-payment-simulator.ps1 -Currency Both
```

For backend regression, choose a separate disposable SQL test prefix and isolated build outputs so running business API binaries are untouched:

```powershell
cd C:\Nexaura
$env:AMAARA_TEST_SQL_CONNECTION = 'Server=localhost;Database=AmaaraPhase6Tests_Regression_' + [Guid]::NewGuid().ToString('N') + ';Integrated Security=true;Encrypt=true;TrustServerCertificate=true'
dotnet test .\tests\be.Tests\be.Tests.csproj --configuration Debug --artifacts-path .\.runtime\payment-regression-build
Remove-Item Env:AMAARA_TEST_SQL_CONNECTION
cd .\client
npm.cmd ci
npm.cmd run lint
npm.cmd run typecheck
npm.cmd test
```

The SQL test exercises both currencies, tampered client totals, idempotent checkout/retry, all four payment outcomes, inventory, signed event replay/conflict, reconciliation and refund constraints. Existing gateway/webhook/auth tests remain in the backend suite. The dedicated browser runner performs real browser checkout/payment/return/retry/refund interactions, security boundary checks, responsive overflow checks and an accessibility scan against SQL persistence.

## Development boundary and genuine provider testing

The host is under `tests/be.PaymentSimulator` and references existing test fixtures. The simulator, test fixture assembly and acceptance host are all nonpublishable and nonpackable. The production `be` project has no reference to them. The simulator server requires Development, an explicit environment opt-in and `--local-payments`; it binds loopback only and requires an exact Host, per-run HttpOnly cookie and approved write Origin. Production/staging cannot start it. Synthetic credential collection endpoints are blocked. Email is captured locally and background/external delivery jobs are removed in the fixture host.

The Vite simulator module is injected only during development serve. Building simulator mode, or building any mode with its opt-in flag, fails. Normal storefront/admin builds omit its routes, redirect, banner and controls. Release artifact checks reject simulator assemblies, scripts and browser markers. Do not package the test host or publish source directories as release artifacts.

No production payment configuration, existing SQL data, migrations in the business database, or real integration behavior is changed. Zahls remains disabled inside this lab; historical Datatrans records remain outside it. Simulation is distinct from genuine provider TEST mode.

Genuine provider credentials are still required to test provider-hosted checkout, provider-specific authentication/signatures, actual provider webhooks, settlement, merchant dashboard reconciliation and actual refunds. A reachable provider-approved callback URL may also be required for that separate testing. Real money requires a legitimate live merchant setup and an explicitly supported live integration; this work does not enable live Zahls payments.
