import { api } from "./api";
import { DoorCommand, Schedule } from "@/config/api";
import { env } from "@/config/env";
import { demoBackend } from "./demoBackend";

export interface CreateScheduleRequest {
  command: DoorCommand;
  /** ISO-8601 UTC instant. */
  executeAtUtc: string;
  percentage?: number;
}

export const scheduleService = {
  getSchedules(): Promise<Schedule[]> {
    if (env.demoMode) return Promise.resolve(demoBackend.getSchedules());

    return api.get<Schedule[]>("/schedules");
  },

  createSchedule(schedule: CreateScheduleRequest): Promise<Schedule> {
    if (env.demoMode) {
      return Promise.resolve(
        demoBackend.createSchedule(schedule.command, schedule.executeAtUtc, schedule.percentage),
      );
    }

    return api.post<Schedule>("/schedules", {
      commandType: schedule.command,
      targetPercentage: schedule.percentage ?? null,
      executeAtUtc: schedule.executeAtUtc,
    });
  },

  deleteSchedule(id: string): Promise<void> {
    if (env.demoMode) {
      demoBackend.deleteSchedule(id);
      return Promise.resolve();
    }

    return api.delete(`/schedules/${id}`);
  },
};
