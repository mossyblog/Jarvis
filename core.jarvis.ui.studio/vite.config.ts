import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'path'
// import { execSync } from 'child_process'

// Function to get Windows host IP dynamically (currently unused)
// function getWindowsHostIP(): string {
//   try {
//     // Try to get Windows host IP from WSL
//     const result = execSync('ip route show | grep default | awk \'{print $3}\'', { 
//       encoding: 'utf8' 
//     }).trim();
//     
//     if (result && result.match(/^\d+\.\d+\.\d+\.\d+$/)) {
//       console.log(`Detected Windows host IP: ${result}`);
//       return result;
//     }
//   } catch {
//     console.warn('Could not detect Windows host IP, falling back to localhost');
//   }
//   
//   return 'localhost';
// }

// const windowsHostIP = getWindowsHostIP(); // TODO: Use for dev server configuration

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  server: {
    host: '0.0.0.0',
    hmr: {
      host: '0.0.0.0',
      port: 5173,
    },
    watch: {
      // Use polling for file system watching in WSL2
      usePolling: true,
      interval: 1000, // Check every second
    },
    proxy: {
      '/api': {
        target: 'http://172.27.112.1:7071',
        changeOrigin: true,
        secure: false,
      }
    }
  }
})
