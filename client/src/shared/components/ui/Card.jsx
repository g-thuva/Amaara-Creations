/**
 * Reusable card wrapper with elevation variants.
 * Variants: flat, raised, outlined.
 */
export function Card({ children, variant = 'raised', className = '', ...props }) {
  return (
    <div className={`s-card s-card--${variant} ${className}`.trim()} {...props}>
      {children}
    </div>
  );
}

