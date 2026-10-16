import { blank } from "./settingsForm.js";
export function SettingForm({ form, setForm, save, pending }) {
  return (
    <div className="card">
      <div className="card-header">
        <h3 className="card-title">Edit Setting</h3>
      </div>
      <form className="add-product-form" onSubmit={save}>
        <div className="form-row">
          <label className="form-group">
            Key
            <input
              className="form-control"
              required
              value={form.key}
              onChange={(e) => setForm({ ...form, key: e.target.value })}
            />
          </label>
          <label className="form-group">
            Type
            <input
              className="form-control"
              value={form.valueType}
              onChange={(e) => setForm({ ...form, valueType: e.target.value })}
            />
          </label>
        </div>
        <label className="form-group">
          Value
          <textarea
            className="form-control"
            required
            value={form.value}
            onChange={(e) => setForm({ ...form, value: e.target.value })}
          />
        </label>
        <label className="form-group">
          Description
          <input
            className="form-control"
            value={form.description || ""}
            onChange={(e) => setForm({ ...form, description: e.target.value })}
          />
        </label>
        <label>
          <input
            type="checkbox"
            checked={form.isPublic}
            onChange={(e) => setForm({ ...form, isPublic: e.target.checked })}
          />{" "}
          Public
        </label>
        <div className="form-actions">
          <button
            type="button"
            className="btn-cancel"
            onClick={() => setForm(blank)}
          >
            Clear
          </button>
          <button type="submit" className="btn-submit" disabled={pending}>
            Save
          </button>
        </div>
      </form>
    </div>
  );
}
