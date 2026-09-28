/**
 * Writes public/env.js from the repo-root .env, for LOCAL runs.
 *
 * The container does not use this script — nginx's /docker-entrypoint.d/40-env.sh
 * writes the same file with envsubst from the container environment. Both produce
 * the identical shape, so the app cannot tell the two apart.
 *
 * Uses Node's built-in process.loadEnvFile (Node 20.12+), so no dependency.
 */
import { writeFileSync, existsSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const frontendRoot = resolve(here, "..");

// Walk up to the repo root to find the single shared .env.
function findEnvFile(startDir) {
  let dir = startDir;

  for (;;) {
    const candidate = join(dir, ".env");
    if (existsSync(candidate)) return candidate;

    const parent = dirname(dir);
    if (parent === dir) return null;
    dir = parent;
  }
}

// Only PUBLIC values. Everything listed here is served to every visitor's browser.
const PUBLIC_KEYS = ["API_BASE_URL"];
const OPTIONAL_PUBLIC_KEYS = ["DEMO_MODE"];

// `npm run dev:demo` passes --demo so demo mode can be tried locally without editing
// .env. Done as a flag rather than an inline env var because `DEMO_MODE=true npm run
// dev` is Bash-only syntax and fails in PowerShell.
const forceDemo = process.argv.includes("--demo");

const envFile = findEnvFile(frontendRoot);

if (envFile) {
  process.loadEnvFile(envFile);
} else {
  console.warn(
    "[generate-env] No .env found above this folder. " +
      "Copy .env.example to .env at the repo root.",
  );
}

const missing = PUBLIC_KEYS.filter((key) => !process.env[key]);

if (missing.length > 0) {
  console.error(
    `[generate-env] Missing required value(s): ${missing.join(", ")}.\n` +
      `Add them to ${envFile ?? "the repo-root .env"} (see .env.example).`,
  );
  process.exit(1);
}

const values = Object.fromEntries(
  [...PUBLIC_KEYS, ...OPTIONAL_PUBLIC_KEYS]
    .filter((key) => process.env[key] !== undefined)
    .map((key) => [key, process.env[key]]),
);

if (forceDemo) {
  values.DEMO_MODE = "true";
}

const contents =
  "// GENERATED FILE — do not edit and do not commit.\n" +
  "// Written by scripts/generate-env.mjs from the repo-root .env.\n" +
  `window.__env = ${JSON.stringify(values, null, 2)};\n`;

const outputPath = join(frontendRoot, "public", "env.js");
writeFileSync(outputPath, contents, "utf8");

console.log(`[generate-env] Wrote public/env.js (${Object.keys(values).join(", ")}).`);
