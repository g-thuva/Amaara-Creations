import type { ApiErrorSummary, ProblemDetails } from '../types/api';

interface ResponseLike {
  status?: number;
  data?: unknown;
}

interface AxiosErrorLike {
  message?: string;
  response?: ResponseLike;
}

const isProblemDetails = (value: unknown): value is ProblemDetails => {
  return typeof value === 'object' && value !== null && ('title' in value || 'detail' in value || 'errors' in value);
};

export const normalizeApiError = (error: AxiosErrorLike): ApiErrorSummary => {
  const data = error.response?.data;

  if (isProblemDetails(data)) {
    return {
      status: data.status ?? error.response?.status,
      message: data.detail ?? data.title ?? 'Request failed',
      correlationId: data.correlationId,
      validationErrors: data.errors
    };
  }

  if (typeof data === 'object' && data !== null && 'message' in data && typeof data.message === 'string') {
    return {
      status: error.response?.status,
      message: data.message
    };
  }

  return {
    status: error.response?.status,
    message: error.message ?? 'Request failed'
  };
};
