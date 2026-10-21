# Threat model — boutique redesign

Trust boundaries: untrusted browser → .NET API → Identity/EF Core/SQL Server; external payment webhook → verifier → transaction; authenticated upload → private storage → authorized file response; staff browser → independent host/gateway → recent MFA → per-function policy.

| STRIDE | Concrete threat | Control / verification |
| --- | --- | --- |
| Spoofing | Edited JWT/role state, anonymous staff API, stolen/revoked access token | Signature/issuer/audience/lifetime/algorithm validation, current database roles, stamp binding, recent second factor; direct HTTP negative tests |
| Tampering | Client price/coupon/quantity, overposted role, reordered production state | Typed DTOs, backend quote/order reconciliation, role-specific policies, state machine and concurrency tests |
| Repudiation | Staff refunds or role edits without trace | Existing append-only audit service, redaction and SQL append-only boundary; no ordinary API to mutate evidence |
| Information disclosure | Cross-user orders/artwork/proofs, public staff chunks, public upload paths | Existing owner-scoped queries/private storage; separate staff build and protected-host proposal; ID substitution tests |
| Denial of service | Upload bombs, repeated sign-in/TOTP, expensive queries | Existing upload/quote/auth limiters, bounded image decoding and pagination; add failed-factor account lockout |
| Elevation | Signup role fields, stale elevated JWT, public staff route | Customer-only registration, database role refresh, recent-MFA policy, default-disabled administration, independent build |

Residual risks: compromised-password corpus coverage, TOTP replay/phishing resistance, privileged host gateway, backup encryption/restore, real payment sandbox, CSP enforced by static host, manual accessibility and current legal duties require explicit evidence. No production readiness claim follows from in-memory tests.
