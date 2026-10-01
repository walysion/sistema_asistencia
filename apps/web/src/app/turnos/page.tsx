'use client';

import { useEffect, useState } from 'react';
import { api } from '@/lib/api';
import { Clock, Plus, Trash2, CheckCircle, AlertCircle } from 'lucide-react';

interface Turno {
  id: number;
  nombre: string;
  horaEntrada: string;
  horaSalida: string;
  toleranciaMinutos: number;
  minutosColacion: number;
}

export default function TurnosPage() {
  const [turnos, setTurnos] = useState<Turno[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalAbierto, setModalAbierto] = useState(false);
  const [guardando, setGuardando] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Formulario de nuevo turno
  const [nombre, setNombre] = useState('');
  const [horaEntrada, setHoraEntrada] = useState('09:00');
  const [horaSalida, setHoraSalida] = useState('18:00');
  const [toleranciaMinutos, setToleranciaMinutos] = useState(15);
  const [minutosColacion, setMinutosColacion] = useState(60);

  const cargarTurnos = async () => {
    try {
      setLoading(true);
      const res = await api.get('/Turnos');
      setTurnos(res.data);
    } catch (err) {
      console.error('Error al obtener turnos:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    cargarTurnos();
  }, []);

  const handleCrearTurno = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setGuardando(true);

    try {
      await api.post('/Turnos', {
        nombre,
        horaEntrada: `${horaEntrada}:00`,
        horaSalida: `${horaSalida}:00`,
        toleranciaMinutos: Number(toleranciaMinutos),
        minutosColacion: Number(minutosColacion),
      });

      setModalAbierto(false);
      setNombre('');
      cargarTurnos();
    } catch (err: any) {
      setError(err.response?.data?.message || err.response?.data || 'Error al guardar el turno.');
    } finally {
      setGuardando(false);
    }
  };

  const handleEliminarTurno = async (id: number) => {
    if (!confirm('¿Está seguro de eliminar este turno de trabajo?')) return;

    try {
      await api.delete(`/Turnos/${id}`);
      cargarTurnos();
    } catch (err: any) {
      alert(err.response?.data || 'No se pudo eliminar el turno porque está asignado a empleados.');
    }
  };

  return (
    <div className="space-y-6">
      {/* Header del Módulo */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 bg-slate-900 border border-slate-800 p-6 rounded-2xl">
        <div>
          <h1 className="text-2xl font-bold text-white flex items-center gap-3">
            <Clock className="w-7 h-7 text-blue-500" />
            Turnos y Horarios
          </h1>
          <p className="text-sm text-slate-400">Definición de jornadas laborales, colación y minutos de tolerancia</p>
        </div>

        <button
          onClick={() => setModalAbierto(true)}
          className="bg-blue-600 hover:bg-blue-500 text-white font-semibold px-4 py-2.5 rounded-xl text-sm flex items-center gap-2 shadow-lg shadow-blue-600/20 transition"
        >
          <Plus className="w-4 h-4" />
          Nuevo Turno
        </button>
      </div>

      {/* Grid de Turnos Existentes */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {loading ? (
          <p className="text-slate-500 text-sm col-span-3">Cargando turnos de la empresa...</p>
        ) : turnos.length === 0 ? (
          <p className="text-slate-500 text-sm col-span-3">No hay turnos registrados en el sistema.</p>
        ) : (
          turnos.map((t) => (
            <div key={t.id} className="bg-slate-900 border border-slate-800 p-5 rounded-2xl space-y-4 relative group">
              <div className="flex justify-between items-start">
                <div>
                  <h3 className="font-bold text-white text-lg">{t.nombre}</h3>
                  <span className="text-xs text-blue-400 font-medium">Jornada Regular</span>
                </div>
                <button
                  onClick={() => handleEliminarTurno(t.id)}
                  className="p-2 text-slate-500 hover:text-rose-400 hover:bg-rose-500/10 rounded-lg transition"
                  title="Eliminar Turno"
                >
                  <Trash2 className="w-4 h-4" />
                </button>
              </div>

              <div className="grid grid-cols-2 gap-2 pt-2 border-t border-slate-800/80 text-xs">
                <div>
                  <p className="text-slate-500 font-medium">Entrada / Salida</p>
                  <p className="text-slate-200 font-semibold mt-0.5">
                    {t.horaEntrada.slice(0, 5)} - {t.horaSalida.slice(0, 5)}
                  </p>
                </div>
                <div>
                  <p className="text-slate-500 font-medium">Tolerancia Atrasos</p>
                  <p className="text-amber-400 font-semibold mt-0.5">{t.toleranciaMinutos} minutos</p>
                </div>
                <div className="col-span-2 pt-1">
                  <p className="text-slate-500 font-medium">Colación</p>
                  <p className="text-slate-300 font-semibold mt-0.5">{t.minutosColacion} minutos programados</p>
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
            <h2 className="text-xl font-bold text-white">Registrar Nuevo Turno</h2>

            {error && (
              <div className="flex items-center gap-2 bg-rose-500/10 border border-rose-500/30 text-rose-400 p-3 rounded-lg text-xs">
                <AlertCircle className="w-4 h-4 flex-shrink-0" />
                <span>{error}</span>
              </div>
            )}

            <form onSubmit={handleCrearTurno} className="space-y-4">
              <div>
                <label className="block text-xs font-semibold uppercase text-slate-400 mb-1">Nombre del Turno</label>
                <input
                  type="text"
                  required
                  placeholder="Ej. Turno Administrativo u Operativo"
                  value={nombre}
                  onChange={(e) => setNombre(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-blue-500"
                />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-semibold uppercase text-slate-400 mb-1">Hora Entrada</label>
                  <input
                    type="time"
                    required
                    value={horaEntrada}
                    onChange={(e) => setHoraEntrada(e.target.value)}
                    className="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-blue-500"
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold uppercase text-slate-400 mb-1">Hora Salida</label>
                  <input
                    type="time"
                    required
                    value={horaSalida}
                    onChange={(e) => setHoraSalida(e.target.value)}
                    className="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-blue-500"
                  />
                </div>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-semibold uppercase text-slate-400 mb-1">Tolerancia (Minutos)</label>
                  <input
                    type="number"
                    min="0"
                    value={toleranciaMinutos}
                    onChange={(e) => setToleranciaMinutos(Number(e.target.value))}
                    className="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-blue-500"
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold uppercase text-slate-400 mb-1">Colación (Minutos)</label>
                  <input
                    type="number"
                    min="0"
                    value={minutosColacion}
                    onChange={(e) => setMinutosColacion(Number(e.target.value))}
                    className="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-blue-500"
                  />
                </div>
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
                  {guardando ? 'Guardando...' : 'Crear Turno'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}