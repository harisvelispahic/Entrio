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

/** Reads an optional boolean flag. Absent or unsubstituted means false. */
function optionalFlag(name: string): boolean {
  const value = window.__env?.[name];

  if (!value || value.startsWith("${")) {
    return false;
  }

  return value.toLowerCase() === "true";
}

export const env = {
  /** Base URL of the API, including the /api prefix. Must be reachable from the BROWSER. */
  apiBaseUrl: required("API_BASE_URL"),

  /**
   * Runs the app against an in-memory simulation instead of a real API.
   *
   * Exists for the public Vercel deployment, which has no backend: without it a
   * visitor would meet a login form that can never succeed. Set to true ONLY there.
   * Because it is runtime config, it cannot leak into a local or Docker run, and the
   * UI states plainly that the data is simulated -- unlike the silent mock-data
   * fallback this replaces, which was indistinguishable from real data.
   */
  demoMode: optionalFlag("DEMO_MODE"),
};
