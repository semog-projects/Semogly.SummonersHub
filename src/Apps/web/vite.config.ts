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
    // Porta fixa: o redirect URI do Google (http://localhost:5173/api/auth/signin-google) precisa ser exato
    port: 5173,
    strictPort: true,
    // Mesma origem para o navegador: sem CORS. A API usa o certificado de desenvolvimento do .NET,
    // que o proxy do Vite não conhece (secure: false)
    proxy: {
      // xfwd: repassa a origem do navegador (X-Forwarded-Host/Proto) para o redirect do login com Google
      '/api': { target: api, changeOrigin: true, secure: false, xfwd: true },
    },
  },
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: './src/test/setup.ts',
    css: false,
  },
})
