import { EmptyState } from '../components/storefront/UI';
export default function NotFound() { return <div className="s-container s-page"><h1 className="s-eyebrow">404 / Page not found</h1><EmptyState title="This page has wandered off." to="/" label="Back to home">Explore the shop through the navigation above, or start again at home.</EmptyState></div>; }
