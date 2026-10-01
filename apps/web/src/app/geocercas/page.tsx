'use client';

import { useEffect, useState } from 'react';
import { api } from '@/lib/api';
import { MapPin, Plus, Trash2, CheckCircle, AlertCircle } from 'lucide-react';

interface Geocerca {
  id: number;
  nombre: string;
  latitud: number;
  longitud: number;
  radioMetros: number;
  activa: boolean;
}

export default function GeocercasPage() {
  const [geocercas, setGeocercas] = useState<Geocerca[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalAbierto, setModalAbierto] = useState(false);
  const [guardando, setGuardando] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Formulario nueva geocerca
  const [nombre, setNombre] = useState('');
  const [latitud, setLatitud] = useState(-33.4372);
  const [longitud, setLongitud] = useState(-70.6506);
  const [radioMetros, setRadioMetros] = useState(200);

  const cargarGeocercas = async () => {
    try {
      setLoading(true);
      const res = await api.get('/Geocercas');
      setGeocercas(res.data);
    } catch (err) {
      console.error('Error al obtener geocercas:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    cargarGeocercas();
  }, []);

  const handleCrearGeocerca = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setGuardando(true);

    try {
      await api.post('/Geocercas', {
        nombre,
        latitud: Number(latitud),
        longitud: Number(longitud),
        radioMetros: Number(radioMetros),
      });

      setModalAbierto(false);
      setNombre('');
      cargarGeocercas();
    } catch (err: any) {
      setError(err.response?.data?.message || err.response?.data || 'Error al guardar la geocerca.');
    } finally {
      setGuardando(false);
    }
  };

  const handleDesactivarGeocerca = async (id: number) => {
    if (!confirm('¿Desea desactivar esta geocerca?')) return;

    try {
      await api.delete(`/Geocercas/${id}`);
      cargarGeocercas();
    } catch (err) {
      console.error('Error al desactivar geocerca:', err);
    }
  };

  return (
    <div className="space-y-6">
      {/* Header del Módulo */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 bg-slate-900 border border-slate-800 p-6 rounded-2xl">
        <div>
          <h1 className="text-2xl font-bold text-white flex items-center gap-3">
            <MapPin className="w-7 h-7 text-blue-500" />
            Geocercas y Perímetros de Asistencia
          </h1>
          <p className="text-sm text-slate-400">Configuración de zonas geográficas autorizadas para marcaciones móviles</p>
        </div>

        <button
          onClick={() => setModalAbierto(true)}
          className="bg-blue-600 hover:bg-blue-500 text-white font-semibold px-4 py-2.5 rounded-xl text-sm flex items-center gap-2 shadow-lg shadow-blue-600/20 transition"
        >
          <Plus className="w-4 h-4" />
          Nueva Geocerca
        </button>
      </div>

      {/* Grid de Geocercas */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {loading ? (
          <p className="text-slate-500 text-sm col-span-3">Cargando perímetro de geocercas...</p>
        ) : geocercas.length === 0 ? (
          <p className="text-slate-500 text-sm col-span-3">No hay geocercas configuradas.</p>
        ) : (
          geocercas.map((g) => (
            <div key={g.id} className="bg-slate-900 border border-slate-800 p-5 rounded-2xl space-y-4">
              <div className="flex justify-between items-start">
                <div>
                  <h3 className="font-bold text-white text-lg">{g.nombre}</h3>
                  <span className="inline-flex items-center gap-1 text-xs text-emerald-400 font-medium">
                    <CheckCircle className="w-3.5 h-3.5" /> Activa
                  </span>
                </div>
                <button
                  onClick={() => handleDesactivarGeocerca(g.id)}
                  className="p-2 text-slate-500 hover:text-rose-400 hover:bg-rose-500/10 rounded-lg transition"
                  title="Desactivar Geocerca"
                >
                  <Trash2 className="w-4 h-4" />
                </button>
              </div>

              <div className="space-y-1.5 pt-2 border-t border-slate-800/80 text-xs text-slate-400">
                <div className="flex justify-between">
                  <span>Coordenadas GPS:</span>
                  <span className="font-mono text-slate-200">{g.latitud.toFixed(4)}, {g.longitud.toFixed(4)}</span>
                </div>
                <div className="flex justify-between">
                  <span>Radio Permitido:</span>
                  <span className="font-semibold text-blue-400">{g.radioMetros} metros</span>
                </div>
              </div>
            </div>
          ))
        )}
      </div>

      {/* Modal de Creación */}
      {modalAbierto && (
        <div className="fixed inset-0 bg-slate-950/80 backdrop-blur-sm flex items-center justify-center p-4 z-50">
          <div className="bg-slate-900 border border-slate-800 rounded-2xl p-6 max-w-md w-full space-y-5 shadow-2xl">
            <h2 className="text-xl font-bold text-white">Registrar Nueva Geocerca</h2>

            {error && (
              <div className="flex items-center gap-2 bg-rose-500/10 border border-rose-500/30 text-rose-400 p-3 rounded-lg text-xs">
                <AlertCircle className="w-4 h-4 flex-shrink-0" />
                <span>{error}</span>
              </div>
            )}

            <form onSubmit={handleCrearGeocerca} className="space-y-4">
              <div>
                <label className="block text-xs font-semibold uppercase text-slate-400 mb-1">Nombre de la Ubicación / Sucursal</label>
                <input
                  type="text"
                  required
                  placeholder="Ej. Oficina Central Santiago"
                  value={nombre}
                  onChange={(e) => setNombre(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-blue-500"
                />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-semibold uppercase text-slate-400 mb-1">Latitud</label>
                  <input
                    type="number"
                    step="any"
                    required
                    value={latitud}
                    onChange={(e) => setLatitud(Number(e.target.value))}
                    className="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-blue-500"
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold uppercase text-slate-400 mb-1">Longitud</label>
                  <input
                    type="number"
                    step="any"
                    required
                    value={longitud}
                    onChange={(e) => setLongitud(Number(e.target.value))}
                    className="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-blue-500"
                  />
                </div>
              </div>

              <div>
                <label className="block text-xs font-semibold uppercase text-slate-400 mb-1">Radio de Cobertura (Metros)</label>
                <input
                  type="number"
                  min="10"
                  required
                  value={radioMetros}
                  onChange={(e) => setRadioMetros(Number(e.target.value))}
                  className="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-blue-500"
                />
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
                  {guardando ? 'Guardando...' : 'Crear Geocerca'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}