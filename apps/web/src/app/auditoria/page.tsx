'use client';

import { useEffect, useState } from 'react';
import { api } from '@/lib/api';
import { ShieldCheck, Search, Clock, User, Activity } from 'lucide-react';

interface AuditLog {
  id: number;
  accion: string;
  detalle: string;
  usuario: string;
  fechaHoraUtc: string;
}

export default function AuditoriaPage() {
  const [logs, setLogs] = useState<AuditLog[]>([]);
  const [filtro, setFiltro] = useState('');
  const [loading, setLoading] = useState(true);

  const cargarLogs = async () => {
    try {
      setLoading(true);
      const res = await api.get('/Auditoria?limite=100');
      setLogs(res.data);
    } catch (err) {
      console.error('Error al cargar historial de auditoría:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    cargarLogs();
  }, []);

  const logsFiltrados = logs.filter(
    (l) =>
      l.accion.toLowerCase().includes(filtro.toLowerCase()) ||
      l.detalle.toLowerCase().includes(filtro.toLowerCase()) ||
      l.usuario.toLowerCase().includes(filtro.toLowerCase())
  );

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 bg-slate-900 border border-slate-800 p-6 rounded-2xl">
        <div>
          <h1 className="text-2xl font-bold text-white flex items-center gap-3">
            <ShieldCheck className="w-7 h-7 text-blue-500" />
            Bitácora de Auditoría y Seguridad
          </h1>
          <p className="text-sm text-slate-400">Historial inmutable de operaciones y cambios de configuración</p>
        </div>
      </div>

      {/* Buscador */}
      <div className="relative">
        <Search className="absolute left-4 top-3.5 w-5 h-5 text-slate-500" />
        <input
          type="text"
          placeholder="Filtrar por acción, usuario o detalle del registro..."
          value={filtro}
          onChange={(e) => setFiltro(e.target.value)}
          className="w-full bg-slate-900 border border-slate-800 rounded-xl pl-12 pr-4 py-3 text-white placeholder-slate-500 focus:outline-none focus:border-blue-500 transition text-sm"
        />
      </div>

      {/* Tabla de Logs */}
      <div className="bg-slate-900 border border-slate-800 rounded-2xl overflow-hidden shadow-xl">
        <table className="w-full text-left border-collapse">
          <thead>
            <tr className="border-b border-slate-800 bg-slate-950/50 text-slate-400 text-xs uppercase tracking-wider">
              <th className="p-4">Fecha / Hora (UTC)</th>
              <th className="p-4">Acción</th>
              <th className="p-4">Usuario</th>
              <th className="p-4">Detalle de la Operación</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-800/60 text-sm">
            {loading ? (
              <tr>
                <td colSpan={4} className="p-8 text-center text-slate-500">
                  Cargando bitácora de eventos...
                </td>
              </tr>
            ) : logsFiltrados.length === 0 ? (
              <tr>
                <td colSpan={4} className="p-8 text-center text-slate-500">
                  No se encontraron registros de auditoría.
                </td>
              </tr>
            ) : (
              logsFiltrados.map((log) => (
                <tr key={log.id} className="hover:bg-slate-800/30 transition">
                  <td className="p-4 text-xs font-mono text-slate-400 whitespace-nowrap">
                    <span className="inline-flex items-center gap-1.5">
                      <Clock className="w-3.5 h-3.5 text-blue-400" />
                      {new Date(log.fechaHoraUtc).toLocaleString()}
                    </span>
                  </td>
                  <td className="p-4">
                    <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-semibold bg-blue-500/10 text-blue-400 border border-blue-500/20">
                      <Activity className="w-3 h-3" />
                      {log.accion}
                    </span>
                  </td>
                  <td className="p-4 text-xs font-medium text-slate-300">
                    <span className="inline-flex items-center gap-1">
                      <User className="w-3.5 h-3.5 text-slate-500" />
                      {log.usuario}
                    </span>
                  </td>
                  <td className="p-4 text-xs text-slate-300 font-mono">{log.detalle}</td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}