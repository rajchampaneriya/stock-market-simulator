import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': {
        target: process.env.services__broker_service__http__0 || 'http://localhost:5002',
        changeOrigin: true,
        secure: false,
      }
    }
  }
})
