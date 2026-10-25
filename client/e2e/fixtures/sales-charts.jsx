// Test-only entry point; not imported by either production application.
import React, { useState } from 'react';
import { createRoot } from 'react-dom/client';
import { MemoryRouter } from 'react-router-dom';
import { BarChart } from '../../src/features/admin/shared/components/charts/BarChart.jsx';
import { DonutChart } from '../../src/features/admin/shared/components/charts/DonutChart.jsx';
import { TrendChip } from '../../src/features/admin/shared/components/charts/TrendChip.jsx';
import '../../src/shared/styles/fonts.css';
import '../../src/features/admin/shared/styles/adminTokens.css';
import '../../src/features/admin/shared/styles/adminBase.css';
import '../../src/features/admin/features/dashboard/Dashboard.css';
const data = [1200, 1800, 900, 0, 2900, 2400, 4800, 2300, 1700, 3900, 3200, 2800, 5100, 3700, 2600, 1900, 3300, 4200, 3800, 6200, 3500, 2800, 4500, 7100, 4900, 3200, 4100, 5600, 4300, 6800].map((value, index) => ({ date: `2026-10-${String(index + 1).padStart(2, '0')}`, value, orders: value ? index % 5 + 1 : 0 }));
export function Verification() {
  const [state, setState] = useState('populated');
  return <MemoryRouter><main className="admin-app"><div className="admin-content"><h1>Chart component verification</h1><p>Synthetic test fixtures · never displayed in the production dashboard</p><div className="sales-period-control" role="group" aria-label="Fixture state">{['populated', 'empty', 'loading', 'error'].map(value => <button key={value} aria-pressed={state === value} onClick={() => setState(value)}>{value}</button>)}</div><div className="sales-chart-row"><section className="sales-panel"><div className="sales-panel-header"><h2>Sales overview</h2><TrendChip current={120} previous={100} description /></div><BarChart key={state} data={state === 'populated' ? data : []} previous={state === 'populated' ? data.map(bucket => ({ ...bucket, value: bucket.value * .8 })) : null} loading={state === 'loading'} error={state === 'error'} retry={() => setState('populated')} /></section><section className="sales-panel"><h2>Sales breakdown</h2><DonutChart items={state === 'populated' ? [{ label: 'Delivered', value: 4200 }, { label: 'Processing', value: 3300 }, { label: 'Pending', value: 2500 }] : []} /></section></div></div></main></MemoryRouter>;
}
createRoot(document.getElementById('root')).render(<Verification />);
