import { SidebarProvider, SidebarTrigger } from '@/components/ui/sidebar';
import { AppSidebar } from './AppSidebar';
import { DeviceIndicator } from './DeviceIndicator';
import { Separator } from '@/components/ui/separator';
import { DeviceStatusProvider } from '@/contexts/DeviceStatusContext';

interface DashboardLayoutProps {
  children: React.ReactNode;
}

export function DashboardLayout({ children }: DashboardLayoutProps) {
  return (
    <SidebarProvider>
      {/* One poll of /door/status feeds the header, the sidebar footer and the dashboard. */}
      <DeviceStatusProvider>
      <div className="min-h-screen flex w-full bg-background">
        <AppSidebar />
        <div className="flex-1 flex flex-col">
          {/* Header */}
          <header className="h-14 flex items-center gap-4 border-b border-border px-4 lg:px-6">
            <SidebarTrigger className="-ml-1" />
            <Separator orientation="vertical" className="h-6" />
            <div className="flex-1" />
            <DeviceIndicator compact />
          </header>

          {/* Main content */}
          <main className="flex-1 p-4 lg:p-6 overflow-auto">
            {children}
          </main>
        </div>
      </div>
      </DeviceStatusProvider>
    </SidebarProvider>
  );
}
