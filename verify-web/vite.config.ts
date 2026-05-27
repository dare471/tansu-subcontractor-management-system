import { defineConfig, loadEnv } from 'vite';
import vue from '@vitejs/plugin-vue';
import basicSsl from '@vitejs/plugin-basic-ssl';

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');
  const proxyTarget = env.VITE_VERIFY_API_PROXY_TARGET || 'http://localhost:8091';

  return {
    plugins: [vue(), basicSsl()],
    server: {
      port: 5174,
      host: true,
      // HTTPS нужен для getUserMedia с телефона/планшета по LAN (не localhost).
      proxy: {
        '/api': {
          target: proxyTarget,
          changeOrigin: true
        }
      }
    }
  };
});
