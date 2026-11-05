# Amaara Creations frontend

React 19, Vite, React Router, Axios, Vitest and Playwright. Use Node 22+.

```powershell
npm ci
npm run dev
```

The default development base is `/Amaara-Creations/`. `npm run dev` starts both the storefront on port 5179 and the staff workspace on 5173. Ctrl+C stops both, and a startup failure closes both. `npm run dev:all` is an alias; `dev:storefront` and `dev:admin` are available for explicit separate processes. Staff complete password entry, authenticator verification and any first-time QR setup on 5179. Only completed authentication redirects to the admin dashboard on 5173. If the local staff app is stopped, the completed sign-in offers a retry without repeating credentials or MFA. Start the API separately on port 5192. See the [repository README](../README.md) for public configuration and deployment.

[Frontend architecture](../docs/architecture/frontend.md) documents ownership, routing/providers, API interfaces, styling and contribution examples. [Admin architecture](../docs/architecture/admin.md) describes operational workflows.

```powershell
npm run lint
npm run typecheck
npm test -- --run
npm run test:dev
npm run test:e2e:dev
npm run build
npm run test:e2e
npm run test:e2e:production
npm run check:performance
```

Browser tests start the disposable acceptance API/frontend on ports 5198/5178. Production-browser checks test the built application without deploying it.
