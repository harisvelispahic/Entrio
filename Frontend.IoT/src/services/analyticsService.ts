import { api } from "./api";
import { env } from "@/config/env";
import { eventSourceLabel } from "@/config/eventLabels";
import { demoBackend } from "./demoBackend";

const openClosedColors: Record<string, string> = {
  Open: "hsl(185, 70%, 50%)",
  Closed: "hsl(220, 15%, 35%)",
};

/** Keys match the DeviceEventSource enum names the API returns. */
const sourceColors: Record<string, string> = {
  Remote: "hsl(35, 90%, 55%)",
  System: "hsl(145, 70%, 45%)",
  LocalRfid: "hsl(185, 70%, 50%)",
  Schedule: "hsl(265, 60%, 60%)",
  AutoClose: "hsl(220, 15%, 55%)",
};

const fallbackColor = "hsl(0, 0%, 55%)";

export interface AnalyticsResponse {
  opensPerDay: { day: string; opens: number }[];
  openVsClosed: { name: string; value: number; color: string }[];
  eventSources: { name: string; value: number; color: string }[];
}

export const analyticsService = {
  async get(): Promise<AnalyticsResponse> {
    // Deliberately no try/catch. This used to swallow every failure and return
    // fabricated numbers, so an outage or a 401 looked like real data. Errors now
    // propagate and the page shows an error state.
    const raw = env.demoMode
      ? demoBackend.getAnalytics()
      : await api.get<AnalyticsResponse>("/analytics");

    return {
      ...raw,
      openVsClosed: raw.openVsClosed.map((x) => ({
        ...x,
        color: openClosedColors[x.name] ?? fallbackColor,
      })),
      eventSources: raw.eventSources.map((x) => ({
        ...x,
        // Colour is looked up by the raw enum name, but the slice is LABELLED with the
        // readable one -- so the chart legend matches the events table.
        name: eventSourceLabel(x.name),
        color: sourceColors[x.name] ?? fallbackColor,
      })),
    };
  },
};
