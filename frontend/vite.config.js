import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), 'VITE_')
  const allowedHost = normalizeAllowedHost(env.VITE_ALLOWED_HOST)
  const allowedHosts = allowedHost ? [allowedHost] : null
  const proxy = {
    '/api': {
      target: 'http://localhost:5166',
      changeOrigin: true,
    },
  }

  return {
    plugins: [
      react(),
      tailwindcss(),
    ],
    server: {
      ...(allowedHosts ? { allowedHosts } : {}),
      proxy,
    },
    preview: {
      ...(allowedHosts ? { allowedHosts } : {}),
      proxy,
    },
  }
})

function normalizeAllowedHost(value) {
  const host = value?.trim().toLowerCase()
  if (!host) return null

  try {
    const parsed = new URL(`http://${host}`)
    const isHostnameOnly = parsed.hostname === host
      && parsed.host === host
      && parsed.pathname === '/'
      && !parsed.username
      && !parsed.password
      && !parsed.search
      && !parsed.hash

    return isHostnameOnly ? host : null
  } catch {
    return null
  }
}
