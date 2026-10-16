import { EmptyState } from "@/features/admin/shared/components/EmptyState.jsx";

export function DataTable({
  caption,
  columns,
  rows,
  rowKey = (row, i) => row.id ?? i,
  empty = "No records found.",
}) {
  return (
    <div
      className="table-responsive"
      tabIndex="0"
      role="region"
      aria-label={caption}
    >
      <table className="admin-table">
        <caption>{caption}</caption>
        <thead>
          <tr>
            {columns.map((column) => (
              <th scope="col" key={column.key}>
                {column.label}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.length ? (
            rows.map((row, i) => (
              <tr key={rowKey(row, i)}>
                {columns.map((column) => (
                  <td key={column.key}>
                    {column.render
                      ? column.render(row)
                      : String(row[column.key] ?? "—")}
                  </td>
                ))}
              </tr>
            ))
          ) : (
            <tr>
              <td colSpan={columns.length}>
                <EmptyState>{empty}</EmptyState>
              </td>
            </tr>
          )}
        </tbody>
      </table>
    </div>
  );
}
