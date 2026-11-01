/**
 * Pill badge for status, tags, and labels.
 * Variants: default, success, warning, danger, terracotta.
 */
export function Badge({ children, variant = 'default', className = '' }) {
  const variantClass = variant !== 'default' ? `s-badge--${variant}` : '';
  return (
    <span className={`s-badge ${variantClass} ${className}`.trim()} data-variant={variant}>
      {children}
    </span>
  );
}

