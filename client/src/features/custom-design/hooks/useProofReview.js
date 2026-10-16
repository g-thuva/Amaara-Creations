import { useState } from 'react';
import { useResource } from '@/shared/hooks/useResource.js';
import { useAuthenticatedMutation } from '@/features/auth/hooks/useAuthenticatedMutation.js';
import { customDesignApi } from '@/features/custom-design/customDesignApi.js';
import { downloadFile } from '@/shared/utils/downloadFile.js';

export function useProofReview(id) {
  const design = useResource(() => customDesignApi.getDesign(id), id);
  const { mutate, pending } = useAuthenticatedMutation();
  const [comment, setComment] = useState('');
  const latest = design.data?.proofRevisions?.[0];

  const download = async (proof) => {
    await mutate(async () => {
      const blob = await customDesignApi.downloadPrivate(proof.downloadUrl);
      downloadFile(blob, `proof-${proof.revisionNumber}`);
    });
  };
  const respond = async (approve) => {
    if (!latest) return;
    await mutate(() => approve ? customDesignApi.approveProof(id, latest.revisionNumber, comment) : customDesignApi.requestProofChanges(id, latest.revisionNumber, comment), design.reload, approve ? 'Proof approved.' : 'Change request sent.');
    setComment('');
  };

  return { design, latest, pending, comment, setComment, download, respond };
}
