import AppIcon from "@/shared/components/ui/Icon.jsx";
export function SettingsList({ items, setForm }) {
  return (
    <div className="products-table-container">
      <table className="admin-table">
        <thead>
          <tr>
            <th scope="col">Key</th>
            <th scope="col">Value</th>
            <th scope="col">Type</th>
            <th scope="col">Public</th>
            <th scope="col">Actions</th>
          </tr>
        </thead>
        <tbody>
          {items.map((item) => (
            <tr key={item.id}>
              <td>{item.key}</td>
              <td>{item.value}</td>
              <td>{item.valueType}</td>
              <td>{item.isPublic ? "Yes" : "No"}</td>
              <td>
                <button
                  className="btn-edit"
                  aria-label={`Edit ${item.key}`}
                  onClick={() => setForm(item)}
                >
                  <AppIcon name="edit" />
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
