export const validQuantity = (value: number, max = Number.MAX_SAFE_INTEGER) => Number.isSafeInteger(value) && value >= 1 && value <= max;
