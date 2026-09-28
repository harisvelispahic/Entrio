import { api } from "./api";
import { DoorCommand, ScheduleEntry } from "@/config/api";
import { env } from "@/config/env";
import { demoBackend } from "./demoBackend";

export interface CreateScheduleRequest {
  commandType: DoorCommand;
  /** Required for Vent, ignored otherwise. */
  targetPercentage?: number | null;
  /** ISO-8601 UTC instant. */
  opensAtUtc: string;
  /** ISO-8601 UTC instant, after opensAtUtc. */
  closesAtUtc: string;
}

export const scheduleService = {
  getSchedules(): Promise<ScheduleEntry[]> {
    if (env.demoMode) return Promise.resolve(demoBackend.getSchedules());

    return api.get<ScheduleEntry[]>("/schedules");
  },

  createSchedule(request: CreateScheduleRequest): Promise<ScheduleEntry> {
    if (env.demoMode) return Promise.resolve(demoBackend.createSchedule(request));

    return api.post<ScheduleEntry>("/schedules", request);
  },

  /** Deletes a period by its group id, which removes both halves. */
  deleteSchedule(groupId: string): Promise<void> {
    if (env.demoMode) {
      demoBackend.deleteSchedule(groupId);
      return Promise.resolve();
    }

    return api.delete(`/schedules/${groupId}`);
  },
};
