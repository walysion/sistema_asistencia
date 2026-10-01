'use client';

import { useEffect, useState } from 'react';
import { api } from '@/lib/api';
import { Building2, Plus, Users, Monitor, MapPin, AlertCircle } from 'lucide-react';

interface EmpresaSaaS {
  id: number;
  razonSocial: string;
  documentoIdentidad: string;
  fechaCreacion: string;
  totalEmpleados: number;
  totalUsuarios: number;
  totalTerminales: number;
}

export default function AdminEmpresasPage() {
  const [empresas, setEmpresas] = useState<EmpresaSaaS[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalAbierto, setModalAbierto] = useState(false);
  const [guardando, setGuardando] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Formulario nueva empresa
  const [razonSocial, setRazonSocial] = useState('');
  const [documentoIdentidad, setDocumentoIdentidad] = useState('');

  const cargarEmpresas = async () => {
    try {
      setLoading(true);
      const res = await api.get('/Empresas');
      setEmpresas(res.data);
    } catch (err) {
      console.error('Error al cargar empresas:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    cargarEmpresas();
  }, []);

  const handleCrearEmpresa = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setGuardando(true);

    try {
      await api.post('/Empresas', {
        razonSocial,
        documentoIdentidad,
      });

      setModalAbierto(false);
      setRazonSocial('');
      setDocumentoIdentidad('');
      cargarEmpresas();
    } catch (err: any) {
      setError(err.response?.data?.message || err.response?.data || 'Error al registrar la empresa.');
    } finally {
      setGuardando(false);
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 bg-slate-900 border border-slate-800 p-6 rounded-2xl">
        <div>
          <h1 className="text-2xl font-bold text-white flex items-center gap-3">
            <Building2 className="w-7 h-7 text-blue-500" />
            SuperAdmin - Empresas Tenant
          </h1>
          <p className="text-sm text-slate-400">Aprovisionamiento global de organizaciones y métricas de plataforma</p>
        </div>

        <button
          onClick={() => setModalAbierto(true)}
          className="bg-blue-600 hover:bg-blue-500 text-white font-semibold px-4 py-2.5 rounded-xl text-sm flex items-center gap-2 shadow-lg shadow-blue-600/20 transition"
        >
          <Plus className="w-4 h-4" />
          Nueva Empresa SaaS
        </button>
      </div>

      {/* Grid de Empresas */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {loading ? (
          <p className="text-slate-500 text-sm col-span-3">Cargando directorio de clientes SaaS...</p>
        ) : empresas.length === 0 ? (
          <p className="text-slate-500 text-sm col-span-3">No hay empresas registradas.</p>
        ) : (
          empresas.map((emp) => (
            <div key={emp.id} className="bg-slate-900 border border-slate-800 p-5 rounded-2xl space-y-4">
              <div>
                <h3 className="font-bold text-white text-lg">{emp.razonSocial}</h3>
                <p className="text-xs text-blue-400 font-mono">RUT/Tax ID: {emp.documentoIdentidad}</p>
              </div>

              <div className="grid grid-cols-3 gap-2 pt-2 border-t border-slate-800/80 text-center">
                <div className="bg-slate-950 p-2 rounded-xl border border-slate-800/50">
                  <Users className="w-4 h-4 text-slate-400 mx-auto mb-1" />
                  <span className="block text-sm font-bold text-white">{emp.totalEmpleados}</span>
                  <span className="text-[10px] text-slate-500">Empleados</span>
                </div>
                <div className="bg-slate-950 p-2 rounded-xl border border-slate-800/50">
                  <Monitor className="w-4 h-4 text-slate-400 mx-auto mb-1" />
                  <span className="block text-sm font-bold text-white">{emp.totalTerminales}</span>
                  <span className="text-[10px] text-slate-500">Kioscos</span>
                </div>
                <div className="bg-slate-950 p-2 rounded-xl border border-slate-800/50">
                  <Building2 className="w-4 h-4 text-slate-400 mx-auto mb-1" />
                  <span className="block text-sm font-bold text-white">{emp.totalUsuarios}</span>
                  <span className="text-[10px] text-slate-500">Admins</span>
                </div>
              </div>
            </div>
          ))
        )}
      </div>

      {/* Modal Crear Empresa */}
      {modalAbierto && (
        <div className="fixed inset-0 bg-slate-950/80 backdrop-blur-sm flex items-center justify-center p-4 z-50">
          <div className="bg-slate-900 border border-slate-800 rounded-2xl p-6 max-w-md w-full space-y-5 shadow-2xl">
            <h2 className="text-xl font-bold text-white">Alta de Empresa SaaS</h2>

            {error && (
              <div className="flex items-center gap-2 bg-rose-500/10 border border-rose-500/30 text-rose-400 p-3 rounded-lg text-xs">
                <AlertCircle className="w-4 h-4 flex-shrink-0" />
                <span>{error}</span>
              </div>
            )}

            <form onSubmit={handleCrearEmpresa} className="space-y-4">
              <div>
                <label className="block text-xs font-semibold uppercase text-slate-400 mb-1">Razón Social</label>
                <input
                  type="text"
                  required
                  placeholder="Ej. Constructora del Norte SpA"
                  value={razonSocial}
                  onChange={(e) => setRazonSocial(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-blue-500"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold uppercase text-slate-400 mb-1">Documento Identidad / RUT</label>
                <input
                  type="text"
                  required
                  placeholder="Ej. 76.123.456-7"
                  value={documentoIdentidad}
                  onChange={(e) => setDocumentoIdentidad(e.target.value)}
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
                  {guardando ? 'Aprovisionando...' : 'Crear Organización'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}