import { useResource } from "@/shared/hooks/useResource.js";
import { mediaApi } from "@/features/admin/features/media/mediaApi.js";
import { MediaTable } from "@/features/admin/features/media/MediaTable.jsx";
import { ResourceState } from "@/features/admin/shared/components/ResourceState.jsx";
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
            <h1>Media Library</h1>
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
