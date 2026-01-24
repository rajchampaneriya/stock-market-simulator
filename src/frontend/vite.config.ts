import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': {
        // Aspire sets services__broker_service__https__0 or services__broker_service__http__0
        // Note: hyphens in service names become underscores in env vars
        target: process.env['services__broker-service__https__0'] || 
                process.env['services__broker-service__http__0'] || 
                'https://localhost:50723', // Current broker service port
        changeOrigin: true,
        secure: false, // Allow self-signed certificates in development
        rewrite: (path) => path, // Keep /api prefix
      }
    }
  }
})
