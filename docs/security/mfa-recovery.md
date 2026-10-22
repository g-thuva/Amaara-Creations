# Authenticator recovery

Forgotten passwords use the existing verified reset process. Resetting a staff password revokes prior sessions and preserves the authenticator requirement, key and memberships. A reset link is never evidence of completed MFA.

## Saved recovery code

Sign in with email/password, choose **Use a recovery code**, and enter one unused saved code. Each code works once. Staff recovery authorizes a replacement setup, not operations: scan the new QR, verify its current TOTP, save the replacement recovery codes and continue. The old authenticator stays stored until the replacement is verified; confirmation rotates the stamp, revokes all prior refresh sessions, replaces the recovery set and sends a generic security notification. Already-used codes and old TOTP values cannot complete the new transaction.

An existing customer who explicitly chose optional MFA may recover shopping access with a saved code. This never creates management assurance or staff permissions.

## Employee has no saved code

Another authorized `SuperAdmin` must verify the employee's identity through the organization's established independent contact/identity procedure. Record the verification reference and reason without recording secrets. Reauthenticate with password plus current authenticator code, then use the recovery form under **Staff permissions** and confirm **Approve authenticator recovery** within five minutes. Ordinary Admin/specialist accounts and self-approval are denied.

The protected `POST /api/v1/admin/staff/mfa-recovery` requires current owner permission, a management session, recent verified MFA and a 10–300 character reason. It rotates the affected employee's security stamp, revokes sessions, cancels pending sign-in and records `StaffMfaRecoveryApproved` with actor, target and expiry. A generic notification is sent; the owner's interface never receives the employee's TOTP key or recovery codes.

The employee must sign in with their password within fifteen minutes, complete replacement QR verification and save new recovery codes before any management session is issued. The pending enrollment has its own ten-minute maximum. Expired approval fails closed and requires another verified approval; it cannot restore use of the old authenticator. After replacement, review `AuthenticatorReplaced` and session revocation. Monitor `AuthenticatorNotificationDeferred` if delivery failed.

## Changing a working authenticator

Account security requires your password and existing factor (or a verified recovery path) before preparing a replacement. Confirm the replacement before discarding the old app entry. Successful confirmation revokes sessions, rotates the stamp and notifies the account. Staff cannot disable mandatory MFA. The shared recovery/QR components avoid exposing setup information outside the limited transaction.

## Owner break-glass procedure

Prefer a privately stored unused recovery code or a second independently verified owner using the procedure above. Keep at least two appropriately governed owners and store recovery material separately from their phones.

If every owner has lost both factors and recovery material, there is deliberately no universal emergency code or web endpoint that disables MFA. Stop privileged operations, preserve audit evidence and obtain documented owner/security approval for an operator recovery under dual control. Verify identity out of band, preserve the SQL database and Data Protection key ring through the approved backup process, and review a narrowly scoped Identity-based recovery operation before execution. It must revoke affected sessions, rotate the stamp, require replacement enrollment, record the approving operators and notify the owner. Rehearse it on a disposable database before an approved production operation. The existing development role-assignment CLI is not an MFA recovery bypass.

No such operator reset, real-account approval, business-database copy or migration was performed in this task. Do not delete Identity token rows or toggle `TwoFactorEnabled` with ad hoc SQL.
