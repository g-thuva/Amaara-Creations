import { describe, expect, it } from 'vitest';
import { normalizeApiError } from './apiError';

describe('normalizeApiError', () => {
  it('maps ProblemDetails responses with correlation ids', () => {
    const result = normalizeApiError({
      response: {
        status: 400,
        data: {
          title: 'Validation failed',
          status: 400,
          correlationId: 'phase1-test',
          errors: {
            name: ['Name is required']
          }
        }
      }
    });

    expect(result).toEqual({
      status: 400,
      message: 'Validation failed',
      correlationId: 'phase1-test',
      validationErrors: {
        name: ['Name is required']
      }
    });
  });

  it('keeps compatibility with legacy message responses', () => {
    const result = normalizeApiError({
      response: {
        status: 404,
        data: {
          message: 'Product not found'
        }
      }
    });

    expect(result).toEqual({
      status: 404,
      message: 'Product not found'
    });
  });
});
