import { useBuilderConfiguration } from "./useBuilderConfiguration.js";
import { BuilderPricingEditor } from "./BuilderPricingEditor.jsx";
import { BuilderOptionsEditor } from "./BuilderOptionsEditor.jsx";
import { BuilderVersionSummary } from "./BuilderVersionSummary.jsx";
export default function BuilderConfigurationPage() {
  const config = useBuilderConfiguration();
  const { draft, data, error, notice, pending, test, save, publish } = config;
  if (!draft)
    return (
      <div className="admin-page">
        <h2>Custom builder</h2>
        <p>{error || "Loading configuration…"}</p>
      </div>
    );
  return (
    <div className="admin-page">
      <div className="page-header">
        <div className="header-content">
          <div>
            <h2>Custom builder configuration</h2>
            <p>
              Draft version {draft.versionNumber}; published version{" "}
              {data?.published?.versionNumber || "none"}.
            </p>
          </div>
          <div className="action-buttons">
            <button
              className="btn btn-outline-primary"
              disabled={pending}
              onClick={test}
            >
              Test quote
            </button>
            <button
              className="btn btn-primary"
              disabled={pending}
              onClick={save}
            >
              Save draft
            </button>
            <button
              className="btn btn-success"
              disabled={pending}
              onClick={publish}
            >
              Publish
            </button>
          </div>
        </div>
      </div>
      {error && (
        <p className="out-of-stock-alert" role="alert">
          {error}
        </p>
      )}
      {notice && (
        <p className="badge-success" role="status">
          {notice}
        </p>
      )}
      <BuilderPricingEditor {...config} />
      <BuilderOptionsEditor {...config} />
      <BuilderVersionSummary data={data} />
    </div>
  );
}
