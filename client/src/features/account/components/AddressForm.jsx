import { Button } from '@/shared/components/ui/Button.jsx';
import { addressFields } from '@/features/account/addressForm.js';
export default function AddressForm({ editing, pending, save }) {
  return (
        <form className="s-form" onSubmit={save}>
          <fieldset disabled={pending}>
            {addressFields.map(([key, label, max, required, autocomplete]) => (
              <label key={key}>{label}{required ? ' *' : ''}<input name={key} type={key === 'phone' ? 'tel' : 'text'} autoComplete={autocomplete} required={required} maxLength={max} defaultValue={editing?.[key] || ''} /></label>
            ))}
            <label className="s-checkbox"><input type="checkbox" name="isDefault" defaultChecked={editing?.isDefault} />Make this my default address</label>
            <Button type="submit" disabled={pending}>{pending ? 'Saving…' : 'Save address'}</Button>
          </fieldset>
        </form>
  );
}
