import { useCallback, useEffect, useState } from "react";
import { scheduleService } from "@/services/scheduleService";
import { DoorCommand, Schedule } from "@/config/api";
import { toast } from "@/hooks/use-toast";

/** Chronological, soonest first. */
function byExecuteAt(a: Schedule, b: Schedule): number {
  return new Date(a.executeAtUtc).getTime() - new Date(b.executeAtUtc).getTime();
}

export function useSchedules() {
  const [schedules, setSchedules] = useState<Schedule[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [isCreating, setIsCreating] = useState(false);
  const [isDeleting, setIsDeleting] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const fetchSchedules = useCallback(async () => {
    setIsLoading(true);

    try {
      const data = await scheduleService.getSchedules();
      setSchedules([...data].sort(byExecuteAt));
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load schedules");
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchSchedules();
  }, [fetchSchedules]);

  const createSchedule = useCallback(
    async (command: DoorCommand, executeAtUtc: string, percentage?: number) => {
      setIsCreating(true);

      try {
        const created = await scheduleService.createSchedule({ command, executeAtUtc, percentage });
        setSchedules((prev) => [...prev, created].sort(byExecuteAt));

        toast({
          title: "Schedule Created",
          description: "Your schedule has been created successfully.",
        });
      } catch (err) {
        toast({
          title: "Error",
          description: err instanceof Error ? err.message : "Failed to create schedule",
          variant: "destructive",
        });
      } finally {
        setIsCreating(false);
      }
    },
    [],
  );

  const deleteSchedule = useCallback(async (id: string) => {
    setIsDeleting(id);

    try {
      await scheduleService.deleteSchedule(id);
      setSchedules((prev) => prev.filter((s) => s.id !== id));

      toast({ title: "Schedule Deleted", description: "Your schedule has been deleted." });
    } catch (err) {
      toast({
        title: "Error",
        description: err instanceof Error ? err.message : "Failed to delete schedule",
        variant: "destructive",
      });
    } finally {
      setIsDeleting(null);
    }
  }, []);

  return {
    schedules,
    isLoading,
    isCreating,
    isDeleting,
    error,
    createSchedule,
    deleteSchedule,
    refreshSchedules: fetchSchedules,
  };
}
