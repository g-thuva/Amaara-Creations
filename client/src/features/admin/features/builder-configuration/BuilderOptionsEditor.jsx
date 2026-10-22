import { DataTable } from '@/features/admin/shared/components/DataTable.jsx';
import { ConfirmAction } from '@/features/admin/shared/components/ConfirmAction.jsx';
export function BuilderOptionsEditor({ draft, changeOption, addOption, removeOption }) {
  const indexOf = option => draft.options.indexOf(option);
  return <section className="card"><div className="card-header"><h2 className="card-title">Customer options</h2><button className="btn btn-outline-primary" onClick={addOption}>Add option</button></div><DataTable caption="Customer options" captionVisible={false} rows={draft.options} rowKey={(option, index) => `${index}-${option.id || 'new'}`} empty="No customer options yet" emptyDescription="Add an option above to configure a shape, material, finish, font or colour." columns={[
    { key: 'group', label: 'Group', render: option => <select className="form-control" aria-label={`Option group ${indexOf(option) + 1}`} value={option.group} onChange={event => changeOption(indexOf(option), 'group', event.target.value)}>{['Shape', 'Material', 'Finish', 'Font', 'Colour'].map(group => <option key={group}>{group}</option>)}</select> },
    { key: 'code', label: 'Code', render: option => <input className="form-control" aria-label={`Option code ${indexOf(option) + 1}`} value={option.code} onChange={event => changeOption(indexOf(option), 'code', event.target.value)} /> },
    { key: 'label', label: 'Label', render: option => <input className="form-control" aria-label={`Option label ${indexOf(option) + 1}`} value={option.label} onChange={event => changeOption(indexOf(option), 'label', event.target.value)} /> },
    { key: 'unitPriceAdjustment', label: 'Per-unit adjustment', render: option => <input className="form-control" type="number" min="0" step="0.01" aria-label={`Option price adjustment ${indexOf(option) + 1}`} value={option.unitPriceAdjustment || 0} onChange={event => changeOption(indexOf(option), 'unitPriceAdjustment', event.target.value)} /> },
    { key: 'active', label: 'Active', render: option => <input type="checkbox" aria-label={`Option active ${indexOf(option) + 1}`} checked={option.isActive} onChange={event => changeOption(indexOf(option), 'isActive', event.target.checked)} /> },
    { key: 'actions', label: 'Actions', render: option => <ConfirmAction onConfirm={() => removeOption(indexOf(option))} title="Remove builder option" detail={`Remove ${option.label || 'this option'} from the draft?`}><button className="btn btn-danger btn-sm">Remove</button></ConfirmAction> },
  ]} /></section>;
}
