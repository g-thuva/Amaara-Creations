import { useState } from "react";
import { useResource } from "@/shared/hooks/useResource.js";
import { useAdminMutation } from "@/features/admin/shared/hooks/useAdminMutation.js";
import { productsApi } from "@/features/admin/features/products/productsApi.js";
export function useProductTools(activeProduct, changed) {
  const [variantForm, setVariantForm] = useState({
    sku: "",
    name: "",
    priceOverride: "",
    stockQuantity: "0",
    isActive: true,
  });
  const [mediaFile, setMediaFile] = useState(null);
  const [mediaAltText, setMediaAltText] = useState("");
  const resource = useResource(async (signal) => {
    const [variants, media] = await Promise.all([
      productsApi.getProductVariants(activeProduct.id, signal),
      productsApi.getProductMedia(activeProduct.id, signal),
    ]);
    return { variants, media };
  }, String(activeProduct.id));
  const mutation = useAdminMutation(resource.reload);
  const media = resource.data?.media || [];
  const saveVariant = async (event) => {
    event.preventDefault();
    return mutation.run(async () => {
      await productsApi.createProductVariant(activeProduct.id, {
        ...variantForm,
        priceOverride:
          variantForm.priceOverride === ""
            ? null
            : Number(variantForm.priceOverride),
        stockQuantity: Number(variantForm.stockQuantity),
      });
      setVariantForm({
        sku: "",
        name: "",
        priceOverride: "",
        stockQuantity: "0",
        isActive: true,
      });
      changed();
    });
  };

  const uploadMedia = async (event) => {
    event.preventDefault();
    if (!mediaFile) return;
    return mutation.run(async () => {
      await productsApi.uploadProductMedia(activeProduct.id, mediaFile, {
        altText: mediaAltText,
        isPrimary: media.length === 0,
        sortOrder: media.length,
      });
      setMediaFile(null);
      setMediaAltText("");
      changed();
    });
  };
  const makePrimary = (item) =>
    mutation.run(() =>
      productsApi.updateProductMedia(activeProduct.id, item.id, {
        isPrimary: true,
      }),
    );
  return {
    resource,
    mutation,
    variantForm,
    setVariantForm,
    setMediaFile,
    mediaAltText,
    setMediaAltText,
    saveVariant,
    uploadMedia,
    makePrimary,
  };
}
