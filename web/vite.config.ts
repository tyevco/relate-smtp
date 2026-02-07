import tailwindcss from "@tailwindcss/vite";
import { devtools } from "@tanstack/devtools-vite";
import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';
import { tanstackRouter } from "@tanstack/router-plugin/vite"
import path from 'path';

export default defineConfig(({ mode }) => {
  // Load env file based on `mode` in the current working directory.
  const env = loadEnv(mode, process.cwd(), '');
  const proxyTarget = env.VITE_API_PROXY_URL || 'http://localhost:5000';
  return {
    plugins: [
      devtools({
        eventBusConfig: {
          port: 52522,
        },
      }),
      tanstackRouter(),
      tailwindcss(),
      react()],
    resolve: {
      alias: {
        '@': path.resolve(__dirname, './src'),
      },
    },
    server: {
      port: 5492,
      proxy: {
        // Proxy API requests to avoid CORS issues in development
        '/api': {
          target: proxyTarget,
          changeOrigin: true,
          // In development, use NODE_TLS_REJECT_UNAUTHORIZED=0 if using self-signed certs
          configure: (proxy, _options) => {
            proxy.on('error', (err, _req, _res) => {
              console.log('❌ Proxy error:', err.message);
            });
            proxy.on('proxyReq', (proxyReq, req, _res) => {
              console.log('→ Proxying:', req.method, req.url, '→', proxyReq.path);
            });
          },
        },
        // Proxy config.json to backend API for dynamic runtime config
        '/config/config.json': {
          target: proxyTarget,
          changeOrigin: true,
          rewrite: (path) => path.replace(/^\/config/, ''),
        },
      },
    },
  }
})
