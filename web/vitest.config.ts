import { defineConfig, mergeConfig } from 'vitest/config'
import viteConfig from './vite.config'

export default mergeConfig(
  viteConfig({ mode: 'test', command: 'serve' }),
  defineConfig({
    test: {
      globals: true,
      environment: 'happy-dom',
      setupFiles: ['./src/test/setup.ts'],
      include: ['src/**/*.{test,spec}.{ts,tsx}'],
      exclude: ['node_modules', 'dist', 'e2e'],
      coverage: {
        provider: 'v8',
        reporter: ['text', 'json', 'html', 'lcov'],
        exclude: [
          'node_modules/',
          'src/test/',
          '**/*.d.ts',
          '**/*.config.*',
          '**/routeTree.gen.ts',
          'src/main.tsx',
          'src/vite-env.d.ts',
        ],
        thresholds: {
          statements: 50,
          branches: 45,
          functions: 50,
          lines: 50,
        },
      },
      testTimeout: 10000,
      hookTimeout: 10000,
    },
  })
)
