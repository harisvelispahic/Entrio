# Entrio — IoT Smart Garage Door System 🚗🔐

Entrio is an IoT-based smart garage access system built using:

- ESP32 (firmware + hardware control)
- RFID authentication
- Headlight flash detection (photoresistor)
- Stepper motor (door control)
- Ultrasonic obstacle detection (safety)
- ASP.NET Core backend + EF Core database
- React.ts frontend
- JWT authentication

Designed as a **complete IoT learning project**:
secure access, controlled movement, safety logic, and remote interaction.

---

## 🗺️ 1️⃣ PIN MAPPING (Authoritative Reference)

This is the **single source of truth** for wiring.

### 🔌 ESP32 Power

- **USB** → powers ESP32 (logic)
- **VIN (5V)** → HC-SR04 VCC & 28BYJ-48 Stepper Motor
- **3V3** → RFID RC522 VCC
- **GND (any)** → shared ground for **ALL components**

---

## 🪪 RFID RC522 (SPI)

| ESP32 Pin | RC522 Pin | Purpose |
|-----------|-----------|--------|
| GPIO **5** | SDA / SS | Chip Select |
| GPIO **18** | SCK | SPI Clock |
| GPIO **23** | MOSI | SPI Data Out |
| GPIO **19** | MISO | SPI Data In |
| GPIO **4** | RST | Reset |
| **3V3** | VCC | Power (⚠️ 3.3V ONLY) |
| **GND** | GND | Ground |

📌 **Purpose**  
Reads RFID cards → opens a 10-second authorization window.

---

## 💡 Photoresistor (LDR) – Headlight Flash Detection

### Voltage Divider

| ESP32 Pin | Component |
|----------|----------|
| GPIO **36** (ADC1) | LDR output |
| **3V3** | LDR top |
| **GND** | Resistor (~10kΩ) |

📌 Detects **double headlight flash** during auth window.

---

## 🚪 Stepper Motor (28BYJ-48) + ULN2003

### ESP32 Connections

| ESP32 Pin | ULN2003 | Motor Coil |
|-----------|---------|-----------|
| GPIO **13** | IN1 | Coil A |
| GPIO **12** | IN2 | Coil B |
| GPIO **14** | IN3 | Coil C |
| GPIO **27** | IN4 | Coil D |

### Power

- **ULN2003 VCC** → External **5V USB** or **VIN pin**
- **ULN2003 GND** → ESP32 GND (**common ground**)

📌 Opens and closes the door smoothly.

---

## 📡 HC-SR04 Ultrasonic Sensor (Obstacle Detection)

| ESP32 Pin | HC-SR04 | Notes |
|----------|---------|------|
| GPIO **26** | TRIG | Output |
| GPIO **32** | ECHO | ⚠️ NEEDS voltage divider |
| **VIN (5V)** | VCC | Power |
| **GND** | GND | Shared ground |

### Voltage Divider (MANDATORY)
ECHO → 2kΩ → GPIO 32

GPIO 32 → 3.3kΩ → GND


📌 Stops the motor if something is under the door.

---

## 🚦 LEDs (User Feedback)

| ESP32 Pin | LED | Meaning |
|----------|-----|--------|
| GPIO **33** | Green | Auth success |
| GPIO **25** | Red | Auth failed |

Wiring:
GPIO → 220–330Ω resistor → LED → GND

---


# 🧩 2️⃣ COMPONENT LIST

### Core Controller
- ESP32 Dev Board

### Sensors
- RFID RC522
- Photoresistor (LDR)
- HC-SR04 ultrasonic sensor

### Actuators
- 28BYJ-48 stepper motor
- ULN2003 driver board
- LEDs (red + green)

### Power
- USB (ESP32 logic)
- External USB 5V (motor + sensor)

---

# 🔁 3️⃣ SYSTEM FLOW

### Normal operation

1. Scan RFID card
2. UID is validated
3. **10-second window opens**
4. Flash headlights twice
5. Door opens
6. Door closes automatically

### Safety behavior

- While closing:
  - Ultrasonic detects obstacle
  - Motor pauses
  - When clear (≈3s), resumes from same position

---

# ⚙️ 4️⃣ Entrio supports both **local offline control** and **remote Wi-Fi/web control**.


## 📌 Features

### Local (no Wi-Fi required)

- RFID card door control
- headlight flashing trigger
- automatic auto-close after inactivity

### Remote (web interface)

- Open / Close / Stop
- Vent (partial opening)
- Live status display
- Command scheduling
- Event logs
- Analytics dashboard
- Authentication (login/logout)
- Auto-close after inactivity

---
Handles:

- RFID reading
- flash detection
- motor control
- ultrasonic safety
- communicating with backend (future)



---



# 🔧 5️⃣ Requirements

### Docker (recommended — covers backend, frontend and database)
- Docker Desktop

### Backend (only for local, non-Docker runs)
- .NET 8 SDK
- Visual Studio
- No SQL Server install needed — local runs use the Docker database on port 1437
- No `dotnet-ef` needed — the API applies migrations itself at startup

### Frontend (only for local, non-Docker runs)
- Node.js 20.12+ (for the built-in `process.loadEnvFile` used by the config script)
- npm

### ESP32
- Arduino IDE or PlatformIO
- Libraries:
  - WiFi
  - HTTPClient
  - ArduinoJson
  - MFRC522 (RFID)
  - Stepper / AccelStepper

---

# 🚀 Running the Project

There are two ways to run Entrio, and they share one configuration file.

| Mode | What it is | Use it when |
|---|---|---|
| **Docker** | Whole stack in containers: SQL Server, API, web | Normal use; one command, nothing to install |
| **Local** | API from Visual Studio, web from the Vite dev server | Debugging with breakpoints or hot reload |

Local runs use the **Docker database** through its published port, so you never
need SQL Server installed on Windows. The database container must be up either way.

---

## Configuration

All infrastructure config lives in **one gitignored file at the repo root: `.env`**.
Nothing infrastructural is committed — no connection strings, no keys, not even
empty placeholders.

| File | Holds | Tracked? |
|---|---|---|
| `.env` | Every secret and connection string, for **both** run modes | ❌ gitignored |
| `.env.example` | Template with placeholders and comments | ✅ tracked |
| `Backend.IoT/IoT.API/appsettings.json` | Application behaviour only: logging, CORS origins, JWT issuer/audience | ✅ tracked |
| `Backend.IoT/IoT.API/appsettings.Development.json` | The default `Logging` block, nothing else | ✅ tracked |
| `Frontend.IoT/public/env.js` | Generated at start-up from `.env`; sets `window.__env` | ❌ gitignored |
| `Esp32.IoT/secrets.h` | WiFi credentials, server URL, device key | ❌ gitignored |
| `Esp32.IoT/secrets.example.h` | Template for the above | ✅ tracked |

### How one `.env` serves both run modes

`.env` uses plain, readable names (`JWT_KEY`). ASP.NET Core binds environment
variables using `Section__Key` (`Jwt__Key`). The bridge differs per mode:

- **Docker** — `docker-compose.yml` sets the `Section__Key` names directly in each
  service's `environment:` block.
- **Local** — `DotNetEnv` reads `.env`, then the alias map in
  `IoT.API/Configuration/EnvironmentConfiguration.cs` copies each plain name onto
  its `Section__Key` counterpart.

Both express the **same mapping**, and each side says so in comments. A value
already set in the environment always wins, so compose never loses to a stray file.

Values that differ between host and container — chiefly the connection string,
which points at the compose service name inside Docker but at `localhost,1437`
outside — cannot be aliased (one name, two values). Those sit in a clearly marked
block at the **bottom** of `.env` as explicit `Section__Key` lines.

> **Adding a new secret means touching three places together:**
> `.env.example`, the alias map in `EnvironmentConfiguration.cs`, and the
> `environment:` block in `docker-compose.yml`.

### First-time setup

```bash
cp .env.example .env
```

Then fill in `.env`:

| Variable | Notes |
|---|---|
| `MSSQL_SA_PASSWORD` | 8+ chars, upper + lower + digit + symbol |
| `JWT_KEY` | 32+ characters. `openssl rand -base64 48` |
| `DEVICE_KEY` | Shared secret for the ESP32. Must match `Esp32.IoT/secrets.h` |
| `API_BASE_URL` | `http://localhost:5263/api` |
| `ConnectionStrings__DefaultConnection` | Bottom block. Password must match `MSSQL_SA_PASSWORD` |

> ⚠️ **SQL Server applies `MSSQL_SA_PASSWORD` only when its data volume is first
> created.** Changing it later silently has no effect — the container keeps the
> original password and the API can no longer log in. Fixing it requires
> `docker compose down -v`, **which deletes the database.**

---

## Logging in

The owner account is seeded on first start from
`Backend.IoT/IoT.API/Configuration/DatabaseSeeder.cs`:

| Email | Password |
|---|---|
| `admin@entrio.local` | `Entrio123!` |

These are deliberately public and in source, not in `.env`, so anyone who clones the
repo can log in. There is nothing behind the login but a simulated garage door. The
login page shows them too, with a button to fill them in. A real deployment would
replace the seeder.

Auth uses short-lived access tokens (15 min) plus rotating refresh tokens (7 days).
The frontend refreshes transparently, so you are not logged out mid-session; a
refresh token can only be used once, and logging out revokes every token for the
account.

---

## Mode 1 — Docker

```bash
docker compose up -d --build
```

Startup is ordered by healthchecks: the API waits for SQL Server to answer
`SELECT 1`, and the web container waits for the API's port to open. The API
applies EF migrations and seeds the owner account and device row **before** it
starts listening, so there is no manual `dotnet ef database update` step.

| Service | URL | Container port |
|---|---|---|
| Web | <http://localhost:4300> | 80 |
| API | <http://localhost:5263> | 8080 |
| Swagger | <http://localhost:5263/swagger> | |
| SQL Server | `localhost,1437` (user `sa`) | 1433 |

**Why these ports.** `5263` is fixed because the ESP32 firmware hardcodes it in
`SERVER_BASE_URL` and it is a listed CORS origin — changing it means reflashing
the device. `4300` keeps `8080` free for the Vite dev server, so Docker and
`npm run dev` can run at the same time; both origins are allowed in
`appsettings.json`. `1437` avoids 1433 (local instance), 1434 (SQL Browser),
1435 and 1436 (other projects' containers).

Useful commands:

```bash
docker compose logs -f entrio-api     # follow API logs
docker compose ps                     # health status
docker compose down                   # stop, keep data
docker compose down -v                # stop and DELETE the database
```

Query the database from inside its container (Git Bash needs `MSYS_NO_PATHCONV=1`
so it does not mangle the Linux path):

```bash
MSYS_NO_PATHCONV=1 docker compose exec entrio-sqlserver \
  /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" \
  -C -d GarageIoT -Q "SELECT * FROM Devices"
```

---

## Mode 2 — Local (Visual Studio + Vite)

The database still comes from Docker:

```bash
docker compose up -d entrio-sqlserver
```

**Backend** — run the `http` profile from Visual Studio, or:

```bash
cd Backend.IoT/IoT.API
dotnet run
```

No environment variables to set: `DotNetEnv` finds the repo-root `.env` by walking
up from the working directory. Swagger opens at <http://localhost:5263/swagger>.

There are three launch profiles:

| Profile | Binds | Use for |
|---|---|---|
| `http` | `localhost:5263` | normal development; opens Swagger |
| `https` | `localhost:7287` + `localhost:5263` | when you need TLS locally |
| `http-lan` | `0.0.0.0:5263` (all interfaces) | ESP32 work — the device reaches the API over the LAN, which a localhost-only binding refuses |

`http-lan` does not launch a browser, because `0.0.0.0` is a valid address to *bind*
but not one a browser can reliably *open*.

> Visual Studio locks `bin`, so build or test from a shell with
> `--artifacts-path <some other folder>` to avoid file-in-use errors.

**Frontend**

```bash
cd Frontend.IoT
npm install
npm run dev          # http://localhost:8080
```

`npm run dev` automatically regenerates `public/env.js` from the repo-root `.env`
first (the `predev` hook), so there is no separate configuration step.

> The API cannot run in Docker and locally at the same time — both want port 5263.
> Stop one first: `docker compose stop entrio-api`.

---

## How the frontend is configured at runtime

The API URL is **not** baked into the build. `import.meta.env.VITE_*` values are
substituted by Vite during `npm run build` and frozen into the bundle, which would
mean rebuilding the image to change backends. Instead:

1. `env.js` sets `window.__env` and is loaded from `index.html` **before** the app
   bundle.
2. `src/config/env.ts` reads it and throws a clear, named error if a required value
   is missing.
3. It is written at **start-up**, not build time — by `scripts/generate-env.mjs`
   locally (Node's built-in `process.loadEnvFile`, no dependency) and by
   `/docker-entrypoint.d/40-env.sh` with `envsubst` in the container.

`API_BASE_URL` must be reachable **from the browser**, so it is `http://localhost:5263/api`
and never `http://entrio-api:8080`. The `fetch` runs on your machine, outside the
Docker network, where compose service names do not resolve.

Only public values belong in `env.js` — every visitor's browser downloads it.

---

## Time and time zones

**The API deals only in UTC instants and never converts between zones.** Timestamps go
out with an explicit `Z`, and incoming values are read as UTC. The browser does all
local-time work: it turns your wall-clock choice into a UTC instant and renders
incoming instants back into local time.

That division is also what makes daylight saving correct for free. The browser knows
the offset actually in force on the chosen date, so a time picked in winter and one
picked in summer both convert correctly — no timezone database is needed on the
server.

---

## 🧪 Device simulator (no hardware required)

The ESP32 controller has been disassembled, but it only ever spoke four HTTP
endpoints. `tools/device-simulator/` re-implements them, so the whole control loop
— web command → device poll → ack → status → dashboard — works without hardware.

```bash
docker compose --profile simulator up -d
docker compose logs -f entrio-device-sim
```

Or on the host:

```bash
cd tools/device-simulator
node --env-file=../../.env simulator.mjs
```

---

## Demo mode (the Vercel deployment)

The public Vercel deployment has no backend, so a visitor would otherwise meet a login
form that can never succeed. Setting `DEMO_MODE=true` runs the app against an
in-memory simulation instead: the door opens, closes and vents, schedules can be
created and deleted, and the charts react to what you do. State resets on reload.

A permanent banner says the data is simulated, and because the flag is **runtime**
config it cannot leak into a local or Docker run — those never set it.

Set it in Vercel's environment variables. To try it locally before deploying:

```bash
cd Frontend.IoT
npm run dev:demo          # http://localhost:8080, demo mode on
npm run dev               # back to normal, talking to the real API
```

(`dev:demo` passes a flag to the config generator rather than setting an inline
environment variable, because `DEMO_MODE=true npm run dev` is Bash-only syntax and
fails in PowerShell.)

> This replaces an older silent fallback that returned invented data whenever an API
> call failed, which made an outage or a 401 look exactly like real activity. Errors
> now surface as errors.

---

## 📡 ESP32 setup

Firmware is **not** containerised — it is cross-compiled and flashed to the chip,
so there is nothing for a container to run. It does not need to be: the ESP32 is
an **HTTP client over WiFi**, not a USB device, so it needs no serial passthrough.
It simply calls the API on your PC's LAN address, and Docker publishes port 5263
on that same address. A containerised backend needs no firmware changes at all.

1. Copy the credentials template:

   ```bash
   cd Esp32.IoT
   cp secrets.example.h secrets.h
   ```

2. Fill in `secrets.h` (gitignored):

   - `WIFI_SSID` / `WIFI_PASSWORD`
   - `SERVER_BASE_URL` — your PC's **LAN IP**, not `localhost`: the ESP32 resolves
     this itself, so `localhost` would mean the ESP32. Find it with `ipconfig`.
   - `DEVICE_KEY` — must exactly match `DEVICE_KEY` in the repo-root `.env`

   These four are the bootstrap set: the device needs all of them **before** it can
   reach the backend, so none of them can come from the API or `appsettings.json`.

3. Ensure the ESP32 and your PC are on the same WiFi, then upload from the Arduino IDE.

To check a device key without flashing anything:

```bash
curl -i -X POST http://localhost:5263/api/device/ping -H "X-Device-Key: <your key>"
```

`200 DEVICE AUTH OK` means the key matches the seeded device; `401` means it does not.

The device's identity GUID `0f8fad5b-d9cb-469f-a165-70867728950e` is hardcoded in the
firmware's status payload and in the seeder (`DatabaseSeeder.FirmwareDeviceId`). Both
must agree. The frontend no longer needs it: the API resolves the single device
server-side.

**Future work:** WiFiManager + NVS would let you change the network and server URL
from a captive portal instead of reflashing, and `allowedUIDs` could be served from
the backend so RFID cards are managed from the dashboard (it would need a cached
copy on the device to keep offline operation working).

---

# 🔐 Authentication

Protected functionality requires login:

- schedules
- analytics
- sending commands
- logs

Authentication uses JWT tokens.

---

## 📊 Analytics Dashboard

Entrio tracks:

| Metric           | Description              |
|------------------|--------------------------|
| Opens per day    | Usage trends             |
| Open vs Closed   | Ratio of door states     |
| Event sources    | Remote / System / RFID   |
