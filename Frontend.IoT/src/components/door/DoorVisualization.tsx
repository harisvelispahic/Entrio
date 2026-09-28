import { DoorState, DoorStateLabels } from "@/config/api";
import { cn } from "@/lib/utils";

interface DoorVisualizationProps {
  position: number; // 0-100
  state: DoorState;
}

export function DoorVisualization({ position, state }: DoorVisualizationProps) {
  // The pulse is what tells the user the door is still travelling, so only these two
  // states may animate. A stopped door has to sit still or it reads as still moving.
  const isMoving = state === DoorState.Opening || state === DoorState.Closing;
  const isStopped = state === DoorState.Stopped;

  const doorHeight = 100 - position; // Invert: 0 position = 100% height (closed)

  // A door stopped part-way is described better by where it stopped than by the bare
  // word "stopped".
  const label =
    isStopped && position > 0 ? `Stopped at ${position}%` : DoorStateLabels[state];

  return (
    <div className="relative w-full max-w-xs mx-auto">
      {/* Garage frame */}
      <div className="relative aspect-[3/4] bg-secondary/30 rounded-lg industrial-border overflow-hidden">
        {/* Frame edges */}
        <div className="absolute inset-y-0 left-0 w-3 metal-texture" />
        <div className="absolute inset-y-0 right-0 w-3 metal-texture" />
        <div className="absolute top-0 inset-x-0 h-3 metal-texture" />

        {/* Interior (visible when door opens) */}
        <div className="absolute inset-3 bg-background/50 rounded-sm">
          {/* Interior details */}
          <div className="absolute inset-0 flex items-center justify-center">
            <div className="text-muted-foreground/30 text-4xl">🚗</div>
          </div>
        </div>

        {/* Door panels */}
        <div
          className={cn(
            "absolute inset-x-3 top-3 bg-secondary origin-top transition-transform duration-1000 ease-in-out",
            isMoving && "animate-pulse-glow"
          )}
          style={{
            height: `calc(${doorHeight}% - 0.75rem)`,
            transformOrigin: "top",
          }}
        >
          {/* Door segments */}
          <div className="h-full flex flex-col">
            {[...Array(4)].map((_, i) => (
              <div key={i} className="flex-1 border-b border-border/50 metal-texture flex items-center justify-center">
                {/* Panel detail */}
                <div className="w-3/4 h-1/2 bg-muted/30 rounded-sm" />
              </div>
            ))}
          </div>
        </div>

        {/* Status indicator light */}
        <div
          className={cn(
            "absolute top-1 right-1 w-3 h-3 rounded-full",
            state === DoorState.Open && "bg-success glow-success",
            state === DoorState.Closed && "bg-destructive",
            isMoving && "bg-warning glow-accent animate-pulse",
            isStopped && "bg-primary glow-primary",
            state === DoorState.Error && "bg-muted"
          )}
        />
      </div>

      {/* Position indicator */}
      <div className="mt-4 space-y-2">
        <div className="flex justify-between text-sm text-muted-foreground">
          <span>Position</span>
          <span className="font-mono">{position}%</span>
        </div>
        <div className="h-2 bg-secondary rounded-full overflow-hidden">
          <div
            className={cn(
              "h-full transition-all duration-500",
              state === DoorState.Open && "bg-success",
              state === DoorState.Closed && "bg-muted",
              isMoving && "bg-warning",
              isStopped && "bg-primary",
              state === DoorState.Error && "bg-muted"
            )}
            style={{ width: `${position}%` }}
          />
        </div>
      </div>

      {/* State label */}
      <div className="mt-3 text-center">
        <span
          className={cn(
            "inline-flex items-center px-3 py-1 rounded-full text-sm font-medium uppercase tracking-wide",
            state === DoorState.Open && "bg-success/20 text-success",
            state === DoorState.Closed && "bg-muted text-muted-foreground",
            isMoving && "bg-warning/20 text-warning animate-pulse",
            isStopped && "bg-primary/20 text-primary",
            state === DoorState.Error && "bg-muted text-muted-foreground"
          )}
        >
          {label}
        </span>
      </div>
    </div>
  );
}
