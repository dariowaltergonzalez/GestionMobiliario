import { useState, useEffect, useCallback } from 'react'
import { Plus, Search, Pencil, Trash2, ChevronLeft, ChevronRight, AlertTriangle, Building2, FileDown, Link2, Check } from 'lucide-react'
import DashboardLayout from '../../../components/layout/DashboardLayout'
import PropietarioForm from './PropietarioForm'
import {
  getPropietarios, deletePropietario, generarTokenPortalPropietario,
  type PropietarioDto, type FiltrosPropietarios,
} from '../../../api/propietarios'
import { exportarPropietariosPdf } from '../../../api/reportes'

function formatFecha(iso: string) {
  const utc = iso.endsWith('Z') ? iso : iso + 'Z'
  return new Date(utc).toLocaleDateString('es-AR', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

export default function PropietariosPage() {
  const [propietarios, setPropietarios] = useState<PropietarioDto[]>([])
  const [totalRegistros, setTotalRegistros] = useState(0)
  const [totalPaginas, setTotalPaginas] = useState(1)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const [filtros, setFiltros] = useState<FiltrosPropietarios>({
    buscar: '', activo: 'true', pagina: 1, tamano: 10,
  })
  const [buscarInput, setBuscarInput] = useState('')

  const [modalAbierto, setModalAbierto] = useState(false)
  const [propietarioEditar, setPropietarioEditar] = useState<PropietarioDto | null>(null)
  const [confirmDelete, setConfirmDelete] = useState<PropietarioDto | null>(null)
  const [deletando, setDeletando] = useState(false)
  const [exportando, setExportando] = useState(false)
  const [copiadoId, setCopiadoId] = useState<number | null>(null)

  const cargar = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      const res = await getPropietarios(filtros)
      if (res.success) {
        setPropietarios(res.data.items)
        setTotalRegistros(res.data.totalRegistros)
        setTotalPaginas(res.data.totalPaginas)
      }
    } catch {
      setError('No se pudieron cargar los propietarios.')
    } finally {
      setLoading(false)
    }
  }, [filtros])

  useEffect(() => { cargar() }, [cargar])

  const handleBuscar = () => setFiltros(f => ({ ...f, buscar: buscarInput, pagina: 1 }))
  const handleEditar = (p: PropietarioDto) => { setPropietarioEditar(p); setModalAbierto(true) }
  const handleNuevo = () => { setPropietarioEditar(null); setModalAbierto(true) }
  const handleGuardado = () => { setModalAbierto(false); cargar() }

  const handleCopiarLinkPortal = async (p: PropietarioDto) => {
    try {
      const res = await generarTokenPortalPropietario(p.id)
      if (!res.success) { setError(res.errors?.[0] ?? res.message ?? 'No se pudo generar el link.'); return }
      const url = `${window.location.origin}/portal/propietario/${res.data.token}`
      await navigator.clipboard.writeText(url)
      setCopiadoId(p.id)
      setTimeout(() => setCopiadoId(null), 2000)
    } catch {
      setError('No se pudo generar el link del portal.')
    }
  }

  const handleExportar = async () => {
    setExportando(true)
    try {
      await exportarPropietariosPdf({ buscar: filtros.buscar, activo: filtros.activo })
    } catch {
      setError('No se pudo generar el PDF.')
    } finally {
      setExportando(false)
    }
  }

  const handleConfirmarDelete = async () => {
    if (!confirmDelete) return
    setDeletando(true)
    try {
      await deletePropietario(confirmDelete.id)
      setConfirmDelete(null)
      cargar()
    } catch {
      setError('No se pudo dar de baja el propietario.')
    } finally {
      setDeletando(false)
    }
  }

  const selectClass = 'border border-gray-200 rounded-xl px-3 py-2.5 text-sm text-gray-600 bg-white outline-none'

  return (
    <DashboardLayout titulo="Propietarios">

      {/* TOOLBAR */}
      <div className="flex flex-col sm:flex-row gap-3 mb-6">
        <div className="flex gap-2 flex-1">
          <div className="flex items-center gap-2 border border-gray-200 rounded-xl px-3 bg-white flex-1 max-w-xs">
            <Search className="w-4 h-4 text-gray-400 shrink-0" />
            <input
              type="text"
              placeholder="Buscar por nombre, apellido, DNI..."
              value={buscarInput}
              onChange={e => setBuscarInput(e.target.value)}
              onKeyDown={e => e.key === 'Enter' && handleBuscar()}
              className="py-2.5 text-sm outline-none w-full text-gray-700 placeholder-gray-400"
            />
          </div>
          <button
            onClick={handleBuscar}
            className="bg-blue-900 text-white px-4 py-2.5 rounded-xl text-sm font-medium hover:bg-blue-800 transition-colors"
          >
            Buscar
          </button>
        </div>

        <div className="flex gap-2">
          <select
            value={filtros.activo}
            onChange={e => setFiltros(f => ({ ...f, activo: e.target.value, pagina: 1 }))}
            className={selectClass}
          >
            <option value="true">Solo activos</option>
            <option value="false">Solo inactivos</option>
            <option value="">Todos</option>
          </select>
          <button
            onClick={handleExportar}
            disabled={exportando}
            className="flex items-center gap-2 border border-gray-200 text-gray-600 px-4 py-2.5 rounded-xl text-sm font-medium hover:bg-gray-50 disabled:opacity-60 transition-colors"
          >
            <FileDown className="w-4 h-4" />
            {exportando ? 'Generando...' : 'Exportar PDF'}
          </button>
          <button
            onClick={handleNuevo}
            className="flex items-center gap-2 bg-yellow-400 hover:bg-yellow-500 text-blue-900 font-semibold px-4 py-2.5 rounded-xl text-sm transition-colors"
          >
            <Plus className="w-4 h-4" />
            Nuevo
          </button>
        </div>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-xl mb-4 text-sm">{error}</div>
      )}

      {/* TABLA */}
      <div className="bg-white rounded-2xl shadow-sm border border-gray-100 overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-gray-100 bg-gray-50">
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Propietario</th>
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Contacto</th>
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">DNI / CUIT</th>
                <th className="text-center px-5 py-3.5 font-semibold text-gray-600">Propiedades</th>
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Alta</th>
                <th className="text-center px-5 py-3.5 font-semibold text-gray-600">Acciones</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                Array.from({ length: 5 }).map((_, i) => (
                  <tr key={i} className="border-b border-gray-50">
                    {Array.from({ length: 6 }).map((_, j) => (
                      <td key={j} className="px-5 py-4"><div className="h-4 bg-gray-100 rounded animate-pulse" /></td>
                    ))}
                  </tr>
                ))
              ) : propietarios.length === 0 ? (
                <tr>
                  <td colSpan={6} className="text-center py-16 text-gray-400">No se encontraron propietarios</td>
                </tr>
              ) : (
                propietarios.map(p => (
                  <tr key={p.id} className="border-b border-gray-50 hover:bg-gray-50 transition-colors">
                    <td className="px-5 py-4">
                      <div className="font-medium text-gray-800">{p.apellido}, {p.nombre}</div>
                      {!p.activo && (
                        <span className="text-xs text-red-500 font-medium">Inactivo</span>
                      )}
                    </td>
                    <td className="px-5 py-4 text-gray-500 text-xs">
                      {p.email && <div>{p.email}</div>}
                      {p.telefono && <div>{p.telefono}</div>}
                    </td>
                    <td className="px-5 py-4 text-gray-500 text-xs font-mono">
                      {p.dni && <div>DNI: {p.dni}</div>}
                      {p.cuit && <div>CUIT: {p.cuit}</div>}
                    </td>
                    <td className="px-5 py-4 text-center">
                      <span className="inline-flex items-center gap-1 text-xs text-gray-500">
                        <Building2 className="w-3.5 h-3.5" />
                        {p.cantidadPropiedades}
                      </span>
                    </td>
                    <td className="px-5 py-4 text-xs text-gray-400">{formatFecha(p.fechaCreacion)}</td>
                    <td className="px-5 py-4">
                      <div className="flex items-center justify-center gap-2">
                        <button
                          onClick={() => handleEditar(p)}
                          className="p-1.5 text-blue-600 hover:bg-blue-50 rounded-lg transition-colors"
                          title="Editar"
                        >
                          <Pencil className="w-4 h-4" />
                        </button>
                        <button
                          onClick={() => handleCopiarLinkPortal(p)}
                          className="p-1.5 text-teal-600 hover:bg-teal-50 rounded-lg transition-colors"
                          title="Copiar link del portal de autoservicio"
                        >
                          {copiadoId === p.id ? <Check className="w-4 h-4" /> : <Link2 className="w-4 h-4" />}
                        </button>
                        {p.activo && (
                          <button
                            onClick={() => setConfirmDelete(p)}
                            className="p-1.5 text-red-500 hover:bg-red-50 rounded-lg transition-colors"
                            title="Dar de baja"
                          >
                            <Trash2 className="w-4 h-4" />
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {!loading && totalRegistros > 0 && (
          <div className="px-5 py-4 border-t border-gray-100 flex items-center justify-between">
            <span className="text-sm text-gray-400">
              {totalRegistros} propietario{totalRegistros !== 1 ? 's' : ''} · Página {filtros.pagina} de {totalPaginas}
            </span>
            <div className="flex gap-1">
              <button
                onClick={() => setFiltros(f => ({ ...f, pagina: f.pagina - 1 }))}
                disabled={filtros.pagina <= 1}
                className="p-2 rounded-lg text-gray-400 hover:bg-gray-100 disabled:opacity-30 disabled:cursor-not-allowed transition-colors"
              >
                <ChevronLeft className="w-4 h-4" />
              </button>
              <button
                onClick={() => setFiltros(f => ({ ...f, pagina: f.pagina + 1 }))}
                disabled={filtros.pagina >= totalPaginas}
                className="p-2 rounded-lg text-gray-400 hover:bg-gray-100 disabled:opacity-30 disabled:cursor-not-allowed transition-colors"
              >
                <ChevronRight className="w-4 h-4" />
              </button>
            </div>
          </div>
        )}
      </div>

      {modalAbierto && (
        <PropietarioForm
          propietario={propietarioEditar}
          onGuardado={handleGuardado}
          onCerrar={() => setModalAbierto(false)}
        />
      )}

      {confirmDelete && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-2xl p-6 max-w-sm w-full shadow-xl">
            <div className="flex items-center gap-3 mb-4">
              <div className="w-10 h-10 bg-red-100 rounded-xl flex items-center justify-center">
                <AlertTriangle className="w-5 h-5 text-red-600" />
              </div>
              <h3 className="font-semibold text-gray-800">Dar de baja</h3>
            </div>
            <p className="text-sm text-gray-600 mb-6">
              ¿Confirmás dar de baja a <strong>{confirmDelete.apellido}, {confirmDelete.nombre}</strong>? Esta acción es reversible.
            </p>
            <div className="flex gap-3">
              <button
                onClick={() => setConfirmDelete(null)}
                className="flex-1 border border-gray-200 text-gray-600 py-2.5 rounded-xl text-sm font-medium hover:bg-gray-50 transition-colors"
              >
                Cancelar
              </button>
              <button
                onClick={handleConfirmarDelete}
                disabled={deletando}
                className="flex-1 bg-red-600 text-white py-2.5 rounded-xl text-sm font-medium hover:bg-red-700 disabled:opacity-60 transition-colors"
              >
                {deletando ? 'Procesando...' : 'Dar de baja'}
              </button>
            </div>
          </div>
        </div>
      )}

    </DashboardLayout>
  )
}
