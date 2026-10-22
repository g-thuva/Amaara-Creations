import { describe, expect, it } from 'vitest';

export function isValidOtpCode(code) {
  if (!code || typeof code !== 'string') return false;
  const clean = code.trim().replace(/\s+/g, '');
  return /^\d{6}$/.test(clean);
}

export function sanitizeOtpInput(value) {
  if (!value || typeof value !== 'string') return '';
  return value.replace(/\D/g, '').slice(0, 6);
}

describe('OTP Verification Code helpers', () => {
  it('validates 6-digit numeric codes', () => {
    expect(isValidOtpCode('123456')).toBe(true);
    expect(isValidOtpCode(' 987654 ')).toBe(true);
    expect(isValidOtpCode('000000')).toBe(true);
    expect(isValidOtpCode('999999')).toBe(true);
  });

  it('rejects invalid codes', () => {
    expect(isValidOtpCode('')).toBe(false);
    expect(isValidOtpCode(null)).toBe(false);
    expect(isValidOtpCode(undefined)).toBe(false);
    expect(isValidOtpCode('12345')).toBe(false);
    expect(isValidOtpCode('1234567')).toBe(false);
    expect(isValidOtpCode('abcdef')).toBe(false);
    expect(isValidOtpCode('12345a')).toBe(false);
    expect(isValidOtpCode('12-34-56')).toBe(false);
  });

  it('sanitizes input to max 6 digits', () => {
    expect(sanitizeOtpInput('123456')).toBe('123456');
    expect(sanitizeOtpInput('123abc456')).toBe('123456');
    expect(sanitizeOtpInput('123456789')).toBe('123456');
    expect(sanitizeOtpInput('abc')).toBe('');
    expect(sanitizeOtpInput('   ')).toBe('');
  });
});
