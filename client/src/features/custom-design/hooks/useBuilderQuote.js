import { useEffect, useState } from 'react';
import { useResource } from '@/shared/hooks/useResource.js';
import { customerError } from '@/shared/api/apiError.ts';
import { customDesignApi } from '@/features/custom-design/customDesignApi.js';
import { customDesignRequest } from '@/features/custom-design/designModel.js';

export function useBuilderQuote(config, form, loading) {
  const key = JSON.stringify(customDesignRequest(form));
  const [debouncedKey, setDebouncedKey] = useState('');
  useEffect(() => {
    const timer = window.setTimeout(() => setDebouncedKey(key), 350);
    return () => window.clearTimeout(timer);
  }, [key]);
  const current = Boolean(config && !loading && key === debouncedKey);
  const resource = useResource(signal => customDesignApi.quote(JSON.parse(debouncedKey), signal), debouncedKey, current);
  return {
    quote: current ? resource.data : null,
    quoteError: resource.error ? customerError(resource.error, 'Choose a valid configuration to receive a quote.') : '',
  };
}
