import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// En modo desarrollo (iniciar --dev) Vite reenvía /api a la API local, igual que nginx en los
// contenedores: el frontend siempre llama a "/api" y no hace falta CORS.
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: process.env.API_PROXY_TARGET ?? 'http://localhost:5255',
        changeOrigin: true,
        ws: true,
      },
    },
  },
})
