import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import basicSsl from '@vitejs/plugin-basic-ssl'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    react({
      babel: {
        plugins: [['babel-plugin-react-compiler']],
      },
    }),
    basicSsl()
  ],
  server: {
    port: 3001,
    https: true,
    strictPort: true,
    host: 'localhost',
    hmr: {
      port: 3001,
      clientPort: 3001,
      protocol: 'wss'
    }
  }
})
