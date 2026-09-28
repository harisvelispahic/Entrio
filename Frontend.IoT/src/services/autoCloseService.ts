import { api } from "./api";
import { env } from "@/config/env";
import { demoBackend } from "./demoBackend";

export interface AutoCloseSettings {
  enabled: boolean;
  afterSeconds: number;
}

export const autoCloseService = {
  get(): Promise<AutoCloseSettings> {
    if (env.demoMode) return Promise.resolve(demoBackend.getAutoClose());

    return api.get<AutoCloseSettings>("/auto-close");
  },

  update(settings: AutoCloseSettings): Promise<AutoCloseSettings> {
    if (env.demoMode) return Promise.resolve(demoBackend.setAutoClose(settings));

    return api.put<AutoCloseSettings>("/auto-close", settings);
  },
};
