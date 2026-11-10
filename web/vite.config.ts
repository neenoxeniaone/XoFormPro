import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')

  return {
    // Deploy under a subpath by setting VITE_BASE_PATH (e.g. /FormPro). Empty = served at root.
    base: env.VITE_BASE_PATH ? `${env.VITE_BASE_PATH}/` : '/',
    plugins: [react(), tailwindcss()],
    server: {
      port: 5173,
      // Dev only: proxy API calls to the ASP.NET backend.
      // Every backend path needs a rule here. Anything missing falls through to the SPA
      // fallback and quietly returns index.html with a 200, which looks like a broken
      // endpoint rather than a missing proxy entry.
      proxy: {
        '/api': { target: 'http://localhost:5074', changeOrigin: true },
        '/submit': { target: 'http://localhost:5074', changeOrigin: true },
        '/v1': { target: 'http://localhost:5074', changeOrigin: true },
        // Read by forms.js on page load (tick box / captcha config + signed token).
        '/form-config': { target: 'http://localhost:5074', changeOrigin: true },
        '/form-token': { target: 'http://localhost:5074', changeOrigin: true },
      },
    },
  }
})
