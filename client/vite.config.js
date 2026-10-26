import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { fileURLToPath, URL } from 'node:url'
import process from 'node:process'

export default defineConfig(({ mode }) => {
  return {
  plugins: [react(), ...(mode === 'admin' ? [{ name: 'staff-entry', transformIndexHtml: { order: 'pre', handler: html => html.replace('/src/main.jsx', '/src/admin-main.jsx').replace('<title>Amaara Creations</title>', '<title>Amaara studio operations</title><meta name="robots" content="noindex,nofollow,noarchive" />') } }] : [])],
  test: { include: ['src/**/*.test.{js,jsx,ts,tsx}'] },
  base: process.env.VITE_PUBLIC_BASE_PATH || '/Amaara-Creations/',
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
