# Entrio — notes for Claude

IoT smart garage door system: ASP.NET Core 8 API (clean architecture + MediatR),
React/Vite frontend, and ESP32 firmware. See `README.md` for how to run it.

## Configuration

**One rule: nothing infrastructural lives in a tracked file.** No connection
strings, no keys, no API secrets — not even empty placeholder keys. If you find
one, delete it rather than blanking it.

| File | Holds | Tracked |
|---|---|---|
| `.env` (repo root) | Secrets and connection strings for both run modes | no |
| `.env.example` | Template with placeholders and comments | yes |
| `IoT.API/appsettings.json` | Behaviour only: logging, `Cors:AllowedOrigins`, JWT issuer/audience | yes |
| `IoT.API/appsettings.Development.json` | The default `Logging` block only | yes |
| `Frontend.IoT/public/env.js` | Generated at start-up; sets `window.__env` | no |
| `Esp32.IoT/secrets.h` | WiFi credentials, server URL, device key | no |

The login account is **not** a secret and is **not** in `.env` — it is seeded from
`DatabaseSeeder.OwnerEmail`/`OwnerPassword` so anyone cloning the repo can log in.
`Login.tsx` shows the same values; keep the two in sync.

`.env` uses plain names (`JWT_KEY`); ASP.NET Core binds `Section__Key`
(`Jwt__Key`). Two bridges express the **same** mapping:

- **Local** — `DotNetEnv` loads `.env`, then
  `IoT.API/Configuration/EnvironmentConfiguration.cs` aliases plain names onto
  `Section__Key`. Both run before `CreateBuilder`. A missing `.env` is a silent
  no-op, which is the normal case inside a container.
- **Docker** — `docker-compose.yml` sets the `Section__Key` names directly.

> **Adding a secret means touching three places in the same change:**
> 1. `.env.example` — placeholder and comment
> 2. `EnvironmentConfiguration.Aliases` — plain name → `Section__Key`
> 3. `docker-compose.yml` — the same `Section__Key` in `environment:`
>
> Miss one and it works in exactly one of the two run modes.

Only alias values that are **identical** in both environments. Host-dependent
values (the connection string: compose service name vs `localhost,1437`) go at
the bottom of `.env` as explicit `Section__Key` lines, never aliased.

Frontend config is **runtime, not build-time**: never reintroduce
`import.meta.env.VITE_*` for the API URL — Vite freezes it into the bundle at
build time. Read `window.__env` via `src/config/env.ts`. Only public values may
go in `env.js`; the browser downloads it.

## Docker

- Pin every image to a concrete patch tag. Never `:latest`. Look tags up
  (`docker manifest inspect`, or the MCR tag list) rather than guessing.
- Healthchecks use tools already in each image: `sqlcmd` from
  `/opt/mssql-tools18`, busybox `wget --spider`, and a bash `/dev/tcp` port probe
  for the API. Do not add a `/health` endpoint and do not install `curl`.
- The API migrates and seeds **before** `app.Run()`, which is what makes the
  API's port probe also mean "database ready".
- Fixed host ports: API **5263** (hardcoded in the firmware and a CORS origin),
  web **4300**, SQL **1437**. Do not change them casually.

## Errors

Throw `EntrioException` subclasses (`UnauthorizedException`, `NotFoundException`,
`BusinessRuleException`) for anything a client should see. The handler chain in
`IoT.API/Middleware` maps them to a status code and a consistent JSON body. Anything
else becomes a logged 500 with a `traceId` and no internal detail — never return a
raw exception. Do not use `return BadRequest(...)`/`Problem(...)` in new code.

## Things that are load-bearing

- **Device GUID `0f8fad5b-d9cb-469f-a165-70867728950e`** is hardcoded in the
  firmware's status payload and `DatabaseSeeder.FirmwareDeviceId`. Both must agree
  or status posts fail. The frontend no longer knows it.
- **Seeding is the only way rows get created.** There is no `HasData` anywhere; a
  fresh database with no seed means no login and 401 on every device call.
  `DatabaseSeeder` is idempotent and skips existing rows.
- **The auto-close condition in `CreateDeviceEventCommandHandler`** treats a last
  command of `Close` as non-suppressing. That clause was added deliberately in
  `62c035d` for RFID opens, which create no command row. Do not "simplify" it away.
- **`import.meta.env` must never return for the API URL** — see the config section.
- **Timestamps** rely on `UtcDateTimeConverter`; without it SQL returns
  `DateTimeKind.Unspecified`, the `Z` is dropped, and the browser reads UTC as local.
  The converter RELABELS Unspecified as UTC and never shifts it — do not reintroduce
  `ToUniversalTime()` on the read path, which interprets Unspecified as server-local
  and made the same request mean different instants in Docker (UTC) and locally (UTC+2).
  The API never converts between zones; the browser owns all local-time work, which is
  also what makes DST correct without a timezone database on the server.
- **Device events are raised on travel COMPLETION**, not on command acknowledgement
  (firmware: `runMotor()` at `distanceToGo() == 0`). The simulator must match, or
  auto-close starts counting while the door is still moving.

## Demo mode

`env.demoMode` (runtime, from `env.js`) swaps the services onto `demoBackend`, an
in-memory simulation, for the backendless Vercel deployment. It is opt-in, shows a
permanent banner, and must never be enabled locally or in Docker. Never reintroduce a
silent mock fallback on error — errors must surface as errors.

## Known gaps

- No test project exists in the solution.
- `npm run build` does not typecheck. Run `npm run typecheck` before trusting a build.

## Working preferences

- Never commit or stage anything unless explicitly asked. When asked for a commit
  message, output the text only. Never add a `Co-Authored-By` line.
- Visual Studio locks `bin`; build and test with
  `--artifacts-path <elsewhere>`.
- Git Bash: `docker exec` with Linux paths needs `MSYS_NO_PATHCONV=1`.
- Keep shell scripts and Docker entrypoints LF — enforced by `.gitattributes`.
