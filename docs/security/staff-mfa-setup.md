# Staff authenticator setup

Open the usual Amaara Creations sign-in page. Enter your existing email and password once. The server checks your assigned permissions; there is no customer/staff selector.

- Customers normally go straight to their account.
- Returning staff enter the current six-digit code from their authenticator and reach their permitted workspace.
- New staff see **Secure your Amaara account** and complete the setup below.

## Where the code comes from

The code comes from the authenticator app on your phone. It is not your password, an email code or a Google sign-in code. After setup, open the **Amaara Creations** entry in the app whenever Amaara asks for verification.

1. Install Google Authenticator from [Google Play](https://play.google.com/store/apps/details?id=com.google.android.apps.authenticator2) or the [Apple App Store](https://apps.apple.com/app/google-authenticator/id388497605). Skip installation if you already use it. Another compatible TOTP app also works; Google account linking/cloud sync is not required by Amaara.
2. In the app, tap **+**, choose **Scan a QR code**, and scan the QR displayed after Amaara verifies your password. If scanning is unavailable, expand **Can't scan? Enter the setup key manually**, copy the private key and add a time-based account. On the same phone, the optional authenticator-app link may open a compatible installed app; manual setup is the fallback.
3. Enter the app's six-digit Amaara code and choose **Verify and continue**. Typing and pasting are supported. The code changes approximately every 30 seconds. If it fails, check the account and automatic phone time, then try the current code. Refreshing an unexpired attempt preserves the QR.
4. Download, copy or print the ten recovery codes. Store them privately, preferably in a password manager or offline. Check **I have saved my recovery codes somewhere safe**, then continue directly to your workspace. You do not enter your password or another code again.

Setup expires after ten minutes. If it expires, sign in again to start a valid attempt. After verification, the recovery panel is available for five minutes. Ordinary future sign-ins skip QR setup and recovery-code saving. Never share setup keys, QR screenshots, passwords or recovery codes in support messages.

If you already enrolled this account but cannot find its authenticator entry, use a saved recovery code or the [verified recovery process](mfa-recovery.md). Merely reinstalling the app does not recreate an existing account's secret.

## Local development

Use Node 22+, the repository's .NET 8 SDK and existing private backend configuration. Start the API with `dotnet run --project be` (configured HTTP port 5192), the storefront with `npm --prefix client run dev` (5173), and management with `npm --prefix client run dev:admin` (5179). Open `http://localhost:5173/Amaara-Creations/login`; management entry returns here when sign-in is needed. Keep the same loopback hostname for all three.

Administration must be deliberately enabled in private Development configuration. No account is promoted or enrolled by starting the applications. The configured local administrative identity was read-only verified as email-confirmed with `Admin` and `SuperAdmin` membership and no enrolled MFA; its owner must perform the real phone setup privately. No real account credentials were submitted during automated verification.

Authentication reuses existing tables. The owner requested leaving the database unchanged, so the two pending commerce/request migrations remain unapplied. These are separate from MFA; do not run database updates or the previously prepared backup helper as an authentication fix. Full commerce readiness still requires the separately reviewed migration process.
