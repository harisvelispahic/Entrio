import { useEffect, useState } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Label } from "@/components/ui/label";
import { Input } from "@/components/ui/input";
import { Switch } from "@/components/ui/switch";
import { Button } from "@/components/ui/button";
import { Timer, Loader2 } from "lucide-react";
import { autoCloseService } from "@/services/autoCloseService";
import { toast } from "@/hooks/use-toast";

const MIN_SECONDS = 5;
const MAX_SECONDS = 3600;

/**
 * Controls the automatic close that follows an open.
 *
 * The backend arms it by writing a schedule, which ScheduleWorker later turns into a
 * close command, so the delay survives an API restart.
 */
export function AutoCloseCard() {
  const [enabled, setEnabled] = useState(false);
  const [afterSeconds, setAfterSeconds] = useState(30);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);

  useEffect(() => {
    autoCloseService
      .get()
      .then((settings) => {
        setEnabled(settings.enabled);
        setAfterSeconds(settings.afterSeconds);
      })
      .catch(() => {
        // Non-fatal: the card just shows its defaults rather than breaking the dashboard.
      })
      .finally(() => setIsLoading(false));
  }, []);

  const save = async (nextEnabled: boolean, nextSeconds: number) => {
    if (nextSeconds < MIN_SECONDS || nextSeconds > MAX_SECONDS) {
      toast({
        title: "Invalid delay",
        description: `Delay must be between ${MIN_SECONDS} and ${MAX_SECONDS} seconds.`,
        variant: "destructive",
      });
      return;
    }

    setIsSaving(true);

    try {
      const saved = await autoCloseService.update({ enabled: nextEnabled, afterSeconds: nextSeconds });
      setEnabled(saved.enabled);
      setAfterSeconds(saved.afterSeconds);

      toast({
        title: "Auto-close updated",
        description: saved.enabled
          ? `The door will close ${saved.afterSeconds}s after opening.`
          : "Auto-close is off.",
      });
    } catch (err) {
      toast({
        title: "Error",
        description: err instanceof Error ? err.message : "Failed to update auto-close",
        variant: "destructive",
      });
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <Card className="industrial-border">
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Timer className="h-5 w-5 text-primary" />
          Auto-Close
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="flex items-center justify-between">
          <div className="space-y-0.5">
            <Label htmlFor="auto-close-enabled">Close automatically after opening</Label>
            <p className="text-sm text-muted-foreground">
              Applies to manual and RFID opens, not scheduled ones.
            </p>
          </div>
          <Switch
            id="auto-close-enabled"
            checked={enabled}
            disabled={isLoading || isSaving}
            onCheckedChange={(checked) => save(checked, afterSeconds)}
          />
        </div>

        <div className="flex items-end gap-2">
          <div className="flex-1 space-y-2">
            <Label htmlFor="auto-close-delay">Delay (seconds)</Label>
            <Input
              id="auto-close-delay"
              type="number"
              min={MIN_SECONDS}
              max={MAX_SECONDS}
              value={afterSeconds}
              disabled={isLoading || isSaving}
              onChange={(e) => setAfterSeconds(Number(e.target.value))}
              className="bg-secondary/50"
            />
          </div>
          <Button
            variant="outline"
            disabled={isLoading || isSaving}
            onClick={() => save(enabled, afterSeconds)}
          >
            {isSaving ? <Loader2 className="h-4 w-4 animate-spin" /> : "Save"}
          </Button>
        </div>
      </CardContent>
    </Card>
  );
}
