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

export interface DoorStatus {
  position: number; // 0-100, 0 = closed, 100 = open
  state: DoorState;
  obstacle?: boolean;
  lastUpdated?: string;
}

export interface Schedule {
  id: string;
  deviceId: string;
  commandType: DoorCommand;
  targetPercentage?: number;
  executeAtUtc: string;
  isActive: boolean;
  wasTriggered: boolean;
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
