import {
  DeviceClientKind,
  DoorCommand,
  DoorEvent,
  DoorState,
  DoorStatus,
  ScheduleEntry,
  ScheduleEntryKind,
} from "@/config/api";
import type { CreateScheduleRequest } from "./scheduleService";
import type { AnalyticsResponse } from "./analyticsService";
import type { AutoCloseSettings } from "./autoCloseService";

/**
 * In-memory stand-in for the API, used only when env.demoMode is true.
 *
 * The public Vercel deployment has no backend, so without this a visitor meets a login
 * form that can never succeed. It simulates door travel, schedules and event history so
 * the whole UI is explorable.
 *
 * This is NOT the old silent mock fallback. It runs only behind an explicit runtime
 * flag, never on error, and the UI shows a permanent banner saying the data is
 * simulated. State lives in memory and resets on reload.
 */

const TRAVEL_STEP_PERCENT = 4;
const TRAVEL_TICK_MS = 120;

interface DemoState {
  position: number;
  state: DoorState;
  obstacle: boolean;
  lastUpdated: string;
  target: number | null;
  events: DoorEvent[];
  schedules: ScheduleEntry[];
  autoClose: AutoCloseSettings;
}

function minutesAgo(minutes: number): string {
  return new Date(Date.now() - minutes * 60_000).toISOString();
}

const state: DemoState = {
  position: 0,
  state: DoorState.Closed,
  obstacle: false,
  lastUpdated: new Date().toISOString(),
  target: null,
  events: [
    { id: "d1", eventType: "DoorClosed", source: "Schedule", timestamp: minutesAgo(35) },
    { id: "d2", eventType: "DoorOpened", source: "LocalRfid", timestamp: minutesAgo(52) },
    { id: "d3", eventType: "ObstacleCleared", source: "System", timestamp: minutesAgo(96) },
    { id: "d4", eventType: "ObstacleDetected", source: "System", timestamp: minutesAgo(97) },
    { id: "d5", eventType: "DoorClosed", source: "AutoClose", timestamp: minutesAgo(140) },
    { id: "d6", eventType: "DoorOpened", source: "Remote", timestamp: minutesAgo(155) },
  ],
  schedules: [],
  autoClose: { enabled: false, afterSeconds: 30 },
};

let travelTimer: ReturnType<typeof setInterval> | null = null;
let autoCloseTimer: ReturnType<typeof setTimeout> | null = null;

/** Mirrors the backend: arm on a stop or a completed open, cancel once closed. */
function rearmAutoClose(): void {
  if (autoCloseTimer !== null) {
    clearTimeout(autoCloseTimer);
    autoCloseTimer = null;
  }

  if (!state.autoClose.enabled || state.position === 0) return;

  autoCloseTimer = setTimeout(() => {
    autoCloseTimer = null;
    state.target = 0;
    startTravel();
  }, state.autoClose.afterSeconds * 1000);
}

function touch(): void {
  state.lastUpdated = new Date().toISOString();
}

function addEvent(eventType: string, source: string): void {
  state.events.unshift({
    id: crypto.randomUUID(),
    eventType,
    source,
    timestamp: new Date().toISOString(),
  });
}

/** Advances the door toward its target, mimicking the stepper's travel time. */
function startTravel(): void {
  if (travelTimer !== null) return;

  travelTimer = setInterval(() => {
    if (state.target === null) {
      clearInterval(travelTimer!);
      travelTimer = null;
      return;
    }

    const direction = state.target > state.position ? 1 : -1;
    const next = state.position + direction * TRAVEL_STEP_PERCENT;

    state.position =
      direction > 0 ? Math.min(next, state.target) : Math.max(next, state.target);
    state.state = direction > 0 ? DoorState.Opening : DoorState.Closing;

    if (state.position === state.target) {
      state.state =
        state.position === 100 ? DoorState.Open
        : state.position === 0 ? DoorState.Closed
        : DoorState.Stopped;

      addEvent(state.position === 0 ? "DoorClosed" : "DoorOpened", "Remote");
      rearmAutoClose();

      state.target = null;
      clearInterval(travelTimer!);
      travelTimer = null;
    }

    touch();
  }, TRAVEL_TICK_MS);
}

export const demoBackend = {
  getDoorStatus(): DoorStatus {
    return {
      position: state.position,
      state: state.state,
      obstacle: state.obstacle,
      lastUpdated: state.lastUpdated,
      // Always "just now", so the indicator reads as a live simulated device rather than
      // reporting the demo as offline.
      lastSeenAtUtc: new Date().toISOString(),
      lastClientKind: DeviceClientKind.Simulator,
    };
  },

  sendCommand(command: DoorCommand, percentage: number | null): void {
    switch (command) {
      case DoorCommand.OPEN:
        state.target = 100;
        break;
      case DoorCommand.CLOSE:
        state.target = 0;
        break;
      case DoorCommand.STOP:
        state.target = null;
        state.state = DoorState.Stopped;
        touch();
        return;
      case DoorCommand.VENT:
        state.target = Math.max(1, Math.min(99, percentage ?? 30));
        break;
    }

    startTravel();
  },

  getSchedules(): ScheduleEntry[] {
    return [...state.schedules];
  },

  createSchedule(request: CreateScheduleRequest): ScheduleEntry {
    const entry: ScheduleEntry = {
      id: crypto.randomUUID(),
      kind: ScheduleEntryKind.Period,
      commandType: request.commandType,
      targetPercentage:
        request.commandType === DoorCommand.VENT ? (request.targetPercentage ?? null) : null,
      opensAtUtc: request.opensAtUtc,
      closesAtUtc: request.closesAtUtc,
    };

    state.schedules.push(entry);
    return entry;
  },

  deleteSchedule(groupId: string): void {
    state.schedules = state.schedules.filter((s) => s.id !== groupId);
  },

  getEvents(): DoorEvent[] {
    return [...state.events];
  },

  getAutoClose(): AutoCloseSettings {
    return { ...state.autoClose };
  },

  setAutoClose(settings: AutoCloseSettings): AutoCloseSettings {
    state.autoClose = { ...settings };
    rearmAutoClose();
    return this.getAutoClose();
  },

  /** Derived from the live event list, so the charts react to what the visitor does. */
  getAnalytics(): AnalyticsResponse {
    const dayNames = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];
    const opensByDay = new Map<string, number>(dayNames.map((d) => [d, 0]));

    for (const event of state.events) {
      if (event.eventType !== "DoorOpened") continue;

      const day = dayNames[new Date(event.timestamp).getDay()];
      opensByDay.set(day, (opensByDay.get(day) ?? 0) + 1);
    }

    const opens = state.events.filter((e) => e.eventType === "DoorOpened").length;
    const closes = state.events.filter((e) => e.eventType === "DoorClosed").length;

    const sources = new Map<string, number>();
    for (const event of state.events) {
      sources.set(event.source, (sources.get(event.source) ?? 0) + 1);
    }

    // Monday-first, matching the API.
    const ordered = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];

    return {
      opensPerDay: ordered.map((day) => ({ day, opens: opensByDay.get(day) ?? 0 })),
      openVsClosed: [
        { name: "Open", value: opens, color: "" },
        { name: "Closed", value: closes, color: "" },
      ],
      eventSources: [...sources].map(([name, value]) => ({ name, value, color: "" })),
    };
  },
};
