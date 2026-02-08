import js from '@eslint/js';
import globals from 'globals';
import reactHooks from 'eslint-plugin-react-hooks';
import reactRefresh from 'eslint-plugin-react-refresh';
import tseslint from 'typescript-eslint';

export default tseslint.config(
  { ignores: ['dist', 'node_modules', '.tanstack'] },
  {
    extends: [js.configs.recommended, ...tseslint.configs.recommended],
    files: ['**/*.{ts,tsx}'],
    languageOptions: {
      ecmaVersion: 2020,
      globals: globals.browser,
    },
    plugins: {
      'react-hooks': reactHooks,
      'react-refresh': reactRefresh,
    },
    rules: {
      ...reactHooks.configs.recommended.rules,
      'react-refresh/only-export-components': [
        'warn',
        { allowConstantExport: true },
      ],
      // TypeScript rules - relaxed for existing codebase
      // TODO(tech-debt): Upgrade to 'error' after fixing violations
      '@typescript-eslint/no-unused-vars': ['warn', { argsIgnorePattern: '^_' }],
      '@typescript-eslint/no-explicit-any': 'warn',
      '@typescript-eslint/no-empty-object-type': 'off',
      // React hooks rules - set to 'warn' due to existing violations
      // These are important for correctness but require careful refactoring
      // TODO(tech-debt): Upgrade to 'error' after fixing hook dependency issues
      // See: https://react.dev/reference/rules/rules-of-hooks
      'react-hooks/exhaustive-deps': 'warn',
      'react-hooks/rules-of-hooks': 'error', // This one should always be error
      // setState in effect patterns need refactoring to derived state or useMemo
      // TODO(tech-debt): Refactor to avoid setState in useEffect
      'react-hooks/set-state-in-effect': 'warn',
    },
  },
);
