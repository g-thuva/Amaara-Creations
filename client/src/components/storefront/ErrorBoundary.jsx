import { Component } from 'react';
import { reportClientError } from '../../services/clientTelemetry';
export default class ErrorBoundary extends Component {
  state = { failed: false };
  static getDerivedStateFromError() { return { failed: true }; }
  componentDidCatch(error) { reportClientError('storefront.render', error); }
  render() {
    if (this.state.failed) return <div className="s-empty" role="alert"><h1>Something went wrong</h1><p>Please reload the page to try again.</p><button className="s-button s-button-primary" onClick={() => window.location.reload()}>Reload page</button></div>;
    return this.props.children;
  }
}
