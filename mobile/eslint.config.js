// https://docs.expo.dev/guides/using-eslint/
const { defineConfig } = require('eslint/config');
const expoConfig = require("eslint-config-expo/flat");

module.exports = defineConfig([
  expoConfig,
  {
    ignores: ["dist/*"],
  },
  {
    files: ["**/*.{ts,tsx}"],
    rules: {
      // --- TypeScript strict rules ---
      "@typescript-eslint/no-unused-vars": ["error", {
        argsIgnorePattern: "^_",
        varsIgnorePattern: "^_",
        caughtErrorsIgnorePattern: "^_",
      }],
      "@typescript-eslint/no-explicit-any": "error",

      // --- Security rules ---
      "no-eval": "error",
      "no-implied-eval": "error",
      "no-new-func": "error",
      "no-script-url": "error",

      // --- Bug prevention ---
      "eqeqeq": ["error", "always"],
      "no-constructor-return": "error",
      "no-promise-executor-return": "error",
      "no-self-compare": "error",
      "no-template-curly-in-string": "warn",
      "no-unreachable-loop": "error",

      // --- Code quality ---
      "no-debugger": "error",
      "no-alert": "error",
      "no-console": ["warn", { allow: ["warn", "error"] }],
      "no-var": "error",
      "prefer-const": "error",
      "no-return-assign": "error",
    },
  },
]);
