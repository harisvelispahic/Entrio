import { useMemo, useState } from "react";
import { format } from "date-fns";
import { CalendarIcon } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Calendar } from "@/components/ui/calendar";
import { Label } from "@/components/ui/label";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { ScrollArea } from "@/components/ui/scroll-area";
import { cn } from "@/lib/utils";

interface DateTimePickerProps {
  id: string;
  label: string;
  /** Local Date, or null while nothing is chosen. */
  value: Date | null;
  onChange: (value: Date | null) => void;
  /** Earliest selectable instant; earlier days are disabled outright. */
  min?: Date;
  disabled?: boolean;
}

/** Minutes are offered in five-minute steps: finer precision has no meaning for a garage door. */
const MINUTE_STEP = 5;

const HOURS = Array.from({ length: 24 }, (_, hour) => hour);
const MINUTES = Array.from({ length: 60 / MINUTE_STEP }, (_, i) => i * MINUTE_STEP);

const pad = (value: number) => String(value).padStart(2, "0");

/**
 * Date and time picker: a calendar and two time columns in a single popover.
 *
 * Replaces the browser's native datetime-local and time controls, which render as
 * whatever the OS provides and look nothing like the rest of the UI.
 *
 * Columns rather than a clock face on purpose. A clock face is good with a thumb and
 * fiddly with a mouse — it needs two separate gestures (hour, then minute) and an
 * imprecise drag. Two scrollable lists are one click each, readable at a glance, and
 * keyboard-navigable.
 *
 * Everything here is LOCAL time. The conversion to a UTC instant happens once, at submit,
 * which is what keeps daylight saving correct: the browser knows the offset actually in
 * force on the chosen date.
 */
export function DateTimePicker({
  id,
  label,
  value,
  onChange,
  min,
  disabled,
}: DateTimePickerProps) {
  const [open, setOpen] = useState(false);

  const selectedHour = value?.getHours() ?? null;
  // Snapped to the step so the highlight lands on a row that exists, even if the value
  // came from somewhere that did not round.
  const selectedMinute =
    value === null ? null : Math.floor(value.getMinutes() / MINUTE_STEP) * MINUTE_STEP;

  // Compared by day so "today" stays selectable once the minimum has passed this morning.
  const minDay = useMemo(() => (min ? new Date(min.toDateString()) : null), [min]);

  const withDate = (date: Date) => {
    const next = new Date(date);
    // Default to 09:00 rather than midnight, which is rarely what anyone means.
    next.setHours(value?.getHours() ?? 9, value?.getMinutes() ?? 0, 0, 0);

    return next;
  };

  const withTime = (hour: number, minute: number) => {
    const next = new Date(value ?? new Date());
    next.setHours(hour, minute, 0, 0);

    return next;
  };

  return (
    <div className="space-y-2">
      <Label htmlFor={id}>{label}</Label>

      <Popover open={open} onOpenChange={setOpen}>
        <PopoverTrigger asChild>
          <Button
            id={id}
            type="button"
            variant="outline"
            disabled={disabled}
            className={cn(
              "w-full justify-start bg-secondary/50 font-normal",
              !value && "text-muted-foreground",
            )}
          >
            <CalendarIcon className="mr-2 h-4 w-4 shrink-0" />
            {value ? format(value, "EEE d MMM yyyy 'at' HH:mm") : "Pick a date and time"}
          </Button>
        </PopoverTrigger>

        <PopoverContent className="w-auto p-0" align="start">
          <div className="flex flex-col sm:flex-row">
            <Calendar
              mode="single"
              selected={value ?? undefined}
              onSelect={(date) => date && onChange(withDate(date))}
              disabled={minDay ? (date) => date < minDay : undefined}
              initialFocus
            />

            <div className="flex border-t sm:border-t-0 sm:border-l border-border">
              <TimeColumn
                unit="Hour"
                values={HOURS}
                selected={selectedHour}
                onSelect={(hour) => onChange(withTime(hour, selectedMinute ?? 0))}
              />
              <TimeColumn
                unit="Min"
                values={MINUTES}
                selected={selectedMinute}
                onSelect={(minute) => onChange(withTime(selectedHour ?? 9, minute))}
              />
            </div>
          </div>
        </PopoverContent>
      </Popover>
    </div>
  );
}

interface TimeColumnProps {
  unit: string;
  values: number[];
  selected: number | null;
  onSelect: (value: number) => void;
}

function TimeColumn({ unit, values, selected, onSelect }: TimeColumnProps) {
  return (
    <div className="flex flex-col">
      <span className="px-3 pt-3 pb-1 text-xs font-medium text-muted-foreground text-center">
        {unit}
      </span>
      {/* Fixed height so both columns scroll independently and the popover keeps the
          calendar's height rather than stretching to 24 rows. */}
      <ScrollArea className="h-[232px] w-16">
        <div className="flex flex-col gap-1 p-2">
          {values.map((entry) => (
            <Button
              key={entry}
              type="button"
              size="sm"
              variant={selected === entry ? "default" : "ghost"}
              className="h-8 w-full font-mono text-sm"
              onClick={() => onSelect(entry)}
            >
              {pad(entry)}
            </Button>
          ))}
        </div>
      </ScrollArea>
    </div>
  );
}
