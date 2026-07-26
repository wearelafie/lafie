/// <reference types="vitest/config" />
import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { VitePWA } from 'vite-plugin-pwa'
import Icons from 'unplugin-icons/vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    react(),
    // Icônes Iconify (fluent-color) importées offline + tree-shakées via ~icons/*.
    Icons({ compiler: 'jsx', jsx: 'react' }),
    VitePWA({
      registerType: 'autoUpdate',
      // PWA : app installable + cache des assets (shell offline).
      // NB : l'offline des DONNÉES cliniques (cache API + sync) est un chantier séparé.
      manifest: {
        name: 'Lafie Navigator',
        short_name: 'Lafie',
        description: 'Plateforme HIS/EMR',
        theme_color: '#0f6cbd',
        background_color: '#ffffff',
        display: 'standalone',
      },
    }),
  ],
  resolve: {
    alias: {
      '@app': fileURLToPath(new URL('./src/app', import.meta.url)),
      '@features': fileURLToPath(new URL('./src/features', import.meta.url)),
      '@shared': fileURLToPath(new URL('./src/shared', import.meta.url)),
    },
  },
  server: {
    port: 3000,
    // Dev : proxy /api -> backend (Docker api exposée sur l'hôte 8081).
    proxy: {
      '/api': {
        target: 'http://localhost:8081',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/api/, ''),
      },
    },
  },
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: './src/test/setup.ts',
    css: true,
    // Vitest = tests unitaires/composant sous src/ ; e2e/ est réservé à Playwright.
    include: ['src/**/*.{test,spec}.{ts,tsx}'],
    exclude: ['e2e/**', 'node_modules/**', 'dist/**'],
  },
})
