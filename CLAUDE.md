# Entrio — notes for Claude

IoT smart garage door system: ASP.NET Core 8 API (layered, service-per-area),
React/Vite frontend, and ESP32 firmware. See `README.md` for how to run it.

## Architecture

Layered, not CQRS. MediatR was removed — do not reintroduce commands, queries or
handlers.

```
IoT.Domain        entities only: plain property bags, no constructors, no methods
                  that mutate state (`RefreshToken.IsActive` is a fact, not a mutation)
IoT.Application   one service per area of work, each with an interface:
                  Devices/ Doors/ Schedules/ Analytics/ Identity/ Common/
IoT.Infrastructure  EF context, configurations, hashing, tokens, background worker
IoT.API           thin controllers: HTTP mapping and projection only
```

Rules:
- **Controllers hold no application logic.** No database access, no business rules,
  no private helpers beyond a response projection. If a controller grows an `if`
  that is not about HTTP, it belongs in a service.
- **Entities are created and mutated in services**, with object initialisers. No
  constructors, no `MarkX()` methods on entities.
- **Requests are validated by FluentValidation**, in the service, not the controller.
  Services inject `IValidator<T>` and call `ValidateAndThrowAsync` as their first
  statement; `ValidationExceptionHandler` turns the failure into a 400 keyed by
  property name.
- Every table name is pinned with `ToTable` in `Configurations/`, which is what makes
  a CLR rename schema-neutral (see `RenameEntitiesDropEntitySuffix`, an empty
  migration kept so the snapshot matches the renamed types).

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

## Validation

Request types live in the Application layer beside their service (`DoorRequests.cs`,
`ScheduleRequests.cs`, `DeviceRequests.cs`, `AuthContracts.cs`), not as nested classes
on controllers — that is what gives validators something to attach to and lets a
controller bind and pass straight through.

The split that decides where a check goes:

- **Validator** — anything answerable from the request alone: ranges, enum membership,
  required fields, email format, future-dating. Conditional rules use `.When(...)`, so
  a Vent percentage is required only for Vent.
- **Service** — anything needing database or domain state: "no device registered",
  "schedule not found", "these credentials are wrong".

Bad credentials must stay `UnauthorizedException` (401), never a validation failure
(400): a well-formed request with the wrong password is not a malformed request.

Register new validators nowhere — `AddValidatorsFromAssemblyContaining` discovers
everything in the Application assembly.

## Errors

Throw `EntrioException` subclasses (`UnauthorizedException`, `NotFoundException`,
`BusinessRuleException`) for anything a client should see. The handler chain in
`IoT.API/Middleware` maps them to a status code and a consistent JSON body. The chain
runs in registration order: domain errors, then validation failures, then a terminal
catch-all. Anything
else becomes a logged 500 with a `traceId` and no internal detail — never return a
raw exception. Do not use `return BadRequest(...)`/`Problem(...)` in new code.

## Things that are load-bearing

- **Device GUID `0f8fad5b-d9cb-469f-a165-70867728950e`** is hardcoded in the
  firmware's status payload and `DatabaseSeeder.FirmwareDeviceId`. Both must agree.
  Device endpoints resolve the device from its `X-Device-Key` via
  `HttpContext.Items["Device"]`, never from a body field, so an authenticated device
  can only ever act as itself. The firmware still sends `deviceId`; it is ignored.
- **Enums cross the wire as INTEGERS.** No `JsonStringEnumConverter` is registered, and
  both clients depend on that: the firmware parses `commandType` with
  `doc["commandType"] | -1` and the frontend compares against numeric `DoorState`
  members. Registering a global string-enum converter would break the device, which
  cannot be reflashed. `PendingCommandResponse` casts to `int` explicitly as a guard.
  Event type and source are the exception and are deliberately strings, because only the
  dashboard reads them.
- **Controller actions return `ActionResult<T>`**, not `IActionResult` with an anonymous
  object — otherwise Swagger documents no shape at all, which matters most for the device
  endpoints whose contracts are frozen.
- **Device connectivity is derived, never asserted.** `Device.LastSeenAtUtc` and
  `LastClientKind` are written on every authenticated device call; the UI treats a gap
  over `DEVICE_OFFLINE_AFTER_MS` (30s) as offline. The simulator sends
  `X-Device-Client: simulator`; the firmware sends nothing, so ABSENCE means hardware —
  which is why identifying the simulator needed no firmware change. Never hardcode a
  connection status.
- **One poll feeds everything.** `DeviceStatusProvider` polls `/door/status` once and the
  header, sidebar and dashboard read from it. Do not add a second poller for the same fact.
- **A user schedule is TWO rows sharing a `ScheduleGroupId`**: the Open or Vent and the
  Close that must follow it. `ScheduleGroupId == null` means the row is system-raised
  (currently only auto-close), and that distinction is load-bearing: `ArmAsync`
  supersedes only null-group rows. It used to deactivate every pending row, so arming
  auto-close silently cancelled the user's own schedules.
- **Only Open and Vent are schedulable.** A bare Close is not a user action (auto-close
  covers the safety case) and Stop is meaningless on a stationary door.
- **Seeding is the only way rows get created.** There is no `HasData` anywhere; a
  fresh database with no seed means no login and 401 on every device call.
  `DatabaseSeeder` is idempotent and skips existing rows.
- **The auto-close condition in `DeviceEventService.RecordAsync`** treats a last
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
- **A stop raises no event**, because travel never completed, so auto-close is armed
  from the STATUS transition instead (`DeviceStatusService.ReactToTransitionAsync`).
  That handler compares against the previous state on purpose: the controller
  re-reports its current status every 10s, so reacting to the state rather than the
  change would reset the countdown forever and it would never fire.
- **The simulator is volume-mounted**, so editing `simulator.mjs` needs
  `docker compose restart entrio-device-sim`; a `--build` will not pick it up.

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
