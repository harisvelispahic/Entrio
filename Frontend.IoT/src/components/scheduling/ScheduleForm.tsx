import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Slider } from '@/components/ui/slider';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { DoorCommand } from '@/config/api';
import { CalendarPlus, Loader2 } from 'lucide-react';

interface ScheduleFormProps {
  onSubmit: (command: DoorCommand, scheduledAt: string, percentage?: number) => Promise<void>;
  isLoading: boolean;
}

export function ScheduleForm({ onSubmit, isLoading }: ScheduleFormProps) {
  const [command, setCommand] = useState<string>('');
  const [dateTime, setDateTime] = useState('');
  const [percentage, setPercentage] = useState(50);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!command || !dateTime) return;

    const commandNum = parseInt(command) as DoorCommand;
    const isoDateTime = new Date(dateTime).toISOString();
    
    await onSubmit(
      commandNum,
      isoDateTime,
      commandNum === DoorCommand.VENT ? percentage : undefined
    );

    // Reset form
    setCommand('');
    setDateTime('');
    setPercentage(50);
  };

  const isVent = command === String(DoorCommand.VENT);
  // datetime-local works in LOCAL time, so the minimum must be local too. Using
  // toISOString() here put the floor in the past by the UTC offset.
  const now = new Date();
  const minDateTime = new Date(now.getTime() - now.getTimezoneOffset() * 60_000)
    .toISOString()
    .slice(0, 16);

  return (
    <Card className="industrial-border">
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <CalendarPlus className="h-5 w-5 text-primary" />
          Create Schedule
        </CardTitle>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="command">Command</Label>
            <Select value={command} onValueChange={setCommand}>
              <SelectTrigger className="bg-secondary/50">
                <SelectValue placeholder="Select command" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value={String(DoorCommand.OPEN)}>Open Door</SelectItem>
                <SelectItem value={String(DoorCommand.CLOSE)}>Close Door</SelectItem>
                <SelectItem value={String(DoorCommand.STOP)}>Stop Door</SelectItem>
                <SelectItem value={String(DoorCommand.VENT)}>Ventilate</SelectItem>
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-2">
            <Label htmlFor="datetime">Schedule Time</Label>
            <Input
              id="datetime"
              type="datetime-local"
              value={dateTime}
              onChange={(e) => setDateTime(e.target.value)}
              min={minDateTime}
              className="bg-secondary/50"
            />
          </div>

          {isVent && (
            <div className="space-y-3 p-3 rounded-lg bg-secondary/30 animate-fade-in">
              <div className="flex justify-between">
                <Label>Vent Percentage</Label>
                <span className="text-sm font-mono text-primary">{percentage}%</span>
              </div>
              <Slider
                value={[percentage]}
                onValueChange={(v) => setPercentage(v[0])}
                min={1}
                max={99}
                step={1}
              />
            </div>
          )}

          <Button
            type="submit"
            className="w-full"
            disabled={isLoading || !command || !dateTime}
          >
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
