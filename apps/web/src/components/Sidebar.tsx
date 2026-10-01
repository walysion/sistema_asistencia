'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { 
  LayoutDashboard, 
  Users, 
  Clock, 
  MapPin, 
  FileText, 
  BarChart3, 
  Monitor, 
  ShieldCheck,
  Building2,
  LogOut 
} from 'lucide-react';
import { useAuthStore } from '@/store/useAuthStore';
import { useRouter } from 'next/navigation';

const menuItems = [
  { name: 'Dashboard', href: '/', icon: LayoutDashboard },
  { name: 'Empleados', href: '/empleados', icon: Users },
  { name: 'Turnos y Horarios', href: '/turnos', icon: Clock },
  { name: 'Geocercas', href: '/geocercas', icon: MapPin },
  { name: 'Permisos / Licencias', href: '/permisos', icon: FileText },
  { name: 'Reportes', href: '/reportes', icon: BarChart3 },
  { name: 'Terminales Kiosco', href: '/kioscos', icon: Monitor },
  { name: 'Auditoría', href: '/auditoria', icon: ShieldCheck },
  { name: 'Empresas SaaS', href: '/admin/empresas', icon: Building2 },
];

export default function Sidebar() {
  const pathname = usePathname();
  const router = useRouter();
  const { logout, email, rol } = useAuthStore();

  if (pathname === '/login' || pathname === '/marcar') return null;

  return (
    <aside className="w-64 bg-slate-900 border-r border-slate-800 min-h-screen p-4 flex flex-col justify-between fixed left-0 top-0 z-40">
      <div className="space-y-6">
        {/* Brand Logo */}
        <div className="flex items-center gap-3 px-2">
          <div className="w-9 h-9 rounded-xl bg-blue-600 flex items-center justify-center text-white font-bold text-lg shadow-lg shadow-blue-600/30">
            A
          </div>
          <div>
            <h2 className="font-bold text-white text-base leading-tight">AsistenciaCore</h2>
            <span className="text-[10px] uppercase font-semibold text-blue-400 tracking-wider">SaaS Enterprise</span>
          </div>
        </div>

        {/* Navigation Menu */}
        <nav className="space-y-1">
          {menuItems.map((item) => {
            const Icon = item.icon;
            const isActive = pathname === item.href;
            return (
              <Link
                key={item.href}
                href={item.href}
                className={`flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium transition duration-150 ${
                  isActive
                    ? 'bg-blue-600 text-white shadow-lg shadow-blue-600/20'
                    : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800/60'
                }`}
              >
                <Icon className="w-5 h-5" />
                {item.name}
              </Link>
            );
          })}
        </nav>
      </div>

      {/* User Footer */}
      <div className="pt-4 border-t border-slate-800 space-y-3">
        <div className="px-2">
          <p className="text-xs font-semibold text-white truncate">{email || 'Usuario Activo'}</p>
          <p className="text-[10px] text-slate-500 uppercase font-medium">{rol || 'ADMIN'}</p>
        </div>
        <button
          onClick={() => {
            logout();
            router.push('/login');
          }}
          className="w-full flex items-center gap-2 px-3 py-2 text-xs font-medium text-rose-400 hover:bg-rose-500/10 rounded-lg transition"
        >
          <LogOut className="w-4 h-4" />
          Cerrar Sesión
        </button>
      </div>
    </aside>
  );
}