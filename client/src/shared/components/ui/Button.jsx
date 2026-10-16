export function Button({
  children,
  variant = "primary",
  className = "",
  ...props
}) {
  return (
    <button
      className={`s-button s-button-${variant} ${className}`.trim()}
      {...props}
    >
      {children}
    </button>
  );
}
