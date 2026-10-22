export const PASSWORD_HELP = 'Use 6–128 characters, including at least one uppercase letter, one number and one symbol.';
export const PASSWORD_INPUT_PROPS = {
  minLength: 6,
  maxLength: 128,
  pattern: '(?=[\\s\\S]*\\p{Lu})(?=[\\s\\S]*\\p{Nd})(?=[\\s\\S]*[\\p{P}\\p{S}])[\\s\\S]{6,128}',
  title: PASSWORD_HELP,
};

export function passwordPolicyError(password) {
  const value = password || '';
  const length = Array.from(value).length;
  return length < 6 || length > 128 || !/\p{Lu}/u.test(value) || !/\p{Nd}/u.test(value) || !/[\p{P}\p{S}]/u.test(value) ? PASSWORD_HELP : '';
}
