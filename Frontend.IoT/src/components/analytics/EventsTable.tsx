import { DoorEvent } from "@/config/api";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Badge } from "@/components/ui/badge";
import { ScrollArea } from "@/components/ui/scroll-area";
import { History } from "lucide-react";
import { format } from "date-fns";

interface EventsTableProps {
  events: DoorEvent[];
}

const sourceColors: Record<string, string> = {
  RFID: "bg-primary/20 text-primary border-primary/30",
  Web: "bg-accent/20 text-accent border-accent/30",
  Schedule: "bg-success/20 text-success border-success/30",
  Manual: "bg-muted text-muted-foreground border-border",
};

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
                  <TableCell className="font-medium">{event.eventType}</TableCell>
                  <TableCell>
                    <Badge variant="outline" className={sourceColors[event.source] || sourceColors.Manual}>
                      {event.source}
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
