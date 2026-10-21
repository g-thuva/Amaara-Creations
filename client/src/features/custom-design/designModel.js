import { safeJson } from '@/shared/utils/safeJson.ts';
import { readEditorState } from './editorState.js';
export function optionsByGroup(options = []) {
  return options.filter((option) => option.isActive).reduce((groups, option) => {
    (groups[option.group] ||= []).push(option);
    return groups;
  }, {});
}

export function customDesignRequest(form) {
  const { editor, name, shapeCode, materialCode, finishCode, fontCode, colourCode, customText, textAlignment, rowVersion } = form;
  return {
    name, shapeCode, materialCode, finishCode, fontCode, colourCode, customText, textAlignment, rowVersion,
    width: Number(form.width),
    height: Number(form.height),
    quantity: Number(form.quantity),
    editorStateJson: JSON.stringify(readEditorState(editor)),
  };
}

export function readCustomSnapshot(value) {
  const snapshot = safeJson(value);
  return snapshot?.SnapshotVersion === 1 || snapshot?.snapshotVersion === 1 ? snapshot : null;
}
