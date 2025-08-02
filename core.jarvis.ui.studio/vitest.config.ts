/// <reference types="vitest" />
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'path'

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    css: true,
    include: ['src/**/*.{test,spec}.{js,mjs,cjs,ts,mts,cts,jsx,tsx}'],
    exclude: [
      '**/node_modules/**',
      '**/dist/**',
      '**/cypress/**',
      '**/.{idea,git,cache,output,temp}/**',
      '**/storybook-static/**',
      '**/*.d.ts'
    ],
    coverage: {
      provider: 'v8',
      reporter: ['text', 'json', 'html', 'cobertura', 'json-summary'],
      exclude: [
        'coverage/**',
        'dist/**',
        'packages/*/test{,s}/**',
        '**/*.d.ts',
        'cypress/**',
        'test{,s}/**',
        'test{,-*}.{js,cjs,mjs,ts,tsx,jsx}',
        '**/*{.,-}test.{js,cjs,mjs,ts,tsx,jsx}',
        '**/*{.,-}spec.{js,cjs,mjs,ts,tsx,jsx}',
        '**/__tests__/**',
        '**/{karma,rollup,webpack,vite,vitest,jest,ava,babel,nyc,cypress,tsup,build}.config.*',
        '**/.{eslint,mocha,prettier}rc.{js,cjs,yml}',
        '**/src/test/**',
        '**/src/**/*.stories.{js,ts,jsx,tsx}',
        '**/*.config.{js,ts}',
        '**/tailwind.config.js',
        '**/postcss.config.js'
      ],
      thresholds: {
        global: {
          branches: 80,
          functions: 80,
          lines: 80,
          statements: 80
        },
        // Per-file thresholds for critical components
        perFile: true
      },
      // Generate coverage reports in CI-friendly formats
      reportsDirectory: './coverage',
      all: true,
      clean: true,
      // Include source maps for better debugging
      sourcemap: true
    },
    // Mock browser APIs that aren't available in jsdom
    pool: 'forks',
    poolOptions: {
      forks: {
        singleFork: true
      }
    },
    // Reporter configuration for CI/CD
    reporters: [
      'default',
      'json',
      'junit'
    ],
    outputFile: {
      json: './test-results/unit-test-results.json',
      junit: './test-results/junit.xml'
    },
    // Test timeout configuration
    testTimeout: 10000,
    hookTimeout: 10000,
    // Watch mode configuration
    watch: false
  }
})