/**
 * Runtime configuration.
 *
 * WHY NOT import.meta.env
 * -----------------------
 * Vite substitutes `import.meta.env.VITE_*` during `npm run build` — the literal
 * string is welded into the minified bundle. Once the Docker image is built the
 * API URL would be frozen, and pointing the same image at a different backend
 * would mean rebuilding it.
 *
 * Instead `/env.js` sets `window.__env` and is loaded by index.html BEFORE the
 * bundle. It is written at start-up, not at build time:
 *   local     — the npm "config" script (runs via predev/prebuild/prestart)
 *   container — /docker-entrypoint.d/40-env.sh, on every container start
 *
 * Only PUBLIC values belong here. env.js is downloaded by every visitor's browser.
 */

declare global {
  interface Window {
    __env?: Record<string, string | undefined>;
  }
}

/**
 * Reads a required runtime value, failing loudly rather than silently falling
 * back to a wrong default — a missing API URL should be an obvious error at
 * start-up, not a confusing network failure later.
 */
function required(name: string): string {
  const value = window.__env?.[name];

  if (!value || value.startsWith("${")) {
    throw new Error(
      `Missing runtime configuration "${name}".\n\n` +
        `window.__env is populated by /env.js, which is generated from the ` +
        `repo-root .env file and is not committed.\n\n` +
        `Local: run "npm run config" (or just "npm run dev", which does it for you) ` +
        `after copying .env.example to .env at the repo root.\n` +
        `Docker: ensure ${name} is set in the entrio-web service's environment block.`,
    );
  }

  return value;
}

export const env = {
  /** Base URL of the API, including the /api prefix. Must be reachable from the BROWSER. */
  apiBaseUrl: required("API_BASE_URL"),
};
