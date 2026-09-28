import { useCallback, useEffect, useState } from "react";
import { scheduleService } from "@/services/scheduleService";
import { DoorCommand, ScheduleEntry } from "@/config/api";
import { toast } from "@/hooks/use-toast";

/** Chronological, soonest first. An auto-close has no opening half, so it sorts by its close. */
function byStart(a: ScheduleEntry, b: ScheduleEntry): number {
  const at = new Date(a.opensAtUtc ?? a.closesAtUtc).getTime();
  const bt = new Date(b.opensAtUtc ?? b.closesAtUtc).getTime();

  return at - bt;
}

export function useSchedules() {
  const [schedules, setSchedules] = useState<ScheduleEntry[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [isCreating, setIsCreating] = useState(false);
  const [isDeleting, setIsDeleting] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const fetchSchedules = useCallback(async () => {
    setIsLoading(true);

    try {
      const data = await scheduleService.getSchedules();
      setSchedules([...data].sort(byStart));
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
    async (
      commandType: DoorCommand,
      opensAtUtc: string,
      closesAtUtc: string,
      percentage?: number,
    ) => {
      setIsCreating(true);

      try {
        const created = await scheduleService.createSchedule({
          commandType,
          opensAtUtc,
          closesAtUtc,
          targetPercentage: percentage ?? null,
        });

        setSchedules((prev) => [...prev, created].sort(byStart));

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

  const deleteSchedule = useCallback(async (groupId: string) => {
    setIsDeleting(groupId);

    try {
      await scheduleService.deleteSchedule(groupId);
      setSchedules((prev) => prev.filter((s) => s.id !== groupId));

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
