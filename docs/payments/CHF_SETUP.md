# CHF checkout

The store currency is CHF. New product, custom-builder, promotion and shipping
forms default to CHF. Catalog filtering, cart compatibility, server quotes,
payment sessions, account totals and dashboard revenue use CHF. Dashboard
revenue excludes TEST/Sandbox payments. Historical records retain their stored
currency; changing a default never converts an amount.

The editable Development shipping defaults are Standard delivery at CHF 3.50
(free from CHF 50) and Express delivery at CHF 5.50. Manage prices and delivery
areas in **Admin → Shipping**. Configure product and custom-builder prices in
their corresponding admin pages.

`20261025150116_UseChfProductCurrencyDefault` changes the SQL default for new
products to CHF. It does not update existing product prices, orders, payments,
refunds or financial snapshots. Existing catalogs require explicit CHF prices;
an old amount must not acquire a different currency through a display change.

## Zahls hosted TEST payments

The existing Zahls adapter supports CHF in TEST mode. CHF selection alone does
not complete merchant setup. Store the merchant instance name, API key and
webhook signing key in .NET user secrets or private backend configuration, using
the keys shown in `be/appsettings.Development.Local.example.json`.

Configure verified merchant payment-method identifiers, the public HTTPS webhook
URL and the frontend return URL. Set `TestProviderConfigured` and
`ChfSupportedByTestProvider` only after confirming them with the merchant test
account, then enable the gateway and restart the API. Keep credentials out of
the frontend and public site settings. A generic admin site-setting row does
not configure the payment gateway.

Checkout enables **Pay securely** when a valid address, eligible delivery method,
current quote and available CHF gateway are present. Choosing card or another
verified payment method happens on the hosted provider page. The current
adapter does not implement LIVE mode.

Coupons are optional. A blank or whitespace-only code is sent as `null`, and
clearing the coupon field removes any previously applied or rejected code.
A rejected code must be corrected or removed before payment. Checkout displays
the current payment blocker beside the button, including an unavailable gateway,
and **Check payment availability** requests a fresh quote after setup changes.

The separate local simulator now defaults to CHF. It uses its own disposable
database and demonstrates checkout without merchant access; see
[the simulator guide](local-simulator.md).
