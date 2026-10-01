'use client';

import { useEffect, useState } from 'react';
import { api } from '@/lib/api';
import { Users, Upload, Plus, Search, CheckCircle, XCircle } from 'lucide-react';

interface Empleado {
  id: number;
  nombreCompleto: string;
  codigoTrabajador: string;
  rutDni: string;
  cargo: string;
  activo: boolean;
}

export default function EmpleadosPage() {
  const [empleados, setEmpleados] = useState<Empleado[]>([]);
  const [filtro, setFiltro] = useState('');
  const [loading, setLoading] = useState(true);
  const [cargandoCsv, setCargandoCsv] = useState(false);
  const [mensajeCsv, setMensajeCsv] = useState<string | null>(null);

  const cargarEmpleados = async () => {
    try {
      setLoading(true);
      const res = await api.get('/Empleados');
      setEmpleados(res.data);
    } catch (err) {
      console.error('Error al obtener lista de empleados:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    cargarEmpleados();
  }, []);

  const handleSubirCsv = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    const formData = new FormData();
    formData.append('archivoCsv', file);

    try {
      setCargandoCsv(true);
      setMensajeCsv(null);
      const res = await api.post('/Empleados/cargar-masivo', formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
      });
      setMensajeCsv(`Éxito: ${res.data.totalInsertados} empleados agregados.`);
      cargarEmpleados();
    } catch (err: any) {
      setMensajeCsv('Error al procesar el archivo CSV.');
    } finally {
      setCargandoCsv(false);
    }
  };

  const empleadosFiltrados = empleados.filter(
    (e) =>
      e.nombreCompleto.toLowerCase().includes(filtro.toLowerCase()) ||
      e.codigoTrabajador.toLowerCase().includes(filtro.toLowerCase())
  );

  return (
    <div className="space-y-6">
      {/* Header del Módulo */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 bg-slate-900 border border-slate-800 p-6 rounded-2xl">
        <div>
          <h1 className="text-2xl font-bold text-white flex items-center gap-3">
            <Users className="w-7 h-7 text-blue-500" />
            Gestión de Empleados
          </h1>
          <p className="text-sm text-slate-400">Directorio unificado de personal por empresa</p>
        </div>

        <div className="flex items-center gap-3">
          <label className="cursor-pointer bg-slate-800 hover:bg-slate-700 text-slate-200 border border-slate-700 px-4 py-2.5 rounded-xl text-sm font-semibold flex items-center gap-2 transition">
            <Upload className="w-4 h-4 text-blue-400" />
            {cargandoCsv ? 'Procesando...' : 'Cargar CSV'}
            <input type="file" accept=".csv" onChange={handleSubirCsv} className="hidden" disabled={cargandoCsv} />
          </label>
        </div>
      </div>

      {mensajeCsv && (
        <div className="p-4 bg-blue-600/10 border border-blue-500/30 text-blue-400 rounded-xl text-sm">
          {mensajeCsv}
        </div>
      )}

      {/* Barra de Búsqueda */}
      <div className="relative">
        <Search className="absolute left-4 top-3.5 w-5 h-5 text-slate-500" />
        <input
          type="text"
          placeholder="Buscar por nombre o código de trabajador..."
          value={filtro}
          onChange={(e) => setFiltro(e.target.value)}
          className="w-full bg-slate-900 border border-slate-800 rounded-xl pl-12 pr-4 py-3 text-white placeholder-slate-500 focus:outline-none focus:border-blue-500 transition"
        />
      </div>

      {/* Tabla de Empleados */}
      <div className="bg-slate-900 border border-slate-800 rounded-2xl overflow-hidden shadow-xl">
        <table className="w-full text-left border-collapse">
          <thead>
            <tr className="border-b border-slate-800 bg-slate-950/50 text-slate-400 text-xs uppercase tracking-wider">
              <th className="p-4">Código</th>
              <th className="p-4">Nombre Completo</th>
              <th className="p-4">RUT / DNI</th>
              <th className="p-4">Cargo</th>
              <th className="p-4 text-center">Estado</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-800/60 text-sm">
            {loading ? (
              <tr>
                <td colSpan={5} className="p-8 text-center text-slate-500">
                  Cargando directorio de trabajadores...
                </td>
              </tr>
            ) : empleadosFiltrados.length === 0 ? (
              <tr>
                <td colSpan={5} className="p-8 text-center text-slate-500">
                  No se encontraron empleados registrados.
                </td>
              </tr>
            ) : (
              empleadosFiltrados.map((emp) => (
                <tr key={emp.id} className="hover:bg-slate-800/30 transition">
                  <td className="p-4 font-mono text-blue-400 font-semibold">{emp.codigoTrabajador}</td>
                  <td className="p-4 font-medium text-white">{emp.nombreCompleto}</td>
                  <td className="p-4 text-slate-400">{emp.rutDni || '-'}</td>
                  <td className="p-4 text-slate-400">{emp.cargo || 'Operativo'}</td>
                  <td className="p-4 text-center">
                    <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold bg-emerald-500/10 text-emerald-400 border border-emerald-500/20">
                      <CheckCircle className="w-3.5 h-3.5" />
                      Activo
                    </span>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}