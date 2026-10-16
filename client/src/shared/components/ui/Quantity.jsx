import { useId } from 'react';
import Icon from '@/shared/components/ui/Icon.jsx';
import { validQuantity } from '@/shared/utils/quantity.ts';
export function Quantity({
  value,
  onChange,
  max,
  disabled = false,
  label = "Quantity",
}) {
  const id = useId();
  return (
    <div className="s-quantity">
      <label htmlFor={id} className="sr-only">
        {label}
      </label>
      <button
        type="button"
        disabled={disabled || value <= 1}
        aria-label={`Decrease ${label.toLowerCase()}`}
        onClick={() => onChange(value - 1)}
      >
        <Icon name="minus" />
      </button>
      <input
        id={id}
        type="number"
        inputMode="numeric"
        min="1"
        max={max}
        step="1"
        value={value}
        disabled={disabled}
        onChange={(event) => {
          const next = Number(event.target.value);
          if (validQuantity(next, max)) onChange(next);
        }}
      />
      <button
        type="button"
        disabled={disabled || value >= max}
        aria-label={`Increase ${label.toLowerCase()}`}
        onClick={() => onChange(value + 1)}
      >
        <Icon name="plus" />
      </button>
    </div>
  );
}
