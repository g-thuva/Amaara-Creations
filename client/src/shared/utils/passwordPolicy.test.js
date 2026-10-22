import { describe, expect, it } from 'vitest';
import { PASSWORD_INPUT_PROPS, passwordPolicyError } from './passwordPolicy.js';

describe('new password policy', () => {
  it.each([
    ['R7!abc', true], ['ABC1!Z', true], ['R7!ab', false],
    ['r7!abc', false], ['Rz!abc', false], ['R71abc', false],
    ['R71 ab', false], ['', false], ['Ä٧!abc', true],
    ['R7😀abc', true], ['R7😀ab', false],
    ['R7!' + 'a'.repeat(125), true], ['R7!' + 'a'.repeat(126), false],
  ])('validates length and all required character types (%s)', (value, accepted) => {
    expect(passwordPolicyError(value) === '').toBe(accepted);
    // Browser pattern and submit handlers must apply the same rule.
    expect(new RegExp(`^(?:${PASSWORD_INPUT_PROPS.pattern})$`, 'v').test(value)).toBe(accepted);
  });
});
