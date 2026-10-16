import { Button } from '@/shared/components/ui/Button.jsx';
import Icon from '@/shared/components/ui/Icon.jsx';
export function Pagination({ page, totalPages, onChange }) {
  if (totalPages <= 1) return null;
  return (
    <nav className="s-pagination" aria-label="Product pages">
      <Button
        variant="secondary"
        disabled={page <= 1}
        aria-label="Previous product page"
        onClick={() => onChange(page - 1)}
      >
        <Icon name="chevronLeft" />
        Previous
      </Button>
      <span aria-current="page" aria-live="polite">
        Page {page} of {totalPages}
      </span>
      <Button
        variant="secondary"
        disabled={page >= totalPages}
        aria-label="Next product page"
        onClick={() => onChange(page + 1)}
      >
        Next
        <Icon name="chevronRight" />
      </Button>
    </nav>
  );
}
