import { StatusChip } from './StatusChip.jsx';

export function AuditDecision({ value }) {
  let decision;
  try { decision = typeof value === 'string' ? JSON.parse(value) : value; } catch { return <span>Historical details unavailable.</span>; }
  if (!decision || typeof decision !== 'object' || Array.isArray(decision)) return <span>No details recorded.</span>;
  const status = decision.Status || decision.ModerationStatus;
  return <div className="admin-audit-decision">
    {typeof status === 'string' && <StatusChip status={status} />}
    {typeof decision.Reason === 'string' && <span>{decision.Reason}</span>}
    {Number.isInteger(decision.RevokedSessions) && <small>{decision.RevokedSessions} sessions revoked</small>}
    {!status && !decision.Reason && <span>No status decision recorded.</span>}
  </div>;
}
