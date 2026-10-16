import { useEffect, useState } from "react";
import { builderConfigurationApi } from "@/features/admin/features/builder-configuration/builderConfigurationApi.js";
import { customerError } from '@/shared/api/apiError.ts';
import { numberFields } from "@/features/admin/features/builder-configuration/builderForm.js";
export function useBuilderConfiguration() {
  const [data, setData] = useState(null);
  const [draft, setDraft] = useState(null);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const [pending, setPending] = useState(false);
  const [testQuote, setTestQuote] = useState(null);
  const load = async () => {
    setError("");
    try {
      const value = await builderConfigurationApi.getConfig();
      setData(value);
      setDraft(structuredClone(value.draft));
    } catch (requestError) {
      setError(customerError(requestError));
    }
  };
  useEffect(() => {
    load();
  }, []);
  const change = (field, value) =>
    setDraft((current) => ({
      ...current,
      [field]: numberFields.includes(field) ? Number(value) : value,
    }));
  const changeOption = (index, field, value) =>
    setDraft((current) => ({
      ...current,
      options: current.options.map((option, optionIndex) =>
        optionIndex === index
          ? {
              ...option,
              [field]:
                field === "unitPriceAdjustment" || field === "sortOrder"
                  ? Number(value)
                  : value,
            }
          : option,
      ),
    }));
  const save = async () => {
    setPending(true);
    setError("");
    try {
      const updated = await builderConfigurationApi.updateDraft(
        draft.id,
        draft,
      );
      setDraft(updated);
      setNotice("Draft saved.");
    } catch (requestError) {
      setError(customerError(requestError));
    } finally {
      setPending(false);
    }
  };
  const publish = async () => {
    setPending(true);
    setError("");
    try {
      await builderConfigurationApi.publish(draft.id);
      setNotice("Configuration published; a new draft was created.");
      await load();
    } catch (requestError) {
      setError(customerError(requestError));
    } finally {
      setPending(false);
    }
  };
  const test = async () => {
    setPending(true);
    try {
      const first = (group) =>
        draft.options.find(
          (option) => option.group === group && option.isActive,
        )?.code;
      setTestQuote(
        await builderConfigurationApi.testQuote(draft.id, {
          width: draft.minimumWidth,
          height: draft.minimumHeight,
          quantity: draft.minimumQuantity,
          shapeCode: first("Shape"),
          materialCode: first("Material"),
          finishCode: first("Finish"),
          fontCode: first("Font"),
          colourCode: first("Colour"),
          textAlignment: "center",
        }),
      );
    } catch (requestError) {
      setError(customerError(requestError));
    } finally {
      setPending(false);
    }
  };
  const addOption = () =>
    setDraft((current) => ({
      ...current,
      options: [
        ...current.options,
        {
          group: "Shape",
          code: "",
          label: "",
          unitPriceAdjustment: 0,
          isActive: true,
          sortOrder: current.options.length,
        },
      ],
    }));
  const removeOption = (index) =>
    setDraft((current) => ({
      ...current,
      options: current.options.filter((_, itemIndex) => itemIndex !== index),
    }));
  return {
    data,
    draft,
    setDraft,
    error,
    notice,
    pending,
    testQuote,
    change,
    changeOption,
    save,
    publish,
    test,
    addOption,
    removeOption,
  };
}
