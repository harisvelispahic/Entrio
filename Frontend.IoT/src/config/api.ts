// API Configuration
// Resolved at RUNTIME from window.__env (see src/config/env.ts and public/env.js),
// not baked in at build time, so one built image can target any backend.
import { env } from "@/config/env";

export const API_BASE_URL = env.apiBaseUrl;


// Polling interval for door status (in milliseconds)
export const DOOR_STATUS_POLL_INTERVAL = 3000;

// Door commands enum matching backend
export enum DoorCommand {
  OPEN = 0,
  CLOSE = 1,
  STOP = 2,
  VENT = 3,
}

// Door states
export enum DoorState {
  Closed = 0,
  Opening = 1,
  Open = 2,
  Closing = 3,
  Stopped = 4,
  Error = 5,
}

export const DoorStateLabels: string[] = ["closed", "opening", "open", "closing", "stopped", "error"];

// API Response types
export interface LoginResponse {
  token: string;
}

/** Mirrors DeviceClientKind on the API. */
export enum DeviceClientKind {
  Unknown = 0,
  Hardware = 1,
  Simulator = 2,
}

export interface DoorStatus {
  position: number; // 0-100, 0 = closed, 100 = open
  state: DoorState;
  obstacle?: boolean;
  lastUpdated?: string;
  /** When the controller last authenticated. MinValue-ish means it never has. */
  lastSeenAtUtc?: string;
  /** What last reported in, so the UI states hardware or simulator rather than guessing. */
  lastClientKind?: DeviceClientKind;
}

/**
 * How long after its last check-in the controller is treated as offline.
 *
 * The firmware reports status every 10s and polls for commands every 1-4s, so a gap
 * beyond this means it has actually stopped talking rather than simply being idle.
 */
export const DEVICE_OFFLINE_AFTER_MS = 30_000;

/** Mirrors ScheduleEntryKind on the API. */
export enum ScheduleEntryKind {
  Period = 0,
  AutoClose = 1,
}

/**
 * One row in the schedules list. A user-created period collapses its two underlying
 * rows into a single entry keyed by group id; a pending auto-close arrives as its own
 * entry and is not deletable.
 */
export interface ScheduleEntry {
  id: string;
  kind: ScheduleEntryKind;
  commandType: DoorCommand;
  targetPercentage?: number | null;
  /** Null for an auto-close, which has no opening half. */
  opensAtUtc: string | null;
  closesAtUtc: string;
}

export interface DoorEvent {
  id: string;
  eventType: string;
  /** DeviceEventSource enum name, serialized by the API as a string. */
  source: string;
  timestamp: string;
  details?: string;
}

export interface CommandRequest {
  command: DoorCommand;
  percentage: number | null;
}
