import { api } from "./api";
import { CommandRequest, DoorCommand, DoorStatus } from "@/config/api";
import { env } from "@/config/env";
import { demoBackend } from "./demoBackend";

export const doorService = {
  getStatus(): Promise<DoorStatus> {
    if (env.demoMode) return Promise.resolve(demoBackend.getDoorStatus());

    return api.get<DoorStatus>("/door/status");
  },

  sendCommand(command: DoorCommand, percentage: number | null): Promise<void> {
    if (env.demoMode) {
      demoBackend.sendCommand(command, percentage);
      return Promise.resolve();
    }

    const request: CommandRequest = { command, percentage };
    return api.post("/door/command", request);
  },
};
