import { formatDistanceToNow } from "date-fns";
import { Cpu, FlaskConical, HelpCircle, WifiOff } from "lucide-react";
import { DeviceClientKind } from "@/config/api";
import { useDeviceStatus } from "@/contexts/DeviceStatusContext";
import { cn } from "@/lib/utils";

/**
 * States what is actually on the other end of the connection, and when it last said so.
 *
 * Replaces a hardcoded "ESP32 Connected" that was true regardless of whether anything was
 * attached. The kind comes from the device's own check-in: the simulator sends an
 * X-Device-Client header, the firmware sends none, so hardware needs no change to be
 * reported correctly.
 */
export function DeviceIndicator({ compact = false }: { compact?: boolean }) {
  const { isDeviceOnline, clientKind, lastSeenAtUtc, apiError } = useDeviceStatus();

  // An unreachable API tells us nothing about the device, so say that rather than
  // reporting the device as offline on the strength of a failed request.
  if (apiError) {
    return (
      <Indicator
        icon={<WifiOff className="h-4 w-4" />}
        tone="destructive"
        title="Backend unreachable"
        detail={compact ? undefined : "Cannot reach the API"}
      />
    );
  }

  if (!isDeviceOnline) {
    return (
      <Indicator
        icon={<WifiOff className="h-4 w-4" />}
        tone="muted"
        title="No device"
        detail={
          lastSeenAtUtc
            ? `Last seen ${formatDistanceToNow(new Date(lastSeenAtUtc), { addSuffix: true })}`
            : "Never reported in"
        }
      />
    );
  }

  if (clientKind === DeviceClientKind.Simulator) {
    return (
      <Indicator
        icon={<FlaskConical className="h-4 w-4" />}
        tone="primary"
        title="Simulated device"
        detail={compact ? undefined : "No hardware attached"}
      />
    );
  }

  if (clientKind === DeviceClientKind.Hardware) {
    return (
      <Indicator
        icon={<Cpu className="h-4 w-4" />}
        tone="success"
        title="ESP32 connected"
        detail={compact ? undefined : "Hardware reporting"}
      />
    );
  }

  return (
    <Indicator
      icon={<HelpCircle className="h-4 w-4" />}
      tone="muted"
      title="Unknown device"
      detail={compact ? undefined : "Reported without identifying itself"}
    />
  );
}

const tones = {
  success: "text-success",
  primary: "text-primary",
  destructive: "text-destructive",
  muted: "text-muted-foreground",
} as const;

interface IndicatorProps {
  icon: React.ReactNode;
  tone: keyof typeof tones;
  title: string;
  detail?: string;
}

function Indicator({ icon, tone, title, detail }: IndicatorProps) {
  return (
    <div className="flex items-center gap-2 text-sm min-w-0">
      <span className={cn("shrink-0", tones[tone])}>{icon}</span>
      <div className="min-w-0">
        <p className={cn("truncate leading-tight", tones[tone])}>{title}</p>
        {detail && <p className="text-xs text-muted-foreground truncate leading-tight">{detail}</p>}
      </div>
    </div>
  );
}
