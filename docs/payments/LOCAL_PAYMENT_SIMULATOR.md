# Runtime simulator retired

The customer-facing simulator was removed on 27 October 2026. Its executable,
Vite aliases, synthetic sign-in, outcome screens, endpoints and startup scripts
are no longer available. Normal storefront authentication uses Identity.

Existing disposable simulator databases and historical payment records were
retained. They are not connected to the business API. No cleanup is authorized
by this change. Automated payment doubles and SQL lifecycle tests remain solely
in the test assembly under `tests/be.Tests/Fixtures/Payments`.

Use [genuine Zahls TEST setup](ZAHLS_TEST_SETUP.md) and the
[verification report](ZAHLS_VERIFICATION_REPORT.md).
