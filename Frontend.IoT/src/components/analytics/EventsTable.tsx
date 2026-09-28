import { DoorEvent } from "@/config/api";
import {
  EVENT_SOURCE_BADGE,
  EVENT_SOURCE_BADGE_FALLBACK,
  eventSourceLabel,
  eventTypeLabel,
} from "@/config/eventLabels";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Badge } from "@/components/ui/badge";
import { ScrollArea } from "@/components/ui/scroll-area";
import { History } from "lucide-react";
import { format } from "date-fns";

interface EventsTableProps {
  events: DoorEvent[];
}

export function EventsTable({ events }: EventsTableProps) {
  return (
    <Card className="industrial-border">
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <History className="h-5 w-5 text-primary" />
          Recent Events
        </CardTitle>
      </CardHeader>
      <CardContent>
        <ScrollArea className="h-[400px]">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Time</TableHead>
                <TableHead>Event</TableHead>
                <TableHead>Source</TableHead>
                <TableHead>Details</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {events.map((event) => (
                <TableRow key={event.id} className="animate-fade-in">
                  <TableCell className="font-mono text-sm text-muted-foreground">
                    {format(new Date(event.timestamp), "MMM d, HH:mm")}
                  </TableCell>
                  <TableCell className="font-medium">{eventTypeLabel(event.eventType)}</TableCell>
                  <TableCell>
                    <Badge
                      variant="outline"
                      className={EVENT_SOURCE_BADGE[event.source] ?? EVENT_SOURCE_BADGE_FALLBACK}
                    >
                      {eventSourceLabel(event.source)}
                    </Badge>
                  </TableCell>
                  <TableCell className="text-muted-foreground">{event.details || "—"}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </ScrollArea>
      </CardContent>
    </Card>
  );
}
