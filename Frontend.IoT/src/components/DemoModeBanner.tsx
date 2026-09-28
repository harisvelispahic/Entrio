import { FlaskConical } from "lucide-react";
import { env } from "@/config/env";

/**
 * Permanent, unmissable notice that the data on screen is simulated.
 *
 * The point of demo mode is that a visitor can explore the UI without a backend. The
 * point of this banner is that they are never misled into thinking it is real -- which
 * is exactly what the old silent mock-data fallback did.
 */
export function DemoModeBanner() {
  if (!env.demoMode) return null;

  return (
    <div className="sticky top-0 z-50 flex items-center justify-center gap-2 bg-primary/15 px-4 py-2 text-center text-sm text-primary border-b border-primary/30">
      <FlaskConical className="h-4 w-4 shrink-0" />
      <span>
        <strong>Demo mode</strong> — no backend is connected. All data is simulated and
        resets when you reload.
      </span>
    </div>
  );
}
