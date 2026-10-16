export const blank = {
  code: "",
  name: "",
  discountType: 1,
  discountValue: "",
  currency: "",
  startsAt: "",
  endsAt: "",
  minimumSpend: "",
  maximumDiscount: "",
  usageLimit: "",
  perCustomerUsageLimit: "",
  isActive: false,
  appliesToStandard: true,
  appliesToCustom: true,
};
export const localDate = (value) =>
  value
    ? new Date(
        new Date(value).getTime() - new Date(value).getTimezoneOffset() * 60000,
      )
        .toISOString()
        .slice(0, 16)
    : "";
