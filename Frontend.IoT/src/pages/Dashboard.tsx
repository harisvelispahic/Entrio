import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { DoorVisualization } from "@/components/door/DoorVisualization";
import { DoorControls } from "@/components/door/DoorControls";
import { AutoCloseCard } from "@/components/door/AutoCloseCard";
import { useDoorStatus } from "@/hooks/useDoorStatus";
import { Activity, Clock, AlertTriangle } from "lucide-react";
import { formatDistanceToNow } from "date-fns";
import { DoorState, DoorStateLabels } from "@/config/api";

export default function Dashboard() {
  const { status, isLoading, activeCommand, error, sendCommand } = useDoorStatus();

  return (
    <div className="space-y-6 animate-fade-in">
      {/* Page header */}
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Dashboard</h1>
        <p className="text-muted-foreground">Monitor and control your garage door in real-time</p>
      </div>

      {/* Error banner */}
      {error && (
        <Card className="border-warning bg-warning/10">
          <CardContent className="flex items-center gap-3 py-3">
            <AlertTriangle className="h-5 w-5 text-warning" />
            <span className="text-sm text-warning">{error}</span>
          </CardContent>
        </Card>
      )}

      {/* Main grid */}
      <div className="grid gap-6 lg:grid-cols-2">
        {/* Door visualization */}
        <Card className="industrial-border">
          <CardHeader>
            <CardTitle className="flex items-center justify-between">
              <span className="flex items-center gap-2">
                <Activity className="h-5 w-5 text-primary" />
                Live Status
              </span>
              <span className="text-xs font-normal text-muted-foreground flex items-center gap-1">
                <Clock className="h-3 w-3" />
                {status.lastUpdated
                  ? formatDistanceToNow(new Date(status.lastUpdated), { addSuffix: true })
                  : "Polling every 3s"}
              </span>
            </CardTitle>
          </CardHeader>
          <CardContent>
            <DoorVisualization position={status.position} state={status.state} />
          </CardContent>
        </Card>

        {/* Controls */}
        <div className="space-y-6">
          <DoorControls onCommand={sendCommand} isLoading={isLoading} activeCommand={activeCommand} />
          <AutoCloseCard />
        </div>
      </div>

      {/* Quick stats */}
      <div className="grid gap-4 md:grid-cols-3">
        <Card className="industrial-border">
          <CardContent className="pt-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-muted-foreground">Current Position</p>
                <p className="text-2xl font-bold font-mono text-primary">{status.position}%</p>
              </div>
              <div className="h-12 w-12 rounded-full bg-primary/10 flex items-center justify-center">
                <Activity className="h-6 w-6 text-primary" />
              </div>
            </div>
          </CardContent>
        </Card>

        <Card className="industrial-border">
          <CardContent className="pt-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-muted-foreground">Door State</p>
                {status.obstacle ? (
                  <span className="text-red-500 text-2xl font-semibold">Obstacle detected</span>
                ) : (
                  <p className="text-2xl font-bold capitalize">
                    {status.state === DoorState.Stopped && status.position > 0
                      ? `Stopped at ${status.position}%`
                      : DoorStateLabels[status.state]}
                  </p>
                )}
              </div>
              <div
                className={`h-12 w-12 rounded-full flex items-center justify-center ${
                  status.state === DoorState.Open
                    ? "bg-success/10"
                    : status.state === DoorState.Closed
                    ? "bg-muted"
                    : status.state === DoorState.Opening || status.state === DoorState.Closing
                    ? "bg-warning/10"
                    : "bg-primary/10"
                }`}
              >
                <div
                  className={`h-3 w-3 rounded-full ${
                    status.state === DoorState.Open
                      ? "bg-success"
                      : status.state === DoorState.Closed
                      ? "bg-muted-foreground"
                      : status.state === DoorState.Opening || status.state === DoorState.Closing
                      ? "bg-warning animate-pulse"
                      : "bg-primary"
                  }`}
                />
              </div>
            </div>
          </CardContent>
        </Card>

        <Card className="industrial-border">
          <CardContent className="pt-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-muted-foreground">Connection</p>
                {/* Derived from the polling result. This used to be hardcoded to
                    "Online" and stayed green even while every request was failing. */}
                <p className={`text-2xl font-bold ${error ? "text-destructive" : "text-success"}`}>
                  {error ? "Offline" : "Online"}
                </p>
              </div>
              <div
                className={`h-12 w-12 rounded-full flex items-center justify-center ${
                  error ? "bg-destructive/10" : "bg-success/10"
                }`}
              >
                <div
                  className={`h-3 w-3 rounded-full ${
                    error ? "bg-destructive" : "bg-success animate-pulse"
                  }`}
                />
              </div>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
