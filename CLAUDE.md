# Entrio — notes for Claude

IoT smart garage door system: ASP.NET Core 8 API (clean architecture + MediatR),
React/Vite frontend, and ESP32 firmware. See `README.md` for how to run it.

## Configuration

**One rule: nothing infrastructural lives in a tracked file.** No connection
strings, no keys, no API secrets — not even empty placeholder keys. If you find
one, delete it rather than blanking it.

| File | Holds | Tracked |
|---|---|---|
| `.env` (repo root) | Every secret and connection string, for both run modes | no |
| `.env.example` | Template with placeholders and comments | yes |
| `IoT.API/appsettings.json` | Behaviour only: logging, `Cors:AllowedOrigins`, JWT issuer/audience | yes |
| `IoT.API/appsettings.Development.json` | The default `Logging` block only | yes |
| `Frontend.IoT/public/env.js` | Generated at start-up; sets `window.__env` | no |
| `Esp32.IoT/secrets.h` | WiFi credentials, server URL, device key | no |

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

## Things that are load-bearing

- **Device GUID `0f8fad5b-d9cb-469f-a165-70867728950e`** is hardcoded in three
  places: the firmware's status payload, `scheduleService.ts`, and
  `DatabaseStartup.FirmwareDeviceId`. All three must agree or status posts and
  schedules fail.
- **Seeding is the only way rows get created.** There is no `HasData` anywhere;
  a fresh database with no seed means no login and 401 on every device call.
  `DatabaseStartup.SeedAsync` is idempotent and skips existing rows.
- **`AutoCloseService` sits in the global namespace** (no `namespace`
  declaration). Do not add a `using` for it.

## Known gaps (not bugs introduced here)

- No test project exists in the solution.
- `DeviceStatusController` is `[AllowAnonymous]` with a TODO to secure it —
  anyone who knows the device GUID can post door status. The firmware already
  sends `X-Device-Key`, so adding `[DeviceAuthorize]` would not break it.
- `ProtectedRoute` was removed from every frontend route in `fd1fedc` for a
  Vercel demo, but the backend still requires `[Authorize]`. Pages render and
  then their API calls 401 until you log in.

## Working preferences

- Never commit or stage anything unless explicitly asked. When asked for a commit
  message, output the text only. Never add a `Co-Authored-By` line.
- Visual Studio locks `bin`; build and test with
  `--artifacts-path <elsewhere>`.
- Git Bash: `docker exec` with Linux paths needs `MSYS_NO_PATHCONV=1`.
- Keep shell scripts and Docker entrypoints LF — enforced by `.gitattributes`.
