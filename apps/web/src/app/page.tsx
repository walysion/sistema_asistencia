'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import { api } from '@/lib/api';
import { useAuthStore } from '@/store/useAuthStore';
import { Users, UserCheck, Clock, AlertTriangle, LogOut, Radio } from 'lucide-react';

interface ResumenData {
  fechaUtc: string;
  totalEmpleados: number;
  presentes: number;
  ausentes: number;
  conAtraso: number;
  conPermiso: number;
  fueraDeGeocerca: number;
  porcentajeAsistencia: number;
}

export default function DashboardPage() {
  const [resumen, setResumen] = useState<ResumenData | null>(null);
  const [conectadoSignalR, setConectadoSignalR] = useState(false);
  const [ultimasMarcaciones, setUltimasMarcaciones] = useState<any[]>([]);

  const router = useRouter();
  const { logout, email } = useAuthStore();

  const cargarResumen = async () => {
    try {
      const res = await api.get('/Dashboard/resumen-hoy');
      setResumen(res.data);
    } catch (error) {
      console.error('Error al cargar el resumen:', error);
    }
  };

  useEffect(() => {
    const token = localStorage.getItem('accessToken');
    if (!token) {
      router.push('/login');
      return;
    }

    cargarResumen();

    let isMounted = true;
    const signalrUrl = process.env.NEXT_PUBLIC_SIGNALR_URL || 'http://localhost:5240/hubs/asistencia';

    const connection: HubConnection = new HubConnectionBuilder()
      .withUrl(`${signalrUrl}?access_token=${token}`)
      .withAutomaticReconnect()
      .build();

    const iniciarConexion = async () => {
      try {
        if (connection.state === HubConnectionState.Disconnected) {
          await connection.start();
          if (isMounted) {
            setConectadoSignalR(true);
            connection.on('NuevaMarcacion', (marcacion) => {
              setUltimasMarcaciones((prev) => [marcacion, ...prev.slice(0, 4)]);
              cargarResumen();
            });
          }
        }
      } catch (err) {
        if (isMounted) {
          console.error('Error al conectar con SignalR:', err);
        }
      }
    };

    iniciarConexion();

    return () => {
      isMounted = false;
      if (
        connection.state === HubConnectionState.Connected ||
        connection.state === HubConnectionState.Connecting
      ) {
        connection.stop();
      }
    };
  }, [router]);

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 p-6 space-y-6">
      {/* Header */}
      <header className="flex flex-col md:flex-row md:items-center justify-between gap-4 bg-slate-900 border border-slate-800 p-6 rounded-2xl shadow-lg">
        <div>
          <h1 className="text-2xl font-bold text-white">Dashboard de Asistencia</h1>
          <p className="text-sm text-slate-400">Monitoreo en tiempo real multitenant</p>
        </div>

        <div className="flex items-center gap-4">
          <div className="flex items-center gap-2 bg-slate-800 border border-slate-700 px-3 py-1.5 rounded-full text-xs">
            <Radio className={`w-4 h-4 ${conectadoSignalR ? 'text-green-400 animate-pulse' : 'text-red-400'}`} />
            <span>{conectadoSignalR ? 'SignalR En Vivo' : 'Desconectado'}</span>
          </div>

          <span className="text-sm font-medium text-slate-300">{email}</span>

          <button
            onClick={() => {
              logout();
              router.push('/login');
            }}
            className="p-2 bg-slate-800 hover:bg-slate-700 text-slate-300 rounded-xl transition border border-slate-700"
            title="Cerrar Sesión"
          >
            <LogOut className="w-5 h-5" />
          </button>
        </div>
      </header>

      {/* Grid de Métricas */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <div className="bg-slate-900 border border-slate-800 p-5 rounded-2xl space-y-2">
          <div className="flex justify-between items-center text-slate-400">
            <span className="text-xs font-semibold uppercase">Total Empleados</span>
            <Users className="w-5 h-5 text-blue-400" />
          </div>
          <p className="text-3xl font-bold text-white">{resumen?.totalEmpleados ?? 0}</p>
        </div>

        <div className="bg-slate-900 border border-slate-800 p-5 rounded-2xl space-y-2">
          <div className="flex justify-between items-center text-slate-400">
            <span className="text-xs font-semibold uppercase">Presentes Hoy</span>
            <UserCheck className="w-5 h-5 text-emerald-400" />
          </div>
          <p className="text-3xl font-bold text-emerald-400">{resumen?.presentes ?? 0}</p>
          <span className="text-xs text-slate-500">{resumen?.porcentajeAsistencia ?? 0}% de asistencia</span>
        </div>

        <div className="bg-slate-900 border border-slate-800 p-5 rounded-2xl space-y-2">
          <div className="flex justify-between items-center text-slate-400">
            <span className="text-xs font-semibold uppercase">Atrasos</span>
            <Clock className="w-5 h-5 text-amber-400" />
          </div>
          <p className="text-3xl font-bold text-amber-400">{resumen?.conAtraso ?? 0}</p>
        </div>

        <div className="bg-slate-900 border border-slate-800 p-5 rounded-2xl space-y-2">
          <div className="flex justify-between items-center text-slate-400">
            <span className="text-xs font-semibold uppercase">Fuera de Geocerca</span>
            <AlertTriangle className="w-5 h-5 text-rose-400" />
          </div>
          <p className="text-3xl font-bold text-rose-400">{resumen?.fueraDeGeocerca ?? 0}</p>
        </div>
      </div>

      {/* Feed en vivo */}
      <div className="bg-slate-900 border border-slate-800 p-6 rounded-2xl space-y-4">
        <h2 className="text-lg font-semibold text-white flex items-center gap-2">
          <Radio className="w-5 h-5 text-blue-500" />
          Últimas Marcaciones Notificadas
        </h2>

        {ultimasMarcaciones.length === 0 ? (
          <p className="text-sm text-slate-500">Esperando marcaciones desde los dispositivos...</p>
        ) : (
          <div className="space-y-2">
            {ultimasMarcaciones.map((m, idx) => (
              <div key={idx} className="flex justify-between items-center bg-slate-950 p-4 rounded-xl border border-slate-800">
                <div>
                  <p className="text-sm font-semibold text-white">{m.nombreEmpleado || `Empleado #${m.empleadoId}`}</p>
                  <p className="text-xs text-slate-400">
                    {m.tipoMovimiento} - {m.origen}
                  </p>
                </div>
                <div className="text-right">
                  <span className={`text-xs px-2.5 py-1 rounded-full font-semibold ${m.minutosAtraso > 0 ? 'bg-amber-500/20 text-amber-400' : 'bg-emerald-500/20 text-emerald-400'}`}>
                    {m.minutosAtraso > 0 ? `${m.minutosAtraso} min atraso` : 'A tiempo'}
                  </span>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}