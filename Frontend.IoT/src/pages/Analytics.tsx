import { useCallback, useEffect, useState } from "react";
import { EventsTable } from "@/components/analytics/EventsTable";
import { AnalyticsCharts } from "@/components/analytics/AnalyticsCharts";
import { eventService } from "@/services/eventService";
import { analyticsService, AnalyticsResponse } from "@/services/analyticsService";
import { DoorEvent } from "@/config/api";
import { Skeleton } from "@/components/ui/skeleton";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { AlertCircle } from "lucide-react";

const emptyAnalytics: AnalyticsResponse = {
  opensPerDay: [],
  openVsClosed: [],
  eventSources: [],
};

export default function Analytics() {
  const [events, setEvents] = useState<DoorEvent[]>([]);
  const [analytics, setAnalytics] = useState<AnalyticsResponse>(emptyAnalytics);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const [eventsData, analyticsData] = await Promise.all([
        eventService.getEvents(),
        analyticsService.get(),
      ]);

      setEvents(eventsData);
      setAnalytics(analyticsData);
    } catch (err) {
      // This page used to fall back to invented sample data on any failure, which made
      // an outage indistinguishable from real activity. It now says so plainly instead.
      setError(err instanceof Error ? err.message : "Failed to load analytics");
      setEvents([]);
      setAnalytics(emptyAnalytics);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  return (
    <div className="space-y-6 animate-fade-in">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Analytics</h1>
        <p className="text-muted-foreground">View door activity history and usage statistics</p>
      </div>

      {error && (
        <Alert variant="destructive">
          <AlertCircle className="h-4 w-4" />
          <AlertDescription className="flex items-center justify-between gap-4">
            <span>{error}</span>
            <Button variant="outline" size="sm" onClick={load}>
              Retry
            </Button>
          </AlertDescription>
        </Alert>
      )}

      {isLoading ? (
        <div className="space-y-6">
          <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
            {[...Array(3)].map((_, i) => (
              <Card key={i} className="industrial-border">
                <CardHeader>
                  <Skeleton className="h-6 w-32" />
                </CardHeader>
                <CardContent>
                  <Skeleton className="h-[200px] w-full" />
                </CardContent>
              </Card>
            ))}
          </div>
          <Card className="industrial-border">
            <CardHeader>
              <Skeleton className="h-6 w-40" />
            </CardHeader>
            <CardContent>
              <Skeleton className="h-[300px] w-full" />
            </CardContent>
          </Card>
        </div>
      ) : (
        <>
          <AnalyticsCharts
            opensPerDay={analytics.opensPerDay}
            openVsClosed={analytics.openVsClosed}
            eventSources={analytics.eventSources}
          />
          <EventsTable events={events} />
        </>
      )}
    </div>
  );
}
