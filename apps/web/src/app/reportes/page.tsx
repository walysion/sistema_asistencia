'use client';

import { useState, useEffect } from 'react';
import { api } from '@/lib/api';
import { BarChart3, Download, Calendar, FileSpreadsheet, Search } from 'lucide-react';

interface ResumenEmpleadoReporte {
  empleadoId: number;
  nombreCompleto: string;
  codigoTrabajador: string;
  totalDiasAsistidos: number;
  totalMinutosAtraso: number;
  totalAlertasGeocerca: number;
  totalMarcaciones: number;
}

export default function ReportesPage() {
  const hoy = new Date().toISOString().split('T')[0];
  const haceUnMes = new Date(new Date().setDate(new Date().getDate() - 30)).toISOString().split('T')[0];

  const [fechaInicio, setFechaInicio] = useState(haceUnMes);
  const [fechaFin, setFechaFin] = useState(hoy);
  const [resumen, setResumen] = useState<ResumenEmpleadoReporte[]>([]);
  const [loading, setLoading] = useState(false);
  const [descargandoDetalle, setDescargandoDetalle] = useState(false);
  const [descargandoResumen, setDescargandoResumen] = useState(false);

  const cargarResumenReporte = async () => {
    try {
      setLoading(true);
      const res = await api.get('/Reportes/resumen-empleados', {
        params: { fechaInicio, fechaFin },
      });
      setResumen(res.data);
    } catch (err) {
      console.error('Error al obtener reporte de resumen:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    cargarResumenReporte();
  }, []);

  const handleExportarDetalleCsv = async () => {
    try {
      setDescargandoDetalle(true);
      const res = await api.get('/Reportes/exportar-csv', {
        params: { fechaInicio, fechaFin },
        responseType: 'blob',
      });

      const url = window.URL.createObjectURL(new Blob([res.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', `Reporte_Marcaciones_${fechaInicio}_a_${fechaFin}.csv`);
      document.body.appendChild(link);
      link.click();
      link.remove();
    } catch (err) {
      alert('Error al descargar el archivo CSV de marcaciones.');
    } finally {
      setDescargandoDetalle(false);
    }
  };

  const handleExportarResumenCsv = async () => {
    try {
      setDescargandoResumen(true);
      const res = await api.get('/Reportes/exportar-resumen-csv', {
        params: { fechaInicio, fechaFin },
        responseType: 'blob',
      });

      const url = window.URL.createObjectURL(new Blob([res.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', `Reporte_Resumen_Empleados_${fechaInicio}_a_${fechaFin}.csv`);
      document.body.appendChild(link);
      link.click();
      link.remove();
    } catch (err) {
      alert('Error al descargar el resumen CSV.');
    } finally {
      setDescargandoResumen(false);
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 bg-slate-900 border border-slate-800 p-6 rounded-2xl">
        <div>
          <h1 className="text-2xl font-bold text-white flex items-center gap-3">
            <BarChart3 className="w-7 h-7 text-blue-500" />
            Reportes y Exportación
          </h1>
          <p className="text-sm text-slate-400">Consolidado mensual de asistencia, minutos de atraso y alertas</p>
        </div>
      </div>

      {/* Barra de Filtros por Rango de Fechas */}
      <div className="bg-slate-900 border border-slate-800 p-6 rounded-2xl space-y-4">
        <h3 className="text-sm font-semibold uppercase text-slate-400 tracking-wider">Filtro de Fechas</h3>
        <div className="flex flex-col sm:flex-row items-end gap-4">
          <div className="w-full sm:w-auto flex-1">
            <label className="block text-xs font-medium text-slate-400 mb-1">Fecha Inicio</label>
            <div className="relative">
              <Calendar className="absolute left-3 top-2.5 w-4 h-4 text-slate-500" />
              <input
                type="date"
                value={fechaInicio}
                onChange={(e) => setFechaInicio(e.target.value)}
                className="w-full bg-slate-950 border border-slate-800 rounded-xl pl-10 pr-3 py-2 text-white text-sm focus:outline-none focus:border-blue-500"
              />
            </div>
          </div>

          <div className="w-full sm:w-auto flex-1">
            <label className="block text-xs font-medium text-slate-400 mb-1">Fecha Fin</label>
            <div className="relative">
              <Calendar className="absolute left-3 top-2.5 w-4 h-4 text-slate-500" />
              <input
                type="date"
                value={fechaFin}
                onChange={(e) => setFechaFin(e.target.value)}
                className="w-full bg-slate-950 border border-slate-800 rounded-xl pl-10 pr-3 py-2 text-white text-sm focus:outline-none focus:border-blue-500"
              />
            </div>
          </div>

          <button
            onClick={cargarResumenReporte}
            disabled={loading}
            className="w-full sm:w-auto bg-slate-800 hover:bg-slate-700 text-slate-200 border border-slate-700 px-5 py-2.5 rounded-xl text-sm font-semibold flex items-center justify-center gap-2 transition disabled:opacity-50"
          >
            <Search className="w-4 h-4 text-blue-400" />
            {loading ? 'Consultando...' : 'Aplicar Filtro'}
          </button>

          <button
            onClick={handleExportarDetalleCsv}
            disabled={descargandoDetalle}
            className="w-full sm:w-auto bg-blue-600 hover:bg-blue-500 text-white px-5 py-2.5 rounded-xl text-sm font-semibold flex items-center justify-center gap-2 shadow-lg shadow-blue-600/20 transition disabled:opacity-50"
          >
            <Download className="w-4 h-4" />
            {descargandoDetalle ? 'Generando...' : 'Descargar Marcaciones (CSV)'}
          </button>

          <button
            onClick={handleExportarResumenCsv}
            disabled={descargandoResumen}
            className="w-full sm:w-auto bg-emerald-600 hover:bg-emerald-500 text-white px-5 py-2.5 rounded-xl text-sm font-semibold flex items-center justify-center gap-2 shadow-lg shadow-emerald-600/20 transition disabled:opacity-50"
          >
            <FileSpreadsheet className="w-4 h-4" />
            {descargandoResumen ? 'Generando...' : 'Descargar Resumen Nómina (CSV)'}
          </button>
        </div>
      </div>

      {/* Tabla de Resumen por Empleado */}
      <div className="bg-slate-900 border border-slate-800 rounded-2xl overflow-hidden shadow-xl">
        <table className="w-full text-left border-collapse">
          <thead>
            <tr className="border-b border-slate-800 bg-slate-950/50 text-slate-400 text-xs uppercase tracking-wider">
              <th className="p-4">Código</th>
              <th className="p-4">Empleado</th>
              <th className="p-4 text-center">Días Asistidos</th>
              <th className="p-4 text-center">Total Minutos Atraso</th>
              <th className="p-4 text-center">Alertas Geocerca</th>
              <th className="p-4 text-center">Total Marcaciones</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-800/60 text-sm">
            {loading ? (
              <tr>
                <td colSpan={6} className="p-8 text-center text-slate-500">
                  Calculando resumen del período seleccionado...
                </td>
              </tr>
            ) : resumen.length === 0 ? (
              <tr>
                <td colSpan={6} className="p-8 text-center text-slate-500">
                  No hay datos registrados en este rango de fechas.
                </td>
              </tr>
            ) : (
              resumen.map((emp) => (
                <tr key={emp.empleadoId} className="hover:bg-slate-800/30 transition">
                  <td className="p-4 font-mono text-blue-400 font-semibold">{emp.codigoTrabajador}</td>
                  <td className="p-4 font-medium text-white">{emp.nombreCompleto}</td>
                  <td className="p-4 text-center font-semibold text-emerald-400">{emp.totalDiasAsistidos} días</td>
                  <td className="p-4 text-center">
                    <span className={`font-semibold ${emp.totalMinutosAtraso > 0 ? 'text-amber-400' : 'text-slate-400'}`}>
                      {emp.totalMinutosAtraso} min
                    </span>
                  </td>
                  <td className="p-4 text-center">
                    <span className={`font-semibold ${emp.totalAlertasGeocerca > 0 ? 'text-rose-400' : 'text-slate-400'}`}>
                      {emp.totalAlertasGeocerca}
                    </span>
                  </td>
                  <td className="p-4 text-center text-slate-300 font-mono">{emp.totalMarcaciones}</td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}