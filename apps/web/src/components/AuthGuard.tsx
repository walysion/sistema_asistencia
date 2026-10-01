'use client';

import { useEffect, useState } from 'react';
import { usePathname, useRouter } from 'next/navigation';
import { useAuthStore } from '@/store/useAuthStore';

export default function AuthGuard({ children }: { children: React.ReactNode }) {
  const token = useAuthStore((state) => state.token);
  const pathname = usePathname();
  const router = useRouter();
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    setMounted(true);
  }, []);

  useEffect(() => {
    if (!mounted) return;

    // Rutas públicas que no requieren iniciar sesión
    const rutasPublicas = ['/login', '/marcar'];
    const esRutaPublica = rutasPublicas.includes(pathname);

    if (!token && !esRutaPublica) {
      router.push('/login');
    }
  }, [token, pathname, router, mounted]);

  // Evitar problemas de hidratación entre SSR y renderizado del cliente
  if (!mounted) return null;

  const rutasPublicas = ['/login', '/marcar'];
  const esRutaPublica = rutasPublicas.includes(pathname);

  if (!token && !esRutaPublica) {
    return null;
  }

  return <>{children}</>;
}