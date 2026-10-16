/* eslint-disable react-refresh/only-export-components */
import { createContext, useCallback, useContext, useState } from 'react';

const ToastContext = createContext(null);
export function ToastProvider({ children }) {
  const [notice, setNotice] = useState(null);
  const notify = useCallback((message, type = 'success') => setNotice({ message, type }), []);
  return <ToastContext.Provider value={{ notify }}>
    {children}
    {notice && <div className={`store-toast ${notice.type}`} role={notice.type === 'error' ? 'alert' : 'status'}>
      <span>{notice.message}</span><button type="button" aria-label="Dismiss notification" onClick={() => setNotice(null)}>×</button>
    </div>}
  </ToastContext.Provider>;
}
export const useToast = () => useContext(ToastContext);
