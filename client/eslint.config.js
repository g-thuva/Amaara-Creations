import js from "@eslint/js";
import globals from "globals";
import reactHooks from "eslint-plugin-react-hooks";
import reactRefresh from "eslint-plugin-react-refresh";
import { defineConfig, globalIgnores } from "eslint/config";

export default defineConfig([
  globalIgnores(["dist"]),
  {
    files: ["**/*.{js,jsx}"],
    extends: [
      js.configs.recommended,
      reactHooks.configs["recommended-latest"],
      reactRefresh.configs.vite,
    ],
    languageOptions: {
      ecmaVersion: 2020,
      globals: globals.browser,
      parserOptions: {
        ecmaVersion: "latest",
        ecmaFeatures: { jsx: true },
        sourceType: "module",
      },
    },
    rules: {
      "no-unused-vars": ["error", { varsIgnorePattern: "^[A-Z_]" }],
    },
  },
  {
    files: ["src/features/**/*.{js,jsx}"],
    ignores: ["src/features/**/*Api.js", "src/features/**/*.test.{js,jsx}"],
    rules: {
      "no-restricted-imports": [
        "error",
        {
          patterns: [
            {
              group: ["axios", "@/shared/api/httpClient", "@/shared/api/httpClient.js", "**/shared/api/httpClient.js"],
              message:
                "Use a named feature API operation; feature API modules own HTTP endpoints.",
            },
          ],
        },
      ],
    },
  },
  {
    files: ["src/features/admin/shared/**/*.{js,jsx}"],
    rules: {
      "no-restricted-imports": [
        "error",
        {
          patterns: [
            {
              group: [
                "@/features/admin/features/**",
                "**/pages/**",
                "**/app/**",
                "**/layout/**",
                "axios",
                "@/shared/api/httpClient.js",
              ],
              message:
                "Admin shared modules must remain domain-neutral and must not own feature requests.",
            },
          ],
        },
      ],
    },
  },
  {
    files: ["src/shared/**/*.{js,jsx}"],
    ignores: ["src/shared/**/*.test.{js,jsx}"],
    rules: {
      "no-restricted-imports": ["error", {
        patterns: [{ group: ["@/features/**", "@/app/**"], message: "Shared infrastructure and UI must remain independent of application features." }],
      }],
    },
  },
]);
