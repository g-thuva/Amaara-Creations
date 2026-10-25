# Sales Command dashboard

The existing admin dashboard now uses a navy and gold, four-row desktop grid:
compact header/period control, four KPI cards, sales overview and breakdown,
then recent orders and top products. Tables/lists scroll inside their cards.
Tablet/mobile layouts flow normally. No dependency was added.

## Data availability and intentional limitations

The production dashboard does not contain fixture sales. All five existing
dashboard requests run independently and in parallel through `dashboardApi`.
Each request has its own abort, stale-response protection, loading/error state
and retry. The actual API prefix is `/api/v1`, and the recent-orders path is
`/admin/dashboard/recent-orders`, not `/recentorders`.

The backend provides a monthly series, but its buckets use **UTC calendar
months**. Reassigning these totals to Asia/Colombo months or days would be
incorrect: orders between 18:30 and midnight UTC can belong to another local
day/month. Individual recent orders cannot repair this because the list is a
limited sample, has no coverage guarantee and omits payment environments.
The existing paginated admin orders list also omits environments, and order
details expose payment currency/provider/status/amount but not provider mode.
The implementation therefore does not make an expensive, incomplete traversal
of those lists or manufacture local history from monthly aggregates.

With nonempty order data, the selected-period chart, monetary status shares,
best day, daily average and comparisons show **not available / —**. The period
control changes the Colombo date range and the adapter's requested coverage
together. KPIs with only cumulative values explicitly say **All time**; the
period control does not pretend to filter these values. When the API explicitly
reports zero orders globally, a complete empty list can be established, so the
dashboard shows true zero sales and the designed empty state for each period.
Customer/rating period values and trends remain unavailable for both states.

The product list is an explicitly labelled all-time snapshot. Its percentages
are shares of the **returned top 10 product revenues**, not shares of all order
value. The API selects those 10 by quantity; the frontend sorts that returned
subset by revenue. The breakdown's disclosure shows the same available
snapshot, grouping entries 6–10 as `Others (in top 10)`. We cannot infer the
revenue of all omitted products or use order totals as an item-revenue
denominator because shipping, discounts and taxes can differ.

Recent orders are individually formatted in CHF and labelled as including all
environments, without aggregation. Non-CHF rows are omitted with a count.
Legacy revenue/product endpoints already filter to CHF, exclude cancelled
orders and exclude orders with TEST/Sandbox payments. The UI performs no
currency conversion and never aggregates the recent sample.

## Exact existing response shapes

These are response contracts/source inspection, not proposed API changes.
All totals/counts are JSON numbers, all named fields use camelCase.

| GET endpoint (after `/api/v1`) | Response |
| --- | --- |
| `/admin/dashboard` | `{ currency, totalRevenue, totalOrders, totalCustomers, totalProducts, averageRating, revenueToday, revenueThisMonth, ordersToday, ordersThisMonth, pendingOrders, processingOrders, shippedOrders, deliveredOrders }` |
| `/admin/dashboard/revenue` | `{ currency, totalRevenue, revenueToday, revenueThisWeek, revenueThisMonth, revenueThisYear, monthlyRevenue: [{ month: string (full month name), year: number, revenue: number }] }` |
| `/admin/dashboard/orders` | `{ totalOrders, ordersToday, ordersThisWeek, ordersThisMonth, ordersThisYear, pendingOrders, processingOrders, shippedOrders, deliveredOrders, cancelledOrders }` |
| `/admin/dashboard/products` | `{ currency, totalProducts, activeProducts, inactiveProducts, outOfStockProducts, lowStockProducts, topSellingProducts: [{ productId, productName, productImageUrl, totalQuantitySold, totalRevenue }] }` |
| `/admin/dashboard/recent-orders?limit=5` | `[{ id, orderNumber, customerName, customerEmail, total, currency, status, orderDate: UTC DateTime string }]` |

The controller takes no date/currency/environment filter on these endpoints.
The fixed `Today/ThisMonth` aggregates also use UTC boundaries, not the selected
Colombo period. The order-status fields are **counts**, not monetary amounts.
Revenue is order value, not settled payment funds. None supplies prior-period
customers, rating history, prior-period order totals or settled series.

## Suggested backend contract (documentation only)

To supply the requested selected-period behavior, the existing dashboard
endpoints would need explicit `from`, exclusive `to`, `timeZone`, `currency`
and `environment`, and the following additional response fields. No endpoint
or backend implementation was changed in this task.

```ts
interface PeriodSalesResponse {
  currency: 'CHF';
  environment: string;
  timeZone: 'Asia/Colombo';
  from: string; // inclusive UTC instant
  to: string;   // exclusive UTC instant
  complete: true;
  current: {
    orderValue: number;
    orders: number;
    customers: number; // define active or newly registered customers explicitly
    averageRating: number | null;
    buckets: Array<{
      date: string; // YYYY-MM-DD Colombo calendar date
      orderValue: number;
      orders: number;
      settledFunds?: number; // only when settlement time basis is defined
    }>;
    byStatus: Array<{ status: string; orderValue: number; orders: number }>;
    byProduct: Array<{
      productId: number;
      productName: string;
      productImageUrl: string;
      quantity: number;
      revenue: number;
    }>;
    otherProductRevenue: number;
    totalProductRevenue: number;
  };
  previous: PeriodSalesResponse['current'];
}
```

`previous` must cover the immediately preceding span of the same number of
calendar days. `3M`/`12M` currently mean current month to date plus the preceding
2/11 months; comparison remains an equal day count, rather than comparing
unequal calendar month lengths. UTC instants stay UTC internally.

The typed frontend adapter (`dashboardContracts.ts` / `dashboardAdapter.js`)
already accepts a complete, explicitly scoped list of timestamped orders with
coverage of both periods. It rejects incomplete coverage, missing scope,
invalid amounts/dates and mixed currencies/environments. It buckets at
Colombo midnight and derives order-value, counts, status shares and trends.
Its populated path is covered by unit tests. It is not wired to a hypothetical
API response; the production adapter only uses the current response contracts.

## Files

- Updated dashboard page, summary, recent orders and top products under
  `client/src/features/admin/features/dashboard/`.
- Added `Dashboard.css`, `useDashboardData.js`, `dashboardAdapter.js`, typed
  contracts and adapter tests there; replaced `Dashboard.module.css`.
  Replaced the unused all-or-nothing `useDashboard.js` with the independent hook.
- Extended `dashboardApi.js` to call the existing revenue/order-stat endpoints
  and accept abort signals. No HTTP route was changed.
- Added shared `charts/BarChart.jsx`, `DonutChart.jsx`, `TrendChip.jsx`,
  `Sparkline.jsx`, `ChartTooltip.jsx`, `charts.css` and chart state tests.
- Added `client/src/utils/percent.js`, `formatMoney.js`, `dateBuckets.js`
  and unit tests.
- Added chart/layout tokens in `adminTokens.css`.
- Added `client/e2e/admin/sales-command.spec.ts` and the explicitly labelled
  component fixture in `client/e2e/fixtures/sales-charts.{html,jsx}`. Neither
  production application imports or builds this fixture entry.

The pre-existing user edit in `client/index.html` was preserved.

## Accessibility and behavior

- Pure SVG charts: no chart library or additional runtime dependency.
- Largest-remainder shares allocate integer tenths to sum to exactly 100.0%.
  Missing/zero comparison baselines return `null`; the UI shows `New` or `—`.
- Chart bars support hover, focus, touch click, Left/Right/Home/End and Escape;
  a table alternative replaces the graphic inside the same reserved card.
- An accessible image summary accompanies the interactive SVG group. Keeping
  the group interactive avoids nesting buttons inside an atomic `role=img`.
- Donut legend text remains full contrast while the graphical marks dim.
  Navy outlines and separated arcs make gold/light segments distinguishable
  against the surface. Status and trends include text/icons.
- All new CSS colors reference tokens. Dynamic geometry uses SVG presentation
  attributes; no inline style attributes are used.
- Reduced motion removes count-up, chart growth/draw, shimmer and transitions.

## Verification and screenshots

Screenshots are under `.runtime/sales-command/`. `api-populated-unavailable-history`
captures the real disposable acceptance API with populated summaries and the
honest unavailable-history state. `empty`, `loading` and `error` use interception
of current response shapes/failures in the browser tests only. `chart-fixture-populated`
captures the separately labelled synthetic component fixture, never production
sales. Desktop/tablet/mobile files identify their viewport width in the name.

Passed verification on 23/10/2026:

- ESLint and TypeScript checking.
- All 281 unit/component tests in 35 test files.
- All 5 Sales Command Playwright tests, including keyboard chart interactions,
  previous-series checkbox, accessible table, focusable donut legend, independent
  retries and retained period selection.
- Automated WCAG A/AA checks (including 2.2 tags) for the dashboard states at
  1920, 1440, 1280, 1024, 768 and 390px; populated chart fixture at desktop,
  tablet and mobile. No violations in those checks.
- No document overflow at 1920×1080, 1440×900, 1280×900 and 1280×800. Tables,
  product lists and disclosed data scroll inside cards. Smaller layouts flow.
- Admin build, storefront build and storefront bundle-isolation check.
- No new raw hex colors outside tokens and no inline React style attributes.

Screenshots (paths relative to this document):

| State | Desktop | Tablet | Mobile |
| --- | --- | --- | --- |
| Real API with populated summaries; history unavailable | [1440px](../../.runtime/sales-command/api-populated-unavailable-history-1440.png) | [1024px](../../.runtime/sales-command/api-populated-unavailable-history-1024.png) | [390px](../../.runtime/sales-command/api-populated-unavailable-history-390.png) |
| True zero / empty response fixture | [1440px](../../.runtime/sales-command/empty-1440.png) | [1024px](../../.runtime/sales-command/empty-1024.png) | [390px](../../.runtime/sales-command/empty-390.png) |
| Revenue loading, other cards available | [1440px](../../.runtime/sales-command/loading-1440.png) | [1024px](../../.runtime/sales-command/loading-1024.png) | [390px](../../.runtime/sales-command/loading-390.png) |
| Revenue error, other cards available | [1440px](../../.runtime/sales-command/error-1440.png) | [1024px](../../.runtime/sales-command/error-1024.png) | [390px](../../.runtime/sales-command/error-390.png) |
| Populated chart component, labelled synthetic test data | [1440px](../../.runtime/sales-command/chart-fixture-populated-1440.png) | [1024px](../../.runtime/sales-command/chart-fixture-populated-1024.png) | [390px](../../.runtime/sales-command/chart-fixture-populated-390.png) |

These are local screenshots of disposable test accounts/fixtures. They are not
production business figures or claims about production sales. Automated checks
do not replace assistive-technology or manual accessibility evaluation.
