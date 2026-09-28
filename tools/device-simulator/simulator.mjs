/**
 * ESP32 stand-in.
 *
 * The physical controller has been disassembled, but its protocol is just four
 * HTTP endpoints authenticated by an X-Device-Key header. This script speaks
 * exactly those, mirroring Esp32.IoT/Esp32.IoT.ino:
 *
 *   GET  /api/device/commands/pending      poll for work        (.ino line ~599)
 *   POST /api/device/commands/{id}/ack     acknowledge          (.ino line ~488)
 *   POST /api/device/status                report door state    (.ino line ~232)
 *   POST /api/device/events                report an event      (.ino line ~223)
 *
 * It simulates door travel so the dashboard shows a door actually moving,
 * rather than teleporting between states.
 *
 * Plain Node, no dependencies. Run on the host or as the "simulator" compose
 * profile. Environment: SERVER_BASE_URL, DEVICE_KEY.
 */

const BASE = (process.env.SERVER_BASE_URL ?? "http://localhost:5263").replace(/\/+$/, "");
const DEVICE_KEY = process.env.DEVICE_KEY;

if (!DEVICE_KEY) {
  console.error("DEVICE_KEY is not set. It must match DEVICE_KEY in the repo-root .env.");
  process.exit(1);
}

// Matches the firmware's poll cadence.
const POLL_MS = Number(process.env.POLL_MS ?? 2000);
// Percentage points of travel per tick, so movement is visible in the UI.
const STEP_PERCENT = 10;

// Mirrors the DoorState enum in IoT.Domain/Entities/Devices/DeviceEnums.cs.
const DoorState = { Closed: 0, Opening: 1, Open: 2, Closing: 3, Stopped: 4, Error: 5 };
// Mirrors CommandType as the firmware interprets it.
const Command = { OPEN: 0, CLOSE: 1, STOP: 2, VENT: 3 };

// The firmware hardcodes this GUID in its status payload and the API seeds the
// device row with it (see DatabaseStartup.FirmwareDeviceId). /pending does not
// return a device id, so the simulator must know it the same way the ESP32 does.
const DEVICE_ID = process.env.DEVICE_ID ?? "0f8fad5b-d9cb-469f-a165-70867728950e";

const state = {
  deviceId: DEVICE_ID,
  position: 0,
  door: DoorState.Closed,
  obstacle: false,
  target: null,
};

function headers(extra = {}) {
  return {
    "X-Device-Key": DEVICE_KEY,
    // Identifies this as the stand-in rather than real hardware, so the dashboard can
    // say "simulated environment" instead of claiming an ESP32 is attached. The firmware
    // sends no such header, so its absence is what means hardware -- which is why this
    // needed no firmware change.
    "X-Device-Client": "simulator",
    ...extra,
  };
}

async function call(method, path, body) {
  const response = await fetch(`${BASE}${path}`, {
    method,
    headers: body === undefined ? headers() : headers({ "Content-Type": "application/json" }),
    body: body === undefined ? undefined : JSON.stringify(body),
  });

  const text = await response.text();

  if (!response.ok) {
    throw new Error(`${method} ${path} -> ${response.status} ${text.slice(0, 200)}`);
  }

  return text ? JSON.parse(text) : null;
}

async function reportStatus() {
  if (!state.deviceId) return;

  await call("POST", "/api/device/status", {
    deviceId: state.deviceId,
    doorState: state.door,
    positionPercent: state.position,
    obstacleDetected: state.obstacle,
  });
}

/**
 * type must be a DeviceEventType name and source a DeviceEventSource name
 * (DeviceEnums.cs); the controller parses both with Enum.TryParse and 400s on
 * anything else. Same body shape the firmware's sendDeviceEvent builds.
 */
async function reportEvent(type, source = "Remote") {
  await call("POST", "/api/device/events", { type, source });
}

/** Advances the door one tick toward its target, exactly as the stepper would. */
/** Set by advance() on the tick travel completes, consumed by tick() to raise the event. */
let justArrived = null;

function advance() {
  if (state.target === null) return false;

  if (state.position === state.target) {
    state.door =
      state.position === 0 ? DoorState.Closed
      : state.position === 100 ? DoorState.Open
      // A partial position is a completed vent, which the firmware also reports as
      // DoorOpened (it leaves the door off its closed stop).
      : DoorState.Stopped;

    justArrived = state.door;
    state.target = null;
    return true;
  }

  const direction = state.target > state.position ? 1 : -1;
  state.door = direction > 0 ? DoorState.Opening : DoorState.Closing;

  const next = state.position + direction * STEP_PERCENT;
  state.position = direction > 0 ? Math.min(next, state.target) : Math.max(next, state.target);

  return true;
}

function applyCommand(command) {
  const type = command.commandType ?? command.CommandType;
  const percentage = command.targetPercentage ?? command.TargetPercentage ?? null;

  switch (type) {
    case Command.OPEN:
      state.target = 100;
      break;
    case Command.CLOSE:
      state.target = 0;
      break;
    case Command.STOP:
      state.target = null;
      state.door = DoorState.Stopped;
      break;
    case Command.VENT:
      state.target = Math.max(1, Math.min(99, percentage ?? 30));
      break;
    default:
      console.warn(`  unknown command type ${type}, ignoring`);
      return false;
  }

  return true;
}

async function tick() {
  const pending = await call("GET", "/api/device/commands/pending");

  if (pending) {
    const id = pending.id ?? pending.Id;
    const type = pending.commandType ?? pending.CommandType;

    console.log(`[cmd] ${id} type=${type} -> applying`);

    if (applyCommand(pending)) {
      await call("POST", `/api/device/commands/${id}/ack`, "");
      console.log(`[cmd] ${id} acknowledged`);

      // Report immediately after applying. STOP changes the state without starting any
      // travel, so advance() below returns false and would never report it -- the
      // backend would keep showing the door as still moving. The firmware reports the
      // same way, from stopDoor().
      await reportStatus();
      console.log(`[state] position=${state.position}% door=${state.door}`);
    }
  }

  if (advance()) {
    await reportStatus();
    console.log(`[state] position=${state.position}% door=${state.door}`);

    // Raise the door event only once travel has FINISHED, which is what the firmware
    // does (Esp32.IoT.ino runMotor(), at `stepper.distanceToGo() == 0`). Reporting it
    // on acknowledgement instead started the auto-close countdown at the beginning of
    // the open, so a 5s delay fired while the door was still moving.
    if (justArrived !== null) {
      if (justArrived === DoorState.Open || justArrived === DoorState.Stopped) {
        await reportEvent("DoorOpened", "Remote");
      } else if (justArrived === DoorState.Closed) {
        await reportEvent("DoorClosed", "Remote");
      }

      justArrived = null;
    }
  }
}

console.log(`Entrio device simulator -> ${BASE} (poll ${POLL_MS}ms)`);

// One authenticated call to confirm the device key is accepted before looping,
// so a bad key fails immediately and loudly instead of scrolling past.
try {
  // Returns 204 (empty) when nothing is queued, which still proves the key works.
  await call("GET", "/api/device/commands/pending");
  console.log("Device key accepted.");
  await reportStatus();
} catch (error) {
  console.error(`Handshake failed: ${error.message}`);
  process.exit(1);
}

setInterval(() => {
  tick().catch((error) => console.error(`[error] ${error.message}`));
}, POLL_MS);
