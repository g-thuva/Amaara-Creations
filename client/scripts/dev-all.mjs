import process from 'node:process';
import { createServer } from 'vite';

const storefront = await createServer({
  server: { port: 5179, strictPort: true },
});
await storefront.listen();
console.log('\n[Storefront] Running on http://localhost:5179/Amaara-Creations/');

const admin = await createServer({
  mode: 'admin',
  server: { port: 5173, strictPort: true },
});
await admin.listen();
console.log('[Admin Portal] Running on http://localhost:5173/Amaara-Creations/\n');

const closeAll = async () => {
  await storefront.close();
  await admin.close();
  process.exit(0);
};

process.on('SIGINT', closeAll);
process.on('SIGTERM', closeAll);
