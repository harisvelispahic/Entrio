import { api } from "./api";
import { DoorEvent } from "@/config/api";
import { env } from "@/config/env";
import { demoBackend } from "./demoBackend";

export const eventService = {
  getEvents(): Promise<DoorEvent[]> {
    if (env.demoMode) return Promise.resolve(demoBackend.getEvents());

    return api.get<DoorEvent[]>("/events");
  },
};
