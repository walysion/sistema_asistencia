'use client';

import { useEffect, useState } from 'react';
import { api } from '@/lib/api';
import { Monitor, Plus, Key, RefreshCw, Trash2, ShieldCheck, MapPin } from 'lucide-react';

interface TerminalKiosco {
  id: number;
  nombre: string;
  apiKey: string;
  activo: boolean;
  ultimaConexionUtc: string;
  geocercaId?: number;
  geocerca?: { nombre: string };
}

interface GeocercaOp {
  id: number;
  nombre: string;
}

export default function KioscosPage() {
  const [terminales, setTerminales] = useState<TerminalKiosco[]>([]);
  const [geocercas, setGeocercas] = useState<GeocercaOp[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalAbierto, setModalAbierto] = useState(false);
  const [nombre, setNombre] = useState('');
  const [geocercaId, setGeocercaId] = useState<string>('');
  const [guardando, setGuardando] = useState(false);

  const cargarDatos = async () => {
    try {
      setLoading(true);
      const [resTerm, resGeo] = await Promise.all([
        api.get('/TerminalesKiosco'),
        api.get('/Geocercas'),
      ]);
      setTerminales(resTerm.data);
      setGeocercas(resGeo.data);
    } catch (err) {
      console.error('Error al cargar datos de kioscos:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    cargarDatos();
  }, []);

  const handleCrearTerminal = async (e: React.FormEvent) => {
    e.preventDefault();
    setGuardando(true);

    try {
      await api.post('/TerminalesKiosco', {
        nombre,
        geocercaId: geocercaId ? Number(geocercaId) : null,
      });

      setModalAbierto(false);
      setNombre('');
      setGeocercaId('');
      cargarDatos();
    } catch (err) {
      alert('Error al registrar el terminal kiosco.');
    } finally {
      setGuardando(false);
    }
  };

  const handleRegenerarApiKey = async (id: number) => {
    if (!confirm('¿Desea regenerar la API Key de este dispositivo? La clave anterior dejará de funcionar.')) return;

    try {
      const res = await api.post(`/TerminalesKiosco/${id}/regenerar-apikey`);
      alert(`Nueva API Key generada:\n${res.data.apiKey}`);
      cargarDatos();
    } catch (err) {
      alert('Error al regenerar la API Key.');
    }
  };

  const handleDesactivarTerminal = async (id: number) => {
    if (!confirm('¿Está seguro de desactivar este terminal?')) return;

    try {
      await api.delete(`/TerminalesKiosco/${id}`);
      cargarDatos();
    } catch (err) {
      alert('Error al desactivar el dispositivo.');
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 bg-slate-900 border border-slate-800 p-6 rounded-2xl">
        <div>
          <h1 className="text-2xl font-bold text-white flex items-center gap-3">
            <Monitor className="w-7 h-7 text-blue-500" />
            Terminales Kiosco y Hardware
          </h1>
          <p className="text-sm text-slate-400">Dispositivos físicos (Tablets/Tótems) autorizados para marcación</p>
        </div>

        <button
          onClick={() => setModalAbierto(true)}
          className="bg-blue-600 hover:bg-blue-500 text-white font-semibold px-4 py-2.5 rounded-xl text-sm flex items-center gap-2 shadow-lg shadow-blue-600/20 transition"
        >
          <Plus className="w-4 h-4" />
          Registrar Dispositivo
        </button>
      </div>

      {/* Grid de Dispositivos */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {loading ? (
          <p className="text-slate-500 text-sm col-span-3">Cargando dispositivos vinculados...</p>
        ) : terminales.length === 0 ? (
          <p className="text-slate-500 text-sm col-span-3">No hay terminales kiosco configurados.</p>
        ) : (
          terminales.map((t) => (
            <div key={t.id} className="bg-slate-900 border border-slate-800 p-5 rounded-2xl space-y-4">
              <div className="flex justify-between items-start">
                <div>
                  <h3 className="font-bold text-white text-lg">{t.nombre}</h3>
                  <span className="inline-flex items-center gap-1 text-xs text-emerald-400 font-medium">
                    <ShieldCheck className="w-3.5 h-3.5" /> Dispositivo Autenticado
                  </span>
                </div>
                <button
                  onClick={() => handleDesactivarTerminal(t.id)}
                  className="p-2 text-slate-500 hover:text-rose-400 hover:bg-rose-500/10 rounded-lg transition"
                  title="Desactivar Dispositivo"
                >
                  <Trash2 className="w-4 h-4" />
                </button>
              </div>

              <div className="space-y-2 pt-2 border-t border-slate-800/80 text-xs">
                <div className="flex items-center justify-between text-slate-400">
                  <span className="flex items-center gap-1">
                    <MapPin className="w-3.5 h-3.5 text-blue-400" /> Geocerca Asignada:
                  </span>
                  <span className="font-semibold text-slate-200">{t.geocerca?.nombre || 'Ninguna (Global)'}</span>
                </div>

                <div className="bg-slate-950 p-2.5 rounded-xl border border-slate-800 space-y-1">
                  <div className="flex justify-between items-center text-slate-500 text-[10px] font-semibold uppercase">
                    <span>API Key del Dispositivo</span>
                    <button
                      onClick={() => handleRegenerarApiKey(t.id)}
                      className="text-blue-400 hover:underline flex items-center gap-1"
                    >
                      <RefreshCw className="w-3 h-3" /> Regenerar
                    </button>
                  </div>
                  <p className="font-mono text-slate-300 text-xs truncate">{t.apiKey}</p>
                </div>
              </div>
            </div>
          ))
        )}
      </div>

      {/* Modal de Registro */}
      {modalAbierto && (
        <div className="fixed inset-0 bg-slate-950/80 backdrop-blur-sm flex items-center justify-center p-4 z-50">
          <div className="bg-slate-900 border border-slate-800 rounded-2xl p-6 max-w-md w-full space-y-5 shadow-2xl">
            <h2 className="text-xl font-bold text-white">Nuevo Terminal Kiosco</h2>

            <form onSubmit={handleCrearTerminal} className="space-y-4">
              <div>
                <label className="block text-xs font-semibold uppercase text-slate-400 mb-1">Nombre o Ubicación del Dispositivo</label>
                <input
                  type="text"
                  required
                  placeholder="Ej. Tablet Recepción Principal"
                  value={nombre}
                  onChange={(e) => setNombre(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-blue-500"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold uppercase text-slate-400 mb-1">Geocerca Opcional</label>
                <select
                  value={geocercaId}
                  onChange={(e) => setGeocercaId(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-blue-500"
                >
                  <option value="">Sin geocerca (Permitir libre ubicación)</option>
                  {geocercas.map((g) => (
                    <option key={g.id} value={g.id}>
                      {g.nombre}
                    </option>
                  ))}
                </select>
              </div>

              <div className="flex justify-end gap-3 pt-2">
                <button
                  type="button"
                  onClick={() => setModalAbierto(false)}
                  className="px-4 py-2 bg-slate-800 hover:bg-slate-700 text-slate-300 rounded-lg text-xs font-semibold"
                >
                  Cancelar
                </button>
                <button
                  type="submit"
                  disabled={guardando}
                  className="px-4 py-2 bg-blue-600 hover:bg-blue-500 text-white rounded-lg text-xs font-semibold shadow-lg shadow-blue-600/30 disabled:opacity-50"
                >
                  {guardando ? 'Generando Key...' : 'Crear Dispositivo'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}