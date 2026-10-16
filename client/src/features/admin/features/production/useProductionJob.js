import { useState } from "react";
import { useResource } from "@/shared/hooks/useResource.js";
import { useAdminMutation } from "@/features/admin/shared/hooks/useAdminMutation.js";
import { downloadFile } from "@/shared/utils/downloadFile.js";
import { productionApi } from "@/features/admin/features/production/productionApi.js";
export function useProductionJob(id) {
  const resource = useResource(
    (signal) => productionApi.getJob(id, signal),
    id,
  );
  const mutation = useAdminMutation(resource.reload);
  const [message, setMessage] = useState("");
  const [archive, setArchive] = useState(null);
  const data = resource.data;
  const design = data?.design;
  const download = (asset) =>
    mutation.run(async () => {
      const blob = await productionApi.downloadAsset(asset.downloadUrl);
      downloadFile(blob, asset.originalFileName || `asset-${asset.id}`);
    }, "Private file downloaded.");
  const upload = (e) => {
    e.preventDefault();
    const form = new FormData(e.currentTarget);
    form.set("customerVisibleMessage", message);
    return mutation.run(async () => {
      await productionApi.uploadProof(id, form);
      setMessage("");
    }, "Proof revision created.");
  };
  const production = (e) => {
    e.preventDefault();
    const form = Object.fromEntries(new FormData(e.currentTarget));
    return mutation.run(() =>
      productionApi.updateProductionStatus(id, {
        status: form.status,
        note: form.note,
        rowVersion: design.rowVersion,
      }),
    );
  };
  const confirmArchive = async () => {
    await mutation.run(
      () => productionApi.archiveJob(id, archive),
      "Fulfilled job archived.",
    );
    setArchive(null);
  };
  return {
    resource,
    mutation,
    message,
    setMessage,
    archive,
    setArchive,
    data,
    design,
    download,
    upload,
    production,
    confirmArchive,
  };
}
