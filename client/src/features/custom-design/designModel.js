import { safeJson } from '@/shared/utils/safeJson.ts';
export function optionsByGroup(options = []) {
  return options.filter((option) => option.isActive).reduce((groups, option) => {
    (groups[option.group] ||= []).push(option);
    return groups;
  }, {});
}

export function customDesignRequest(form) {
  return {
    ...form,
    width: Number(form.width),
    height: Number(form.height),
    quantity: Number(form.quantity),
    editorStateJson: JSON.stringify({ schemaVersion: 1, preview: { text: form.customText, alignment: form.textAlignment } }),
  };
}

export function readCustomSnapshot(value) {
  const snapshot = safeJson(value);
  return snapshot?.SnapshotVersion === 1 || snapshot?.snapshotVersion === 1 ? snapshot : null;
}
