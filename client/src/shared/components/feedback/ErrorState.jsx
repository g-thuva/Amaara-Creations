import { customerError } from '@/shared/api/apiError.ts';
import { Button } from '@/shared/components/ui/Button.jsx';
import Icon from '@/shared/components/ui/Icon.jsx';
export function ErrorState({ error, retry }) {
  return (
    <div className="s-error-state" role="alert">
      <span className="s-error-state__icon" aria-hidden="true">
        <Icon name="refresh" />
      </span>
      <h3 className="s-error-state__title">Something went wrong</h3>
      <p className="s-error-state__message">{customerError(error)}</p>
      {retry && (
        <Button variant="secondary" onClick={retry}>
          <Icon name="refresh" /> Try again
        </Button>
      )}
    </div>
  );
}
