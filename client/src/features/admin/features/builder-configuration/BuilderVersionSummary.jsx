import { formatDateTime } from "@/shared/utils/formatDate.ts";
export function BuilderVersionSummary({ data }) {
  return (
    <section className="card">
      <div className="card-header">
        <h3 className="card-title">Version history</h3>
      </div>
      <div className="table-responsive">
        <table>
          <thead>
            <tr>
              <th scope="col">Version</th>
              <th scope="col">Status</th>
              <th scope="col">Published</th>
            </tr>
          </thead>
          <tbody>
            {data.versions.map((version) => (
              <tr key={version.id}>
                <td>{version.versionNumber}</td>
                <td>{version.status}</td>
                <td>
                  {version.publishedAt
                    ? formatDateTime(version.publishedAt)
                    : "—"}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}
