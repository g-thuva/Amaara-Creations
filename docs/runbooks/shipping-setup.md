# Shipping setup

Checkout lists active delivery methods whose active zone matches the saved
address, store currency and any configured order limits. An empty shipping
configuration returns no delivery options; the application does not invent a
rate while calculating a quote.

An empty Development store using LKR starts with the following editable rates
for Sri Lanka. These defaults were selected with the owner's authorization.

| Method | Charge | Free shipping |
| --- | --- | --- |
| Standard delivery | LKR 350 | Orders of LKR 5,000 or more |
| Express delivery | LKR 550 | None |

The initial zone has no region, postal-code or phone requirements. Delivery
estimates are left unset until configured by the business. The initializer runs
only in Development with LKR and only when both shipping zones and methods are
empty. It preserves existing setup, price changes and inactive methods. No
schema migration is required. Production shipping is configured through admin.

In **Admin → Shipping**, create a delivery zone first, then add or edit a delivery
method. Configure the name, code, currency, charge, order limits, free-shipping
threshold and estimates. Enable **Active** for a method to appear at checkout.
Keep **Test rate (excluded from regular checkout)** unchecked for regular
delivery rates. Test rates are available only in Development with
`Checkout:AllowDevelopmentShipping=true`.

New methods default to regular rates, consistent with the API. Saving a new
zone selects it for a new method; subsequent saves update the same zone/method.
Checkout can reload delivery options after the configuration changes. Outside
the configured delivery area, methods remain unavailable and payment stays
disabled. Selecting a method fetches a fresh server quote with its current rate.
