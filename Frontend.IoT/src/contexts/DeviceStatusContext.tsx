import React, {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import { doorService } from "@/services/doorService";
import {
  DEVICE_OFFLINE_AFTER_MS,
  DeviceClientKind,
  DOOR_STATUS_POLL_INTERVAL,
  DoorCommand,
  DoorState,
  DoorStatus,
} from "@/config/api";
import { toast } from "@/hooks/use-toast";

/**
 * Single source of truth for door status.
 *
 * The header, the sidebar footer and the dashboard all need it, and each polling
 * separately would triple the request rate for one shared fact. The provider polls once
 * and everything reads from here.
 */
interface DeviceStatusContextType {
  status: DoorStatus;
  /** True when the API itself is unreachable or erroring. */
  apiError: string | null;
  /** True when the controller has checked in recently enough to count as connected. */
  isDeviceOnline: boolean;
  /** What last reported in. Unknown until something does. */
  clientKind: DeviceClientKind;
  lastSeenAtUtc: string | null;
  isCommandInFlight: boolean;
  activeCommand: DoorCommand | null;
  sendCommand: (command: DoorCommand, percentage?: number) => Promise<void>;
  refresh: () => Promise<void>;
}

const DeviceStatusContext = createContext<DeviceStatusContextType | undefined>(undefined);

/** DateTime.MinValue round-trips as year 1, which means "has never reported". */
function parseLastSeen(value?: string): string | null {
  if (!value) return null;

  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime()) || parsed.getUTCFullYear() < 2000) return null;

  return value;
}

export function DeviceStatusProvider({ children }: { children: React.ReactNode }) {
  const [status, setStatus] = useState<DoorStatus>({
    position: 0,
    state: DoorState.Closed,
  });
  const [apiError, setApiError] = useState<string | null>(null);
  const [isCommandInFlight, setIsCommandInFlight] = useState(false);
  const [activeCommand, setActiveCommand] = useState<DoorCommand | null>(null);

  // Bumped on a timer so staleness is re-evaluated even when no new status arrives --
  // otherwise a device that goes silent would stay "online" forever, because nothing
  // would trigger a re-render.
  const [, setTick] = useState(0);
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const refresh = useCallback(async () => {
    try {
      setStatus(await doorService.getStatus());
      setApiError(null);
    } catch (err) {
      // A 401 is handled centrally by the API layer (refresh, then session expiry), so
      // anything reaching here is a real failure worth surfacing.
      setApiError(err instanceof Error ? err.message : "Failed to fetch status");
    }
  }, []);

  useEffect(() => {
    refresh();

    intervalRef.current = setInterval(() => {
      refresh();
      setTick((value) => value + 1);
    }, DOOR_STATUS_POLL_INTERVAL);

    return () => {
      if (intervalRef.current) clearInterval(intervalRef.current);
    };
  }, [refresh]);

  const sendCommand = useCallback(
    async (command: DoorCommand, percentage?: number) => {
      setIsCommandInFlight(true);
      setActiveCommand(command);

      try {
        await doorService.sendCommand(command, percentage ?? null);

        const descriptions: Record<DoorCommand, string> = {
          [DoorCommand.OPEN]: "Opening door...",
          [DoorCommand.CLOSE]: "Closing door...",
          [DoorCommand.STOP]: "Door stopped",
          [DoorCommand.VENT]: `Setting vent to ${percentage}%`,
        };

        toast({ title: "Command Sent", description: descriptions[command] });

        await refresh();
      } catch (err) {
        toast({
          title: "Error",
          description: err instanceof Error ? err.message : "Command failed",
          variant: "destructive",
        });
      } finally {
        setIsCommandInFlight(false);
        setActiveCommand(null);
      }
    },
    [refresh],
  );

  const lastSeenAtUtc = parseLastSeen(status.lastSeenAtUtc);

  const isDeviceOnline =
    lastSeenAtUtc !== null &&
    Date.now() - new Date(lastSeenAtUtc).getTime() < DEVICE_OFFLINE_AFTER_MS;

  const value = useMemo(
    () => ({
      status,
      apiError,
      isDeviceOnline,
      clientKind: status.lastClientKind ?? DeviceClientKind.Unknown,
      lastSeenAtUtc,
      isCommandInFlight,
      activeCommand,
      sendCommand,
      refresh,
    }),
    [status, apiError, isDeviceOnline, lastSeenAtUtc, isCommandInFlight, activeCommand, sendCommand, refresh],
  );

  return <DeviceStatusContext.Provider value={value}>{children}</DeviceStatusContext.Provider>;
}

export function useDeviceStatus() {
  const context = useContext(DeviceStatusContext);

  if (context === undefined) {
    throw new Error("useDeviceStatus must be used within a DeviceStatusProvider");
  }

  return context;
}
