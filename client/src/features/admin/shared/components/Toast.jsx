import { createContext, useCallback, useContext, useEffect, useRef, useState } from 'react';
import AppIcon from '@/shared/components/ui/Icon.jsx';
const ToastContext = createContext(null);
// Inline feedback remains visible; this region announces completed operations.
export function ToastProvider({ children }) {
  const [items, setItems] = useState([]); const sequence = useRef(0); const timers = useRef([]);
  useEffect(() => () => timers.current.forEach(clearTimeout), []);
  const notify = useCallback((message, tone = 'success') => {
    if (!message) return;
    const id = ++sequence.current;
    setItems(current => [...current.slice(-2), { id, message, tone }]);
    timers.current.push(setTimeout(() => setItems(current => current.filter(item => item.id !== id)), 7000));
  }, []);
  return <ToastContext.Provider value={notify}>{children}<div className="admin-toast-region" role="region" aria-label="Notifications" aria-live="polite" aria-atomic="false">{items.map(item => <div className={`admin-toast admin-toast-${item.tone}`} key={item.id} title={item.message}><AppIcon name={item.tone === 'error' ? 'x' : 'check'} /><span>{item.tone === 'error' ? 'Changes couldn’t be saved. See the details in the form.' : 'Update complete.'}</span><button aria-label="Dismiss notification" onClick={() => setItems(current => current.filter(value => value.id !== item.id))}><AppIcon name="x" /></button></div>)}</div></ToastContext.Provider>;
}
// eslint-disable-next-line react-refresh/only-export-components
export function useToast() { return useContext(ToastContext); }
