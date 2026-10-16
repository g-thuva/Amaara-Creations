import { useResource } from "../../../hooks/useResource.js";
import { mediaApi } from "./mediaApi.js";
import { MediaTable } from "./MediaTable.jsx";
import { ResourceState } from "../../shared/components/ResourceState.jsx";
export default function MediaPage() {
  const resource = useResource(
    (signal) => mediaApi.getMedia({}, signal),
    "media",
  );
  return (
    <div className="admin-page">
      <div className="page-header">
        <div className="header-content">
          <div>
            <h2>Media Library</h2>
            <p>{resource.data?.length || 0} assets</p>
          </div>
        </div>
      </div>
      <ResourceState resource={resource}>
        <MediaTable items={resource.data || []} />
      </ResourceState>
    </div>
  );
}
