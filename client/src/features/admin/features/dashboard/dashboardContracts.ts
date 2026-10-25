export interface SalesBucket { date: string; value: number; orders: number | null }
export interface SalesSegment { label: string; value: number }
export interface TopProduct { productId: number; productName: string; productImageUrl: string; totalQuantitySold: number; totalRevenue: number }
/** A complete, currency/environment-scoped source. Never use a recent-order sample. */
export interface ScopedSales {
  currency: 'LKR';
  environment: string;
  complete: true;
  from: string;
  to: string;
  orders: Array<{ orderDate: string; total: number; status: string; currency: string; environment: string }>;
}
export interface SalesView {
  series: SalesBucket[] | null;
  previous: SalesBucket[] | null;
  total: number | null;
  previousTotal: number | null;
  orderCount: number | null;
  status: SalesSegment[] | null;
  reason: string | null;
}
