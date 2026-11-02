export function ContentForm({
  form,
  setForm,
  save,
  updateSection,
  pending,
  formId,
}) {
  return (
    <form id={formId} className="add-product-form" onSubmit={save}>
      <fieldset className="admin-form-fields" disabled={pending}>

        <div className="form-row">
          <label className="form-group">
            Title <span aria-hidden="true" className="admin-required" />
            <input
              className="form-control"
              required
              data-dialog-autofocus
              value={form.title}
              onChange={(e) => setForm({ ...form, title: e.target.value })}
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
          Summary
          <input
            className="form-control"
            value={form.summary || ""}
            onChange={(e) => setForm({ ...form, summary: e.target.value })}
          />
        </label>
        {form.sections.map((section, index) => (
          <div className="card" key={index}>
            <div className="card-body">
              <div className="form-row">
                <input
                  className="form-control"
                  aria-label={`Section key ${index + 1}`}
                  value={section.sectionKey}
                  onChange={(e) =>
                    updateSection(index, "sectionKey", e.target.value)
                  }
                />
                <input
                  className="form-control"
                  aria-label={`Content type ${index + 1}`}
                  value={section.contentType}
                  onChange={(e) =>
                    updateSection(index, "contentType", e.target.value)
                  }
                />
              </div>
              <textarea
                className="form-control"
                aria-label={`Section content ${index + 1}`}
                value={section.content}
                onChange={(e) =>
                  updateSection(index, "content", e.target.value)
                }
              />
            </div>
          </div>
        ))}
        <button
          type="button"
          className="btn btn-outline-primary"
          onClick={() =>
            setForm({
              ...form,
              sections: [
                ...form.sections,
                {
                  sectionKey: "",
                  contentType: "text",
                  content: "",
                  sortOrder: form.sections.length,
                  isActive: true,
                },
              ],
            })
          }
        >
          Add Section
        </button>
        <label>
          <input
            type="checkbox"
            checked={form.isPublished}
            onChange={(e) =>
              setForm({ ...form, isPublished: e.target.checked })
            }
          />{" "}
          Published
        </label>
      </fieldset>
    </form>
  );
}
