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
    files: ["src/admin/**/*.{js,jsx}"],
    ignores: ["src/admin/**/*Api.js", "src/admin/**/*.test.{js,jsx}"],
    rules: {
      "no-restricted-imports": [
        "error",
        {
          patterns: [
            {
              group: ["axios", "**/services/api", "**/services/api.js"],
              message:
                "Use a named feature API operation; feature API modules own HTTP endpoints.",
            },
          ],
        },
      ],
    },
  },
  {
    files: ["src/admin/shared/**/*.{js,jsx}"],
    rules: {
      "no-restricted-imports": [
        "error",
        {
          patterns: [
            {
              group: [
                "**/features/**",
                "**/pages/**",
                "**/app/**",
                "**/layout/**",
                "axios",
                "**/services/api",
                "**/services/api.js",
              ],
              message:
                "Admin shared modules must remain domain-neutral and must not own feature requests.",
            },
          ],
        },
      ],
    },
  },
]);
