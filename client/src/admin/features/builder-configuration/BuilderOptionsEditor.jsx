export function BuilderOptionsEditor({
  draft,
  changeOption,
  addOption,
  removeOption,
}) {
  return (
    <section className="card">
      <div className="card-header">
        <h3 className="card-title">Customer options</h3>
        <button className="btn btn-outline-primary" onClick={addOption}>
          Add option
        </button>
      </div>
      <div className="table-responsive">
        <table>
          <thead>
            <tr>
              <th scope="col">Group</th>
              <th scope="col">Code</th>
              <th scope="col">Label</th>
              <th scope="col">Per-unit adjustment</th>
              <th scope="col">Active</th>
              <th scope="col">Actions</th>
            </tr>
          </thead>
          <tbody>
            {draft.options.map((option, index) => (
              <tr key={`${index}-${option.id || "new"}`}>
                <td>
                  <select
                    className="form-control"
                    aria-label={`Option group ${index + 1}`}
                    value={option.group}
                    onChange={(event) =>
                      changeOption(index, "group", event.target.value)
                    }
                  >
                    {["Shape", "Material", "Finish", "Font", "Colour"].map(
                      (group) => (
                        <option key={group}>{group}</option>
                      ),
                    )}
                  </select>
                </td>
                <td>
                  <input
                    className="form-control"
                    aria-label={`Option code ${index + 1}`}
                    value={option.code}
                    onChange={(event) =>
                      changeOption(index, "code", event.target.value)
                    }
                  />
                </td>
                <td>
                  <input
                    className="form-control"
                    aria-label={`Option label ${index + 1}`}
                    value={option.label}
                    onChange={(event) =>
                      changeOption(index, "label", event.target.value)
                    }
                  />
                </td>
                <td>
                  <input
                    className="form-control"
                    type="number"
                    min="0"
                    step="0.01"
                    aria-label={`Option price adjustment ${index + 1}`}
                    value={option.unitPriceAdjustment || 0}
                    onChange={(event) =>
                      changeOption(
                        index,
                        "unitPriceAdjustment",
                        event.target.value,
                      )
                    }
                  />
                </td>
                <td>
                  <input
                    type="checkbox"
                    aria-label={`Option active ${index + 1}`}
                    checked={option.isActive}
                    onChange={(event) =>
                      changeOption(index, "isActive", event.target.checked)
                    }
                  />
                </td>
                <td>
                  <button
                    className="btn btn-danger btn-sm"
                    onClick={() => removeOption(index)}
                  >
                    Remove
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}
