import { useState, useEffect, useCallback } from 'react'
import { Plus, Search, Pencil, Trash2, ChevronLeft, ChevronRight, MapPin, AlertTriangle, FileDown } from 'lucide-react'
import DashboardLayout from '../../../components/layout/DashboardLayout'
import PropiedadForm from './PropiedadForm'
import {
  getPropiedades, deletePropiedad,
  TIPOS_PROPIEDAD, TIPOS_OPERACION, ESTADOS_PROPIEDAD,
  type PropiedadDto, type FiltrosPropiedades,
} from '../../../api/propiedades'
import { exportarPropiedadesPdf } from '../../../api/reportes'

const BADGE_ESTADO: Record<number, string> = {
  1: 'bg-green-100 text-green-700',
  2: 'bg-blue-100 text-blue-700',
  3: 'bg-yellow-100 text-yellow-700',
  4: 'bg-red-100 text-red-700',
  5: 'bg-purple-100 text-purple-700',
  6: 'bg-orange-100 text-orange-700',
}

const BADGE_OPERACION: Record<number, string> = {
  1: 'bg-blue-50 text-blue-700',
  2: 'bg-emerald-50 text-emerald-700',
  3: 'bg-violet-50 text-violet-700',
}

function PrecioCell({ p }: { p: PropiedadDto }) {
  const tieneAlquiler = p.precioAlquiler != null
  const tieneVenta = p.precioVenta != null
  return (
    <div className="text-right">
      {tieneAlquiler && (
        <div className="font-semibold text-gray-800 text-sm">
          ${p.precioAlquiler!.toLocaleString('es-AR')}
          <span className="text-xs font-normal text-gray-400 ml-1">ARS</span>
        </div>
      )}
      {tieneVenta && (
        <div className="font-semibold text-gray-800 text-sm">
          U$S {p.precioVenta!.toLocaleString('es-AR')}
        </div>
      )}
    </div>
  )
}

export default function PropiedadesPage() {
  const [propiedades, setPropiedades] = useState<PropiedadDto[]>([])
  const [totalRegistros, setTotalRegistros] = useState(0)
  const [totalPaginas, setTotalPaginas] = useState(1)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const [filtros, setFiltros] = useState<FiltrosPropiedades>({
    buscar: '', tipo: '', estado: '', operacion: '', pagina: 1, tamano: 10,
  })
  const [buscarInput, setBuscarInput] = useState('')

  const [modalAbierto, setModalAbierto] = useState(false)
  const [propiedadEditar, setPropiedadEditar] = useState<PropiedadDto | null>(null)

  const [confirmDelete, setConfirmDelete] = useState<PropiedadDto | null>(null)
  const [deletando, setDeletando] = useState(false)
  const [exportando, setExportando] = useState(false)

  const cargar = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      const res = await getPropiedades(filtros)
      if (res.success) {
        setPropiedades(res.data.items)
        setTotalRegistros(res.data.totalRegistros)
        setTotalPaginas(res.data.totalPaginas)
      }
    } catch {
      setError('No se pudieron cargar las propiedades.')
    } finally {
      setLoading(false)
    }
  }, [filtros])

  useEffect(() => { cargar() }, [cargar])

  const handleBuscar = () => setFiltros(f => ({ ...f, buscar: buscarInput, pagina: 1 }))

  const handleFiltro = (campo: keyof FiltrosPropiedades, valor: string) =>
    setFiltros(f => ({ ...f, [campo]: valor, pagina: 1 }))

  const handleEditar = (p: PropiedadDto) => { setPropiedadEditar(p); setModalAbierto(true) }
  const handleNueva = () => { setPropiedadEditar(null); setModalAbierto(true) }
  const handleGuardado = () => { setModalAbierto(false); cargar() }

  const handleExportar = async () => {
    setExportando(true)
    try {
      await exportarPropiedadesPdf({
        buscar: filtros.buscar || undefined,
        tipo: filtros.tipo || undefined,
        estado: filtros.estado || undefined,
        operacion: filtros.operacion || undefined,
      })
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
      await deletePropiedad(confirmDelete.id)
      setConfirmDelete(null)
      cargar()
    } catch {
      setError('No se pudo dar de baja la propiedad.')
    } finally {
      setDeletando(false)
    }
  }

  const selectClass = 'border border-gray-200 rounded-xl px-3 py-2.5 text-sm text-gray-600 bg-white outline-none'

  return (
    <DashboardLayout titulo="Propiedades">

      {/* TOOLBAR */}
      <div className="flex flex-col sm:flex-row gap-3 mb-6">
        <div className="flex gap-2 flex-1">
          <div className="flex items-center gap-2 border border-gray-200 rounded-xl px-3 bg-white flex-1 max-w-xs">
            <Search className="w-4 h-4 text-gray-400 shrink-0" />
            <input
              type="text"
              placeholder="Buscar por dirección..."
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

        <div className="flex gap-2 flex-wrap">
          <select value={filtros.operacion} onChange={e => handleFiltro('operacion', e.target.value)} className={selectClass}>
            <option value="">Toda operación</option>
            {Object.entries(TIPOS_OPERACION).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
          </select>
          <select value={filtros.tipo} onChange={e => handleFiltro('tipo', e.target.value)} className={selectClass}>
            <option value="">Todos los tipos</option>
            {Object.entries(TIPOS_PROPIEDAD).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
          </select>
          <select value={filtros.estado} onChange={e => handleFiltro('estado', e.target.value)} className={selectClass}>
            <option value="">Todos los estados</option>
            {Object.entries(ESTADOS_PROPIEDAD).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
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
            onClick={handleNueva}
            className="flex items-center gap-2 bg-yellow-400 hover:bg-yellow-500 text-blue-900 font-semibold px-4 py-2.5 rounded-xl text-sm transition-colors"
          >
            <Plus className="w-4 h-4" />
            Nueva
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
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Propiedad</th>
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Tipo · Operación</th>
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Estado</th>
                <th className="text-right px-5 py-3.5 font-semibold text-gray-600">Precio</th>
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Propietario</th>
                <th className="text-center px-5 py-3.5 font-semibold text-gray-600">Acciones</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                Array.from({ length: 5 }).map((_, i) => (
                  <tr key={i} className="border-b border-gray-50">
                    {Array.from({ length: 6 }).map((_, j) => (
                      <td key={j} className="px-5 py-4">
                        <div className="h-4 bg-gray-100 rounded animate-pulse" />
                      </td>
                    ))}
                  </tr>
                ))
              ) : propiedades.length === 0 ? (
                <tr>
                  <td colSpan={6} className="text-center py-16 text-gray-400">No se encontraron propiedades</td>
                </tr>
              ) : (
                propiedades.map(p => (
                  <tr key={p.id} className="border-b border-gray-50 hover:bg-gray-50 transition-colors">
                    <td className="px-5 py-4">
                      <div className="flex items-start gap-2">
                        <MapPin className="w-3.5 h-3.5 text-gray-400 mt-0.5 shrink-0" />
                        <div>
                          <div className="font-medium text-gray-800">{p.direccion}</div>
                          {p.barrio && <div className="text-xs text-gray-400">{p.barrio}{p.ciudad ? `, ${p.ciudad}` : ''}</div>}
                        </div>
                      </div>
                    </td>
                    <td className="px-5 py-4">
                      <div className="text-gray-600">{p.tipoNombre}</div>
                      <span className={`mt-1 inline-block text-xs px-2 py-0.5 rounded-full font-medium ${BADGE_OPERACION[p.operacion] ?? 'bg-gray-100 text-gray-600'}`}>
                        {TIPOS_OPERACION[p.operacion]}
                      </span>
                    </td>
                    <td className="px-5 py-4">
                      <span className={`px-2.5 py-1 rounded-full text-xs font-medium ${BADGE_ESTADO[p.estado] ?? 'bg-gray-100 text-gray-600'}`}>
                        {p.estadoNombre}
                      </span>
                    </td>
                    <td className="px-5 py-4">
                      <PrecioCell p={p} />
                    </td>
                    <td className="px-5 py-4 text-gray-600">{p.propietarioNombre}</td>
                    <td className="px-5 py-4">
                      <div className="flex items-center justify-center gap-2">
                        <button onClick={() => handleEditar(p)} className="p-1.5 text-blue-600 hover:bg-blue-50 rounded-lg transition-colors" title="Editar">
                          <Pencil className="w-4 h-4" />
                        </button>
                        <button onClick={() => setConfirmDelete(p)} className="p-1.5 text-red-500 hover:bg-red-50 rounded-lg transition-colors" title="Dar de baja">
                          <Trash2 className="w-4 h-4" />
                        </button>
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
              {totalRegistros} propiedad{totalRegistros !== 1 ? 'es' : ''} · Página {filtros.pagina} de {totalPaginas}
            </span>
            <div className="flex gap-1">
              <button onClick={() => setFiltros(f => ({ ...f, pagina: f.pagina - 1 }))} disabled={filtros.pagina <= 1}
                className="p-2 rounded-lg text-gray-400 hover:bg-gray-100 disabled:opacity-30 disabled:cursor-not-allowed transition-colors">
                <ChevronLeft className="w-4 h-4" />
              </button>
              <button onClick={() => setFiltros(f => ({ ...f, pagina: f.pagina + 1 }))} disabled={filtros.pagina >= totalPaginas}
                className="p-2 rounded-lg text-gray-400 hover:bg-gray-100 disabled:opacity-30 disabled:cursor-not-allowed transition-colors">
                <ChevronRight className="w-4 h-4" />
              </button>
            </div>
          </div>
        )}
      </div>

      {modalAbierto && (
        <PropiedadForm propiedad={propiedadEditar} onGuardado={handleGuardado} onCerrar={() => setModalAbierto(false)} />
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
              ¿Confirmás dar de baja la propiedad en <strong>{confirmDelete.direccion}</strong>? Esta acción es reversible.
            </p>
            <div className="flex gap-3">
              <button onClick={() => setConfirmDelete(null)} className="flex-1 border border-gray-200 text-gray-600 py-2.5 rounded-xl text-sm font-medium hover:bg-gray-50 transition-colors">
                Cancelar
              </button>
              <button onClick={handleConfirmarDelete} disabled={deletando} className="flex-1 bg-red-600 text-white py-2.5 rounded-xl text-sm font-medium hover:bg-red-700 disabled:opacity-60 transition-colors">
                {deletando ? 'Procesando...' : 'Dar de baja'}
              </button>
            </div>
          </div>
        </div>
      )}

    </DashboardLayout>
  )
}
