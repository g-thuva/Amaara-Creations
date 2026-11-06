import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { AuditDecision } from './AuditDecision.jsx';

describe('administrative audit decisions', () => {
  it('keeps malformed historical evidence from breaking the surrounding page', () => {
    expect(renderToStaticMarkup(<AuditDecision value="{broken" />)).toContain('Historical details unavailable.');
    expect(renderToStaticMarkup(<AuditDecision value={null} />)).toContain('No details recorded.');
  });
  it('supports historical review status fields without showing raw JSON', () => {
    const html = renderToStaticMarkup(<AuditDecision value={'{"ModerationStatus":"Hidden"}'} />);
    expect(html).toContain('Hidden'); expect(html).not.toContain('ModerationStatus');
  });
  it('escapes operator text and displays the recorded session consequence', () => {
    const html = renderToStaticMarkup(<AuditDecision value={{ Status: 'Blocked', Reason: '<script>unsafe()</script>', RevokedSessions: 3 }} />);
    expect(html).toContain('&lt;script&gt;'); expect(html).not.toContain('<script>'); expect(html).toContain('3 sessions revoked');
  });
});
