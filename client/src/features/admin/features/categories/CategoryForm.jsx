export function CategoryForm({
  items,
  form,
  setForm,
  editing,
  save,
  pending,
  formId,
}) {
  return (
    <form id={formId} className="add-product-form" onSubmit={save}>
      <fieldset className="admin-form-fields" disabled={pending}>

        <div className="form-row">
          <label className="form-group">
            Name <span aria-hidden="true" className="admin-required" />
            <input
              className="form-control"
              required
              data-dialog-autofocus
              value={form.name}
              onChange={(e) => setForm({ ...form, name: e.target.value })}
            />
          </label>
          <label className="form-group">
            Slug
            <input
              className="form-control"
              value={form.slug || ""}
              onChange={(e) => setForm({ ...form, slug: e.target.value })}
            />
          </label>
        </div>
        <label className="form-group">
          Description
          <textarea
            className="form-control"
            value={form.description || ""}
            onChange={(e) => setForm({ ...form, description: e.target.value })}
          />
        </label>
        <div className="form-row">
          <label className="form-group">
            Parent
            <select
              className="form-control"
              value={form.parentCategoryId || ""}
              onChange={(e) =>
                setForm({ ...form, parentCategoryId: e.target.value })
              }
            >
              <option value="">None</option>
              {items
                .filter((item) => item.id !== editing?.id)
                .map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.name}
                  </option>
                ))}
            </select>
          </label>
          <label className="form-group">
            Sort
            <input
              className="form-control"
              type="number"
              value={form.sortOrder}
              onChange={(e) => setForm({ ...form, sortOrder: e.target.value })}
            />
          </label>
        </div>
        <label>
          <input
            type="checkbox"
            checked={form.isActive}
            onChange={(e) => setForm({ ...form, isActive: e.target.checked })}
          />{" "}
          Active
        </label>
      </fieldset>
    </form>
  );
}
