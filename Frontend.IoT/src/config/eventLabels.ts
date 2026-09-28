/**
 * Human-readable names for the device event enums.
 *
 * The API serializes DeviceEventType and DeviceEventSource as their C# enum NAMES, which
 * is right for a wire format but wrong on screen: "DoorClosed" is a database identifier,
 * not something to show a person.
 *
 * Keys are the exact enum names from DeviceEnums.cs. Anything unmapped falls back to the
 * raw value rather than an empty cell, so a newly added enum member degrades to something
 * readable instead of vanishing.
 */

const EVENT_TYPE_LABELS: Record<string, string> = {
  DoorOpened: "Door was opened",
  DoorClosed: "Door was closed",
  ObstacleDetected: "Obstacle detected",
  ObstacleCleared: "Obstacle cleared",
  AutoCloseTriggered: "Auto-close triggered",
  ScheduleTriggered: "Schedule triggered",
  ManualOpen: "Opened manually",
  ManualClose: "Closed manually",
};

const EVENT_SOURCE_LABELS: Record<string, string> = {
  Remote: "Web app",
  LocalRfid: "RFID card",
  Schedule: "Schedule",
  AutoClose: "Auto-close",
  System: "System",
};

/** Splits a PascalCase enum name into words, so an unmapped value still reads sensibly. */
function humanise(value: string): string {
  return value.replace(/([a-z])([A-Z])/g, "$1 $2");
}

export function eventTypeLabel(eventType: string): string {
  return EVENT_TYPE_LABELS[eventType] ?? humanise(eventType);
}

export function eventSourceLabel(source: string): string {
  return EVENT_SOURCE_LABELS[source] ?? humanise(source);
}

/**
 * Badge styling per source. Keyed by the real enum names — the previous map used keys
 * from the deleted mock data ("RFID", "Web", "Manual"), so all but Schedule fell through
 * to the same grey.
 */
export const EVENT_SOURCE_BADGE: Record<string, string> = {
  Remote: "bg-accent/20 text-accent border-accent/30",
  LocalRfid: "bg-primary/20 text-primary border-primary/30",
  Schedule: "bg-success/20 text-success border-success/30",
  AutoClose: "bg-warning/20 text-warning border-warning/30",
  System: "bg-muted text-muted-foreground border-border",
};

export const EVENT_SOURCE_BADGE_FALLBACK = "bg-muted text-muted-foreground border-border";
