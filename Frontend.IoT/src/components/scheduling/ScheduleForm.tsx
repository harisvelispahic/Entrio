import { useState } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Label } from '@/components/ui/label';
import { Slider } from '@/components/ui/slider';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { CalendarPlus, Loader2, AlertCircle } from 'lucide-react';
import { DoorCommand } from '@/config/api';
import { DateTimePicker } from './DateTimePicker';

interface ScheduleFormProps {
  onSubmit: (
    command: DoorCommand,
    opensAtUtc: string,
    closesAtUtc: string,
    percentage?: number,
  ) => Promise<void>;
  isLoading: boolean;
}

export function ScheduleForm({ onSubmit, isLoading }: ScheduleFormProps) {
  const [command, setCommand] = useState<string>('');
  const [opensAt, setOpensAt] = useState<Date | null>(null);
  const [closesAt, setClosesAt] = useState<Date | null>(null);
  const [percentage, setPercentage] = useState(50);
  const [error, setError] = useState('');

  const isVent = command === String(DoorCommand.VENT);
  const now = new Date();

  const validate = (): string => {
    if (!command) return 'Choose what the door should do.';
    if (!opensAt) return 'Choose when the door opens.';
    if (!closesAt) return 'Choose when the door closes.';
    if (opensAt <= now) return 'The opening time must be in the future.';
    if (closesAt <= opensAt) return 'The closing time must be after the opening time.';

    return '';
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    const message = validate();
    setError(message);

    if (message) return;

    const commandNum = parseInt(command, 10) as DoorCommand;

    // The single point where local time becomes a UTC instant. toISOString uses the
    // offset actually in force on the chosen date, so DST is handled here for free.
    await onSubmit(
      commandNum,
      opensAt!.toISOString(),
      closesAt!.toISOString(),
      commandNum === DoorCommand.VENT ? percentage : undefined,
    );

    setCommand('');
    setOpensAt(null);
    setClosesAt(null);
    setPercentage(50);
  };

  return (
    <Card className="industrial-border">
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <CalendarPlus className="h-5 w-5 text-primary" />
          New Schedule
        </CardTitle>
        <CardDescription>
          Schedule a period with the door open. A closing time is required, so the door is
          never left open by a forgotten schedule.
        </CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit} className="space-y-4">
          {error && (
            <Alert variant="destructive">
              <AlertCircle className="h-4 w-4" />
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          )}

          <div className="space-y-2">
            <Label htmlFor="command">Action</Label>
            {/* Only Open and Vent: a bare Close is not a user action, and Stop makes no
                sense on a door that is not moving. */}
            <Select value={command} onValueChange={setCommand}>
              <SelectTrigger id="command" className="bg-secondary/50">
                <SelectValue placeholder="Select an action" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value={String(DoorCommand.OPEN)}>Open Door</SelectItem>
                <SelectItem value={String(DoorCommand.VENT)}>Ventilate</SelectItem>
              </SelectContent>
            </Select>
          </div>

          {isVent && (
            <div className="space-y-2">
              <div className="flex items-center justify-between">
                <Label htmlFor="percentage">Vent opening</Label>
                <span className="font-mono text-sm text-primary">{percentage}%</span>
              </div>
              <Slider
                id="percentage"
                min={1}
                max={99}
                step={1}
                value={[percentage]}
                onValueChange={([next]) => setPercentage(next)}
                disabled={isLoading}
              />
            </div>
          )}

          <DateTimePicker
            id="opens-at"
            label="Opens at"
            value={opensAt}
            onChange={setOpensAt}
            min={now}
            disabled={isLoading}
          />

          <DateTimePicker
            id="closes-at"
            label="Closes at"
            value={closesAt}
            onChange={setClosesAt}
            min={opensAt ?? now}
            disabled={isLoading}
          />

          <Button type="submit" className="w-full" disabled={isLoading}>
            {isLoading ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                Creating...
              </>
            ) : (
              <>
                <CalendarPlus className="mr-2 h-4 w-4" />
                Create Schedule
              </>
            )}
          </Button>
        </form>
      </CardContent>
    </Card>
  );
}
