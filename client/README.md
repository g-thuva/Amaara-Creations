# Amaara Creations frontend

React 19, Vite, React Router, Axios, Vitest and Playwright. Use Node 22+.

```powershell
npm ci
npm run dev
```

The default development base is `/Amaara-Creations/`. `npm run dev` starts the storefront on port 5179; run `npm run dev:admin` in another terminal for admin on 5173. Alternatively, run `npm run dev:all` to run both simultaneously in a single terminal. Staff complete password entry, authenticator verification and any first-time QR setup on 5179. Only completed authentication redirects to the admin dashboard on 5173. Start the API separately on port 5192. See the [repository README](../README.md) for public configuration and deployment.

[Frontend architecture](../docs/architecture/frontend.md) documents ownership, routing/providers, API interfaces, styling and contribution examples. [Admin architecture](../docs/architecture/admin.md) describes operational workflows.

```powershell
npm run lint
npm run typecheck
npm test -- --run
npm run build
npm run test:e2e
npm run test:e2e:production
npm run check:performance
```

Browser tests start the disposable acceptance API/frontend on ports 5198/5178. Production-browser checks test the built application without deploying it.
