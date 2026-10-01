'use client';

import { useState, useEffect } from 'react';
import { api } from '@/lib/api';
import { Clock, CheckCircle2, AlertTriangle, UserCheck, Wifi, WifiOff, RefreshCw } from 'lucide-react';

interface MarcacionPendiente {
  id: string;
  codigoTrabajador: string;
  tipoMovimiento: string;
  latitud: number;
  longitud: number;
  apiKey: string;
  fechaHoraLocal: string;
}

export default function MarcarKioscoPage() {
  const [codigoTrabajador, setCodigoTrabajador] = useState('');
  const [apiKey, setApiKey] = useState('');
  const [tipoMovimiento, setTipoMovimiento] = useState<'ENTRADA' | 'SALIDA_COLACION' | 'ENTRADA_COLACION' | 'SALIDA'>('ENTRADA');
  const [latitud, setLatitud] = useState<number | null>(null);
  const [longitud, setLongitud] = useState<number | null>(null);

  const [online, setOnline] = useState(true);
  const [colaPendientes, setColaPendientes] = useState<MarcacionPendiente[]>([]);
  const [sincronizando, setSincronizando] = useState(false);

  const [procesando, setProcesando] = useState(false);
  const [resultado, setResultado] = useState<{ tipo: 'ok' | 'error' | 'offline'; mensaje: string; detalles?: any } | null>(null);
  const [reloj, setReloj] = useState<string>('');

  // 1. Monitoreo de estado de Red y Carga de Cola Local
  useEffect(() => {
    setOnline(navigator.onLine);

    const handleOnline = () => {
      setOnline(true);
      sincronizarColaLocal();
    };
    const handleOffline = () => setOnline(false);

    window.addEventListener('online', handleOnline);
    window.addEventListener('offline', handleOffline);

    // Cargar marcaciones guardadas en cola local si existen
    const guardadas = localStorage.getItem('marcaciones_offline_cola');
    if (guardadas) {
      try {
        setColaPendientes(JSON.parse(guardadas));
      } catch (e) {
        console.error('Error al leer cola local:', e);
      }
    }

    // Timer Reloj
    const timer = setInterval(() => {
      setReloj(new Date().toLocaleTimeString());
    }, 1000);

    // GPS
    if (navigator.geolocation) {
      navigator.geolocation.getCurrentPosition(
        (pos) => {
          setLatitud(pos.coords.latitude);
          setLongitud(pos.coords.longitude);
        },
        (err) => console.warn('Geolocalización no disponible:', err)
      );
    }

    return () => {
      window.removeEventListener('online', handleOnline);
      window.removeEventListener('offline', handleOffline);
      clearInterval(timer);
    };
  }, []);

  // 2. Función de Sincronización de Cola Offline con la API
  const sincronizarColaLocal = async () => {
    const guardadas = localStorage.getItem('marcaciones_offline_cola');
    if (!guardadas) return;

    const lista: MarcacionPendiente[] = JSON.parse(guardadas);
    if (lista.length === 0) return;

    setSincronizando(true);
    const noEnviadas: MarcacionPendiente[] = [];

    for (const item of lista) {
      try {
        await api.post('/Marcaciones/kiosco', {
          codigoTrabajador: item.codigoTrabajador,
          tipoMovimiento: item.tipoMovimiento,
          latitud: item.latitud,
          longitud: item.longitud,
          apiKey: item.apiKey,
        });
      } catch (err) {
        noEnviadas.push(item);
      }
    }

    localStorage.setItem('marcaciones_offline_cola', JSON.stringify(noEnviadas));
    setColaPendientes(noEnviadas);
    setSincronizando(false);
  };

  // 3. Procesar Marcación (Online u Offline)
  const handleMarcar = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!codigoTrabajador.trim()) return;

    setProcesando(true);
    setResultado(null);

    const payload = {
      codigoTrabajador: codigoTrabajador.trim(),
      tipoMovimiento,
      latitud: latitud ?? -33.4372,
      longitud: longitud ?? -70.6506,
      apiKey: apiKey || 'kiosk_demo_key',
    };

    // Si NO hay conexión a Internet, guardar en local
    if (!online) {
      const nuevaPendiente: MarcacionPendiente = {
        ...payload,
        id: Date.now().toString(),
        fechaHoraLocal: new Date().toISOString(),
      };

      const nuevaCola = [...colaPendientes, nuevaPendiente];
      localStorage.setItem('marcaciones_offline_cola', JSON.stringify(nuevaCola));
      setColaPendientes(nuevaCola);

      setResultado({
        tipo: 'offline',
        mensaje: 'Guardado Offline Localmente',
        detalles: { nombreEmpleado: codigoTrabajador, minutosAtraso: 0 },
      });

      setProcesando(false);
      setTimeout(() => {
        setCodigoTrabajador('');
        setResultado(null);
      }, 3500);
      return;
    }

    // Modo Online Normal
    try {
      const res = await api.post('/Marcaciones/kiosco', payload);

      setResultado({
        tipo: 'ok',
        mensaje: 'Marcación Registrada Correctamente',
        detalles: res.data,
      });

      setTimeout(() => {
        setCodigoTrabajador('');
        setResultado(null);
      }, 4000);
    } catch (err: any) {
      setResultado({
        tipo: 'error',
        mensaje: err.response?.data?.message || err.response?.data || 'Error al procesar la marcación.',
      });
    } finally {
      setProcesando(false);
    }
  };

  return (
    <div className="min-h-screen bg-slate-950 text-white flex flex-col items-center justify-center p-4">
      <div className="max-w-md w-full bg-slate-900 border border-slate-800 rounded-3xl p-8 shadow-2xl space-y-6 text-center relative overflow-hidden">
        
        {/* Indicador de Estado de Red / Cola Local */}
        <div className="flex justify-between items-center bg-slate-950 p-3 rounded-2xl border border-slate-800/80 text-xs">
          <div className="flex items-center gap-2 font-medium">
            {online ? (
              <>
                <Wifi className="w-4 h-4 text-emerald-400" />
                <span className="text-emerald-400">En Línea</span>
              </>
            ) : (
              <>
                <WifiOff className="w-4 h-4 text-amber-400 animate-pulse" />
                <span className="text-amber-400">Modo Offline (Sin Red)</span>
              </>
            )}
          </div>

          {colaPendientes.length > 0 && (
            <div className="flex items-center gap-2">
              <span className="bg-amber-500/20 text-amber-400 px-2 py-0.5 rounded-md font-bold font-mono">
                {colaPendientes.length} pend.
              </span>
              {online && (
                <button
                  type="button"
                  onClick={sincronizarColaLocal}
                  disabled={sincronizando}
                  className="text-blue-400 hover:underline flex items-center gap-1 font-semibold"
                >
                  <RefreshCw className={`w-3.5 h-3.5 ${sincronizando ? 'animate-spin' : ''}`} />
                  Sincronizar
                </button>
              )}
            </div>
          )}
        </div>

        {/* Reloj Digital */}
        <div className="space-y-1">
          <div className="inline-flex items-center gap-2 bg-blue-600/10 text-blue-400 border border-blue-500/20 px-3 py-1 rounded-full text-xs font-semibold">
            <Clock className="w-3.5 h-3.5" /> Terminal Kiosco de Asistencia
          </div>
          <h1 className="text-4xl font-extrabold text-white font-mono tracking-tight pt-2">{reloj || '--:--:--'}</h1>
        </div>

        {/* Notificación de Resultado */}
        {resultado && (
          <div
            className={`p-4 rounded-2xl border text-sm font-medium transition-all ${
              resultado.tipo === 'ok'
                ? 'bg-emerald-500/10 border-emerald-500/30 text-emerald-400'
                : resultado.tipo === 'offline'
                ? 'bg-amber-500/10 border-amber-500/30 text-amber-400'
                : 'bg-rose-500/10 border-rose-500/30 text-rose-400'
            }`}
          >
            <div className="flex items-center justify-center gap-2 text-base font-bold mb-1">
              {resultado.tipo === 'ok' ? (
                <CheckCircle2 className="w-5 h-5 text-emerald-400" />
              ) : resultado.tipo === 'offline' ? (
                <WifiOff className="w-5 h-5 text-amber-400" />
              ) : (
                <AlertTriangle className="w-5 h-5 text-rose-400" />
              )}
              <span>{resultado.mensaje}</span>
            </div>
            {resultado.detalles && (
              <p className="text-xs text-slate-300">
                {resultado.detalles.nombreEmpleado} {resultado.tipo === 'offline' ? '(se enviará al conectar)' : ''}
              </p>
            )}
          </div>
        )}

        {/* Formulario */}
        <form onSubmit={handleMarcar} className="space-y-5">
          <div className="grid grid-cols-2 gap-2">
            {[
              { id: 'ENTRADA', label: 'Entrada' },
              { id: 'SALIDA_COLACION', label: 'Inicio Colación' },
              { id: 'ENTRADA_COLACION', label: 'Fin Colación' },
              { id: 'SALIDA', label: 'Salida' },
            ].map((mov) => (
              <button
                type="button"
                key={mov.id}
                onClick={() => setTipoMovimiento(mov.id as any)}
                className={`py-3 px-2 rounded-xl text-xs font-bold uppercase transition border ${
                  tipoMovimiento === mov.id
                    ? 'bg-blue-600 border-blue-500 text-white shadow-lg shadow-blue-600/30'
                    : 'bg-slate-950 border-slate-800 text-slate-400 hover:bg-slate-800'
                }`}
              >
                {mov.label}
              </button>
            ))}
          </div>

          <div className="text-left space-y-1">
            <label className="text-xs font-semibold uppercase text-slate-400">Código de Trabajador o RUT</label>
            <input
              type="text"
              required
              autoFocus
              placeholder="Ej. EMP-001"
              value={codigoTrabajador}
              onChange={(e) => setCodigoTrabajador(e.target.value)}
              className="w-full bg-slate-950 border border-slate-800 rounded-xl px-4 py-3.5 text-center text-xl font-mono text-white placeholder-slate-600 focus:outline-none focus:border-blue-500 transition"
            />
          </div>

          <div className="text-left space-y-1">
            <label className="text-[10px] font-semibold uppercase text-slate-500">API Key del Terminal (Tablet)</label>
            <input
              type="password"
              placeholder="kiosk_..."
              value={apiKey}
              onChange={(e) => setApiKey(e.target.value)}
              className="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-1.5 text-xs font-mono text-slate-400 focus:outline-none focus:border-blue-500"
            />
          </div>

          <button
            type="submit"
            disabled={procesando}
            className="w-full bg-emerald-600 hover:bg-emerald-500 text-white font-bold py-4 rounded-xl shadow-lg shadow-emerald-600/25 transition disabled:opacity-50 text-base flex items-center justify-center gap-2"
          >
            <UserCheck className="w-5 h-5" />
            {procesando ? 'Verificando...' : 'REGISTRAR MARCACIÓN'}
          </button>
        </form>

        <p className="text-[11px] text-slate-500">
          Ubicación GPS: {latitud ? `${latitud.toFixed(4)}, ${longitud?.toFixed(4)}` : 'Obteniendo GPS...'}
        </p>
      </div>
    </div>
  );
}