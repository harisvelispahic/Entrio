import { DoorCommand, ScheduleEntry, ScheduleEntryKind } from "@/config/api";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Calendar, DoorOpen, Wind, Trash2, Clock, Timer, ArrowRight } from "lucide-react";
import { format, isSameDay } from "date-fns";

interface ScheduleListProps {
  schedules: ScheduleEntry[];
  onDelete: (groupId: string) => Promise<void>;
  isDeleting: string | null;
}

/** Only Open and Vent can be scheduled, so only those two need an icon and a label. */
const actionIcons: Partial<Record<DoorCommand, React.ReactNode>> = {
  [DoorCommand.OPEN]: <DoorOpen className="h-4 w-4 text-success" />,
  [DoorCommand.VENT]: <Wind className="h-4 w-4 text-primary" />,
};

const actionLabels: Partial<Record<DoorCommand, string>> = {
  [DoorCommand.OPEN]: "Open",
  [DoorCommand.VENT]: "Vent",
};

/**
 * Renders the closing time relative to the opening time: a period that ends the same day
 * only needs the time, not the whole date again.
 */
function formatPeriod(opensAt: Date, closesAt: Date): string {
  const opens = format(opensAt, "EEE d MMM, HH:mm");
  const closes = isSameDay(opensAt, closesAt)
    ? format(closesAt, "HH:mm")
    : format(closesAt, "EEE d MMM, HH:mm");

  return `${opens} → ${closes}`;
}

export function ScheduleList({ schedules, onDelete, isDeleting }: ScheduleListProps) {
  if (schedules.length === 0) {
    return (
      <Card className="industrial-border">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Calendar className="h-5 w-5 text-primary" />
            Upcoming Schedules
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="text-center py-8 text-muted-foreground">
            <Clock className="h-12 w-12 mx-auto mb-3 opacity-30" />
            <p>No schedules created yet</p>
            <p className="text-sm">Create a schedule to automate your door</p>
          </div>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card className="industrial-border">
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Calendar className="h-5 w-5 text-primary" />
          Upcoming Schedules ({schedules.length})
        </CardTitle>
      </CardHeader>
      <CardContent>
        <div className="space-y-3">
          {schedules.map((entry) => {
            // An auto-close is raised by the system, not created here, so it is shown for
            // information and cannot be deleted from this list.
            const isAutoClose = entry.kind === ScheduleEntryKind.AutoClose;

            return (
              <div
                key={entry.id}
                className="flex items-center justify-between p-3 rounded-lg bg-secondary/30 border border-border animate-fade-in"
              >
                <div className="flex items-center gap-3">
                  <div className="h-10 w-10 rounded-full bg-secondary flex items-center justify-center shrink-0">
                    {isAutoClose ? (
                      <Timer className="h-4 w-4 text-muted-foreground" />
                    ) : (
                      actionIcons[entry.commandType]
                    )}
                  </div>
                  <div>
                    {isAutoClose ? (
                      <>
                        <p className="font-medium flex items-center gap-2">
                          Auto-close
                          <Badge variant="outline" className="text-xs font-normal">
                            automatic
                          </Badge>
                        </p>
                        <p className="text-sm text-muted-foreground">
                          Closes at {format(new Date(entry.closesAtUtc), "HH:mm:ss")}
                        </p>
                      </>
                    ) : (
                      <>
                        <p className="font-medium">
                          {actionLabels[entry.commandType] ?? "Scheduled"}
                          {entry.commandType === DoorCommand.VENT && entry.targetPercentage && (
                            <span className="text-primary ml-1">({entry.targetPercentage}%)</span>
                          )}
                        </p>
                        <p className="text-sm text-muted-foreground flex items-center gap-1">
                          {entry.opensAtUtc
                            ? formatPeriod(new Date(entry.opensAtUtc), new Date(entry.closesAtUtc))
                            : format(new Date(entry.closesAtUtc), "EEE d MMM, HH:mm")}
                        </p>
                      </>
                    )}
                  </div>
                </div>

                {!isAutoClose && (
                  <Button
                    variant="ghost"
                    size="sm"
                    aria-label="Delete schedule"
                    className="text-destructive hover:text-destructive hover:bg-destructive/10"
                    onClick={() => onDelete(entry.id)}
                    disabled={isDeleting === entry.id}
                  >
                    <Trash2 className="h-4 w-4" />
                  </Button>
                )}
              </div>
            );
          })}
        </div>
      </CardContent>
    </Card>
  );
}
