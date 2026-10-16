# Amaara Creations frontend

React 19, Vite, React Router, Axios, Vitest and Playwright. Use Node 22+.

```powershell
npm ci
npm run dev
```

The default development base is `/Amaara-Creations/`. See the [repository README](../README.md) for public configuration and deployment.

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
