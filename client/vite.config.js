import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { fileURLToPath, URL } from 'node:url'
import process from 'node:process'

// The staff interface uses the bundled Inter font and must work without a
// third-party font stylesheet. The storefront families are also bundled locally.
const staffEntryHtml = html => html
  .replace('/src/main.jsx', '/src/admin-main.jsx')
  .replace('/fonts/plus-jakarta-sans-normal-latin.woff2', '/fonts/inter-latin.woff2')
  .replace(/<link\b[^>]*href=["'][^"']*\/fonts\/playfair-display-normal-latin\.woff2["'][^>]*>/g, '')
  .replace('<title>Amaara Creations</title>', '<title>Amaara studio operations</title><meta name="robots" content="noindex,nofollow,noarchive" />')
  .replace(/<link\b[^>]*href=["']https:\/\/fonts\.(?:googleapis|gstatic)\.com[^"']*["'][^>]*>/g, '')

export default defineConfig(({ mode }) => {
  return {
  plugins: [react(), ...(mode === 'admin' ? [{ name: 'staff-entry', transformIndexHtml: { order: 'pre', handler: staffEntryHtml } }] : [])],
  test: { include: ['src/**/*.test.{js,jsx,ts,tsx}'] },
  base: process.env.VITE_PUBLIC_BASE_PATH || '/Amaara-Creations/',
  cacheDir: mode === 'admin' ? 'node_modules/.vite-admin' : 'node_modules/.vite',
  build: {
    outDir: mode === 'admin' ? 'dist-admin' : 'dist',
    sourcemap: false,
    manifest: true,
    rollupOptions: {
      treeshake: false,
    },
  },
  resolve: {
    alias: [
      {
        find: '@',
        replacement: fileURLToPath(new URL('./src', import.meta.url)),
      },
      {
        find: /^axios$/,
        replacement: fileURLToPath(new URL('./node_modules/axios/dist/esm/axios.min.js', import.meta.url)),
      },
      {
        find: /^react-router$/,
        replacement: fileURLToPath(new URL('./node_modules/react-router/dist/production/index.mjs', import.meta.url)),
      },
      {
        find: /^react-router\/dom$/,
        replacement: fileURLToPath(new URL('./node_modules/react-router/dist/production/dom-export.mjs', import.meta.url)),
      },
    ],
  },
  }
})
