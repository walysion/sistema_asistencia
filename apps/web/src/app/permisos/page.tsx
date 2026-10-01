'use client';

import { useEffect, useState } from 'react';
import { api } from '@/lib/api';
import { FileText, Plus, Check, X, Clock, AlertCircle } from 'lucide-react';

interface Permiso {
  id: number;
  empleadoId: number;
  empleado?: { nombreCompleto: string; codigoTrabajador: string };
  motivo: string;
  fechaInicio: string;
  fechaFin: string;
  estado: 'PENDIENTE' | 'APROBADO' | 'RECHAZADO';
}

interface EmpleadoOp {
  id: number;
  nombreCompleto: string;
  codigoTrabajador: string;
}

export default function PermisosPage() {
  const [permisos, setPermisos] = useState<Permiso[]>([]);
  const [empleados, setEmpleados] = useState<EmpleadoOp[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalAbierto, setModalAbierto] = useState(false);
  const [guardando, setGuardando] = useState(false);

  // Formulario nuevo permiso
  const [empleadoId, setEmpleadoId] = useState('');
  const [motivo, setMotivo] = useState('');
  const [fechaInicio, setFechaInicio] = useState(new Date().toISOString().split('T')[0]);
  const [fechaFin, setFechaFin] = useState(new Date().toISOString().split('T')[0]);

  const cargarDatos = async () => {
    try {
      setLoading(true);
      const [resPerm, resEmp] = await Promise.all([
        api.get('/Permisos'),
        api.get('/Empleados'),
      ]);
      setPermisos(resPerm.data);
      setEmpleados(resEmp.data);
    } catch (err) {
      console.error('Error al cargar permisos:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    cargarDatos();
  }, []);

  const handleCrearPermiso = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!empleadoId) return alert('Seleccione un empleado.');

    setGuardando(true);
    try {
      await api.post('/Permisos', {
        empleadoId: Number(empleadoId),
        motivo,
        fechaInicio,
        fechaFin,
      });

      setModalAbierto(false);
      setMotivo('');
      cargarDatos();
    } catch (err) {
      alert('Error al registrar la solicitud de permiso.');
    } finally {
      setGuardando(false);
    }
  };

  const handleCambiarEstado = async (id: number, nuevoEstado: 'APROBADO' | 'RECHAZADO') => {
    try {
      await api.put(`/Permisos/${id}/estado`, { estado: nuevoEstado });
      cargarDatos();
    } catch (err) {
      alert('Error al actualizar el estado del permiso.');
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 bg-slate-900 border border-slate-800 p-6 rounded-2xl">
        <div>
          <h1 className="text-2xl font-bold text-white flex items-center gap-3">
            <FileText className="w-7 h-7 text-blue-500" />
            Permisos y Licencias Médicas
          </h1>
          <p className="text-sm text-slate-400">Gestión y aprobación de justificaciones horarias y vacaciones</p>
        </div>

        <button
          onClick={() => setModalAbierto(true)}
          className="bg-blue-600 hover:bg-blue-500 text-white font-semibold px-4 py-2.5 rounded-xl text-sm flex items-center gap-2 shadow-lg shadow-blue-600/20 transition"
        >
          <Plus className="w-4 h-4" />
          Registrar Permiso
        </button>
      </div>

      {/* Tabla de Solicitudes */}
      <div className="bg-slate-900 border border-slate-800 rounded-2xl overflow-hidden shadow-xl">
        <table className="w-full text-left border-collapse">
          <thead>
            <tr className="border-b border-slate-800 bg-slate-950/50 text-slate-400 text-xs uppercase tracking-wider">
              <th className="p-4">Empleado</th>
              <th className="p-4">Motivo / Justificación</th>
              <th className="p-4 text-center">Período</th>
              <th className="p-4 text-center">Estado</th>
              <th className="p-4 text-center">Acciones</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-800/60 text-sm">
            {loading ? (
              <tr>
                <td colSpan={5} className="p-8 text-center text-slate-500">
                  Cargando solicitudes de permisos...
                </td>
              </tr>
            ) : permisos.length === 0 ? (
              <tr>
                <td colSpan={5} className="p-8 text-center text-slate-500">
                  No hay permisos o licencias registradas.
                </td>
              </tr>
            ) : (
              permisos.map((p) => (
                <tr key={p.id} className="hover:bg-slate-800/30 transition">
                  <td className="p-4 font-medium text-white">
                    {p.empleado?.nombreCompleto || `Empleado #${p.empleadoId}`}
                  </td>
                  <td className="p-4 text-slate-300">{p.motivo}</td>
                  <td className="p-4 text-center text-xs text-slate-400 font-mono">
                    {p.fechaInicio} al {p.fechaFin}
                  </td>
                  <td className="p-4 text-center">
                    <span
                      className={`inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-semibold ${
                        p.estado === 'APROBADO'
                          ? 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/20'
                          : p.estado === 'RECHAZADO'
                          ? 'bg-rose-500/10 text-rose-400 border border-rose-500/20'
                          : 'bg-amber-500/10 text-amber-400 border border-amber-500/20'
                      }`}
                    >
                      <Clock className="w-3 h-3" />
                      {p.estado}
                    </span>
                  </td>
                  <td className="p-4 text-center">
                    {p.estado === 'PENDIENTE' && (
                      <div className="flex items-center justify-center gap-2">
                        <button
                          onClick={() => handleCambiarEstado(p.id, 'APROBADO')}
                          className="p-1.5 bg-emerald-600/20 text-emerald-400 hover:bg-emerald-600/40 rounded-lg transition"
                          title="Aprobar Permiso"
                        >
                          <Check className="w-4 h-4" />
                        </button>
                        <button
                          onClick={() => handleCambiarEstado(p.id, 'RECHAZADO')}
                          className="p-1.5 bg-rose-600/20 text-rose-400 hover:bg-rose-600/40 rounded-lg transition"
                          title="Rechazar Permiso"
                        >
                          <X className="w-4 h-4" />
                        </button>
                      </div>
                    )}
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {/* Modal Nueva Solicitud */}
      {modalAbierto && (
        <div className="fixed inset-0 bg-slate-950/80 backdrop-blur-sm flex items-center justify-center p-4 z-50">
          <div className="bg-slate-900 border border-slate-800 rounded-2xl p-6 max-w-md w-full space-y-5 shadow-2xl">
            <h2 className="text-xl font-bold text-white">Solicitar Permiso o Licencia</h2>

            <form onSubmit={handleCrearPermiso} className="space-y-4">
              <div>
                <label className="block text-xs font-semibold uppercase text-slate-400 mb-1">Empleado</label>
                <select
                  required
                  value={empleadoId}
                  onChange={(e) => setEmpleadoId(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-blue-500"
                >
                  <option value="">Seleccione un trabajador...</option>
                  {empleados.map((emp) => (
                    <option key={emp.id} value={emp.id}>
                      {emp.nombreCompleto} ({emp.codigoTrabajador})
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-xs font-semibold uppercase text-slate-400 mb-1">Motivo / Justificación</label>
                <textarea
                  required
                  rows={3}
                  placeholder="Ej. Cita médica / Licencia por enfermedad"
                  value={motivo}
                  onChange={(e) => setMotivo(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-blue-500"
                />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-semibold uppercase text-slate-400 mb-1">Fecha Inicio</label>
                  <input
                    type="date"
                    required
                    value={fechaInicio}
                    onChange={(e) => setFechaInicio(e.target.value)}
                    className="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-blue-500"
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold uppercase text-slate-400 mb-1">Fecha Fin</label>
                  <input
                    type="date"
                    required
                    value={fechaFin}
                    onChange={(e) => setFechaFin(e.target.value)}
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
                  {guardando ? 'Guardando...' : 'Crear Solicitud'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}