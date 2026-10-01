'use client';

import { usePathname } from 'next/navigation';
import Sidebar from '@/components/Sidebar';
import AuthGuard from '@/components/AuthGuard';

export default function AppLayout({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const esRutaSinSidebar = pathname === '/login' || pathname === '/marcar';

  return (
    <div className="flex min-h-screen">
      <Sidebar />
      <main className={`flex-1 flex flex-col min-w-0 ${esRutaSinSidebar ? '' : 'md:pl-64'}`}>
        <AuthGuard>{children}</AuthGuard>
      </main>
    </div>
  );
}