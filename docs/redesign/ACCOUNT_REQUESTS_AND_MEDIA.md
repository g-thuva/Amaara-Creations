# Account, request and media operating boundaries

The implementation adds working local account controls: TOTP enrollment/replacement with a 10-minute setup window; single-use authenticator verification and hashed recovery codes; current-device listing and individual revocation; profile crop/cancel/retry; reauthenticated JSON export; verified email change; and owner-scoped quote/deletion-review requests with permission-scoped staff replies. Test-only captured email demonstrates the verification flow; it does not prove genuine SMTP delivery.

## Inactive integrations

These defaults are false until the owner approves the actual process:

| Configuration | Activation evidence |
| --- | --- |
| `Authentication:EmailChangeEnabled` | Approved sender and verified sandbox delivery to old/new addresses; durable protected Identity keys |
| `CustomerRequests:QuotesEnabled` | Staff responsibility for materials/quantity/artwork review and customer replies; genuine price/production approval |
| `Privacy:RequestsEnabled` | Qualified retention/deletion policy, responsible reviewer and approved response process |
| `Uploads:Scanner:Enabled` / `Required` | Approved private scanning adapter, confidentiality/retention review and clean/rejected/outage tests |

Quotes never create orders, take payment, invent prices or promise lead times. Deletion review never claims data has already been erased. The owner approves any later erasure/anonymization procedure against financial and audit retention requirements. New request records have owner authorization, pagination, bounded text/quantity, limited open requests, concurrency checks and audit events. Privacy review requires the sensitive Super Admin policy; quote review uses ManageOrders.

Email changes require password and the current factor where enabled, then a 30-minute Identity token delivered to the new address. Tokens are pasted into Account security instead of being put in browser URLs. Confirmation updates the verified address and username transactionally and revokes prior sessions. Duplicate-address requests use the same generic response. Data export requires reauthentication, excludes authentication secrets/internal staff notes and remains an authenticated attachment with no-store headers. Large histories fail explicitly for assisted handling rather than silently truncating records.

`20261021230822_BoutiqueCustomerRequests` adds a new restricted-FK table. Its rollback refuses to discard any request records. Both new migrations were exercised only against disposable databases. Existing financial data and historical migrations are preserved. The current EF snapshot and reviewed compatibility fixture advance with the new migrations.

## Image processing and scanner contract

Allowlisted signatures, extensions, MIME types, actual bytes and canvas bounds are checked before storage. SkiaSharp fully decodes and re-encodes new uploads as PNG, normalizes all eight EXIF orientations, preserves pixel transparency and removes original metadata/active or trailing bytes. Decode concurrency and decoded/encoded sizes are bounded. Private artwork and proofs remain outside the public root and require owner or the full recent-MFA staff policy. Static hosting and uploads use the same configured physical public root; Production rejects roots that could expose application configuration or private storage.

The optional scanner receives raw bytes at the approved HTTPS endpoint as `application/octet-stream`. A 204 response means clean; 422 means rejected. Other statuses, redirects, timeouts and network errors fail closed. Required-but-disabled/missing scanning cannot silently accept files. `Uploads:Scanner:ApiKey`, if needed, is supplied privately at runtime in X-Scan-Key, never in Vite or a URL. Only loopback development endpoints may use HTTP. Default configuration makes no scanner network call. Adapter tests are not evidence that an external antivirus service has been deployed.

## Offline compromised-password corpus

The small built-in/common-password list remains a baseline. An operator can prepare an approved UTF-8 public compromised-password list with `python scripts/build-password-blocklist.py APPROVED_WORDLIST NEW_PRIVATE_VERSION_DIRECTORY`. Never use customer passwords as input. The importer writes NFC-normalized, sorted SHA-256 buckets with checksums; it does not copy plaintext. It rejects an empty eligible corpus and refuses overwriting a version. Point `Authentication:PasswordBlocklistPath` at that private version and set `Authentication:RequirePasswordBlocklist=true` only after verifying it. A missing/corrupted configured corpus rejects new credential changes; existing hashes and sign-in continue unchanged. No password or hash is sent to a third party. Selecting, licensing and refreshing the real corpus remains an operator responsibility.
