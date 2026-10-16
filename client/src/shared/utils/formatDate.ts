// Customer dates use the business timezone; admin timestamps retain local display.
export function formatDate(value: string) {
  const parsed = new Date(value);
  return !value || Number.isNaN(parsed.getTime()) ? '—' : new Intl.DateTimeFormat('en-LK', { dateStyle: 'medium', timeZone: 'Asia/Colombo' }).format(parsed);
}
export function formatDateTime(value: string) {
  const parsed = new Date(value);
  return !value || Number.isNaN(parsed.getTime()) ? '—' : parsed.toLocaleString();
}
export function formatLocalDate(value: string) {
  const parsed = new Date(value);
  return !value || Number.isNaN(parsed.getTime()) ? '—' : parsed.toLocaleDateString();
}
