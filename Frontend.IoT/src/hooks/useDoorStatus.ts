import { useCallback, useEffect, useRef, useState } from "react";
import { doorService } from "@/services/doorService";
import {
  DOOR_STATUS_POLL_INTERVAL,
  DoorCommand,
  DoorState,
  DoorStatus,
} from "@/config/api";
import { toast } from "@/hooks/use-toast";

export function useDoorStatus() {
  const [status, setStatus] = useState<DoorStatus>({
    position: 0,
    state: DoorState.Closed,
  });
  const [isLoading, setIsLoading] = useState(false);
  const [activeCommand, setActiveCommand] = useState<DoorCommand | null>(null);
  const [error, setError] = useState<string | null>(null);

  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const fetchStatus = useCallback(async () => {
    try {
      setStatus(await doorService.getStatus());
      setError(null);
    } catch (err) {
      // A 401 is handled centrally by the API layer (refresh, then session expiry),
      // so anything reaching here is a real failure worth showing.
      setError(err instanceof Error ? err.message : "Failed to fetch status");
    }
  }, []);

  useEffect(() => {
    fetchStatus();

    intervalRef.current = setInterval(fetchStatus, DOOR_STATUS_POLL_INTERVAL);

    return () => {
      if (intervalRef.current) clearInterval(intervalRef.current);
    };
  }, [fetchStatus]);

  const sendCommand = useCallback(
    async (command: DoorCommand, percentage?: number) => {
      setIsLoading(true);
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

        await fetchStatus();
      } catch (err) {
        toast({
          title: "Error",
          description: err instanceof Error ? err.message : "Command failed",
          variant: "destructive",
        });
      } finally {
        setIsLoading(false);
        setActiveCommand(null);
      }
    },
    [fetchStatus],
  );

  return { status, isLoading, activeCommand, error, sendCommand, refreshStatus: fetchStatus };
}
