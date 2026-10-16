import { customerError } from '@/shared/api/apiError.ts';
import { Button } from '@/shared/components/ui/Button.jsx';
export function ErrorState({ error, retry }) {
  return (
    <div className="s-alert s-error" role="alert">
      <p>{customerError(error)}</p>
      {retry && (
        <Button variant="secondary" onClick={retry}>
          Try again
        </Button>
      )}
    </div>
  );
}
