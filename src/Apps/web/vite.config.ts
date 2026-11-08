import { fileURLToPath, URL } from 'node:url'
import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vitest/config'

// Endereço da API injetado pelo Aspire (WithReference); o fallback permite rodar o front sozinho
const api = process.env.services__api__https__0 ?? process.env.services__api__http__0 ?? 'https://localhost:7253'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) },
  },
  server: {
    // Mesma origem para o navegador: sem CORS. A API usa o certificado de desenvolvimento do .NET,
    // que o proxy do Vite não conhece (secure: false)
    proxy: {
      '/api': { target: api, changeOrigin: true, secure: false },
    },
  },
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: './src/test/setup.ts',
    css: false,
  },
})
