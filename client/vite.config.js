import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { fileURLToPath, URL } from 'node:url'

export default defineConfig({
  plugins: [react()],
  test: { include: ['src/**/*.test.{js,jsx,ts,tsx}'] },
  base: '/Amaara-Creations/',
  build: {
    rollupOptions: {
      treeshake: false,
    },
  },
  resolve: {
    alias: [
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
})
