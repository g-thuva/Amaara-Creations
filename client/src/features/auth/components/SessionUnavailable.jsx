import { useAuth } from '../AuthProvider.jsx';

export default function SessionUnavailable() {
  const { retrySession, logout, authStatus } = useAuth();
  return <main className="s-container" role="alert">
    <h1>{authStatus === 'logout-error' ? 'Sign out could not be completed' : 'Unable to check your session'}</h1>
    <p>{authStatus === 'logout-error' ? 'Your server session has not been confirmed signed out. Reconnect and try again.' : 'Check your connection and try again. Your session has not been signed out.'}</p>
    <button type="button" className="s-button" onClick={() => authStatus === 'logout-error' ? logout() : retrySession()}>Try again</button>
  </main>;
}
