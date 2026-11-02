let locks = 0;
let previous;

export function lockDialogScroll() {
  if (locks++ === 0) {
    const body = document.body;
    const root = document.documentElement;
    previous = { overflow: body.style.overflow, scrollbarGutter: root.style.scrollbarGutter };
    // Reserve an existing scrollbar without adding padding to fixed-width customer layouts.
    const styles = getComputedStyle(root);
    const minimum = Math.max(parseFloat(styles.minWidth) || 0, parseFloat(getComputedStyle(body).minWidth) || 0);
    if (window.innerWidth > root.clientWidth && root.clientWidth >= minimum && !(styles.scrollbarGutter || '').includes('stable')) {
      root.style.scrollbarGutter = 'stable';
    }
    body.style.overflow = 'hidden';
  }
  let released = false;
  return () => {
    if (released) return;
    released = true;
    if (--locks === 0) {
      document.body.style.overflow = previous.overflow;
      document.documentElement.style.scrollbarGutter = previous.scrollbarGutter;
    }
  };
}
