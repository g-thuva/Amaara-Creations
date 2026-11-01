import { forwardRef, useId } from 'react';

export const Input = forwardRef(function Input({
  label,
  hint,
  error,
  id,
  className = '',
  required = false,
  ...props
}, ref) {
  const generatedId = useId();
  const inputId = id || generatedId;
  const hintId = hint ? `${inputId}-hint` : undefined;
  const errorId = error ? `${inputId}-error` : undefined;
  const describedBy = [hintId, errorId, props['aria-describedby']].filter(Boolean).join(' ') || undefined;

  return (
    <div className={`s-input-field ${error ? 'is-invalid' : ''}`.trim()}>
      {label && <label htmlFor={inputId}>{label}{required && <span aria-hidden="true"> *</span>}</label>}
      <input
        {...props}
        ref={ref}
        id={inputId}
        className={`s-input ${className}`.trim()}
        required={required}
        aria-describedby={describedBy}
        aria-invalid={error ? 'true' : props['aria-invalid']}
      />
      {hint && !error && <small id={hintId} className="s-input-hint">{hint}</small>}
      {error && <small id={errorId} className="s-input-error" role="alert">{error}</small>}
    </div>
  );
});
