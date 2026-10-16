import { blankPage } from "@/features/admin/features/content/contentForm.js";
export function ContentForm({
  form,
  setForm,
  editing,
  setEditing,
  save,
  updateSection,
  pending,
}) {
  return (
    <div className="card">
      <div className="card-header">
        <h3 className="card-title">{editing ? "Edit Page" : "Create Page"}</h3>
      </div>
      <form className="add-product-form" onSubmit={save}>
        <div className="form-row">
          <label className="form-group">
            Title
            <input
              className="form-control"
              required
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
        <div className="form-actions">
          <button
            type="button"
            className="btn-cancel"
            onClick={() => {
              setForm(blankPage);
              setEditing(null);
            }}
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
