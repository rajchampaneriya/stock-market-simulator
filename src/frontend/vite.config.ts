import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': {
        // VITE_BROKER_URL is set by Aspire via .WithEnvironment() in AppHost.cs
        // services__broker-service__* vars are set via .WithReference() in AppHost.cs
        target: process.env['VITE_BROKER_URL'] ||
                process.env['services__broker-service__https__0'] || 
                process.env['services__broker-service__http__0'] || 
                'http://localhost:5046', // Fallback to broker service default http port
        changeOrigin: true,
        secure: false, // Allow self-signed certificates in development
        rewrite: (path) => path, // Keep /api prefix
      }
    }
  }
})
