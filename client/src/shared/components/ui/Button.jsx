export function Button({
  children,
  variant = "primary",
  className = "",
  loading = false,
  loadingLabel = "Please wait…",
  ...props
}) {
  return (
    <button
      className={`s-button s-button-${variant} ${className}`.trim()}
      {...props}
      aria-busy={loading || undefined}
      disabled={loading || props.disabled}
    >
      {loading ? loadingLabel : children}
    </button>
  );
}
