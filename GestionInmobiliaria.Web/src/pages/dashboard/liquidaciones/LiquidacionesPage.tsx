import { useState, useEffect, useCallback } from 'react'
import { ChevronLeft, ChevronRight, Search, CheckCircle2 } from 'lucide-react'
import DashboardLayout from '../../../components/layout/DashboardLayout'
import {
  getLiquidaciones, getLiquidacionMetricas, marcarLiquidada,
  type LiquidacionDto, type LiquidacionMetricasDto, type FiltrosLiquidaciones,
} from '../../../api/liquidaciones'
import { getPropietariosActivos, type PropietarioComboDto } from '../../../api/propietarios'

function mesAnio(iso: string) {
  return new Date(iso).toLocaleDateString('es-AR', { month: 'short', year: 'numeric' })
}

function formatFecha(iso: string) {
  return new Date(iso).toLocaleDateString('es-AR', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

function formatMoneda(monto: number, moneda: string) {
  const fmt = monto.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
  return moneda === 'ARS' ? `$ ${fmt}` : `U$S ${fmt}`
}

function MetricCard({ label, value, color }: { label: string; value: string | number; color: string }) {
  return (
    <div className="bg-white rounded-2xl border border-gray-100 shadow-sm px-5 py-4">
      <p className="text-xs text-gray-400 mb-1">{label}</p>
      <p className={`text-xl font-bold ${color}`}>{value}</p>
    </div>
  )
}

export default function LiquidacionesPage() {
  const [metricas, setMetricas] = useState<LiquidacionMetricasDto | null>(null)
  const [items, setItems] = useState<LiquidacionDto[]>([])
  const [totalPaginas, setTotalPaginas] = useState(1)
  const [totalRegistros, setTotalRegistros] = useState(0)
  const [propietarios, setPropietarios] = useState<PropietarioComboDto[]>([])
  const [filtros, setFiltros] = useState<FiltrosLiquidaciones>({ estado: '1', pagina: 1, tamano: 15 })
  const [buscarInput, setBuscarInput] = useState('')
  const [cargando, setCargando] = useState(false)
  const [error, setError] = useState('')
  const [liquidando, setLiquidando] = useState<LiquidacionDto | null>(null)
  const [observaciones, setObservaciones] = useState('')
  const [guardando, setGuardando] = useState(false)

  const cargarMetricas = useCallback(async () => {
    try {
      const res = await getLiquidacionMetricas()
      if (res.success) setMetricas(res.data)
    } catch { /* no bloquea la pantalla */ }
  }, [])

  const cargar = useCallback(async () => {
    setCargando(true)
    setError('')
    try {
      const res = await getLiquidaciones(filtros)
      if (res.success) {
        setItems(res.data.items)
        setTotalPaginas(res.data.totalPaginas)
        setTotalRegistros(res.data.totalRegistros)
      }
    } catch {
      setError('No se pudieron cargar las liquidaciones.')
    } finally {
      setCargando(false)
    }
  }, [filtros])

  useEffect(() => { cargarMetricas() }, [cargarMetricas])
  useEffect(() => { cargar() }, [cargar])
  useEffect(() => {
    getPropietariosActivos().then(res => { if (res.success) setPropietarios(res.data) }).catch(() => {})
  }, [])

  const handleBuscar = () => setFiltros(f => ({ ...f, buscar: buscarInput || undefined, pagina: 1 }))

  const handleAbrirLiquidar = (l: LiquidacionDto) => { setLiquidando(l); setObservaciones('') }

  const handleConfirmarLiquidar = async () => {
    if (!liquidando) return
    setGuardando(true)
    try {
      await marcarLiquidada(liquidando.id, observaciones.trim() || undefined)
      setLiquidando(null)
      cargar()
      cargarMetricas()
    } catch {
      setError('No se pudo marcar la liquidación.')
    } finally {
      setGuardando(false)
    }
  }

  const selectClass = 'border border-gray-200 rounded-xl px-3 py-2.5 text-sm text-gray-600 bg-white outline-none'

  return (
    <DashboardLayout titulo="Liquidaciones">

      {/* Métricas */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        <MetricCard label="Pendientes"          value={metricas?.pendientesCount ?? '—'} color="text-yellow-600" />
        <MetricCard label="Total pendiente"      value={metricas ? formatMoneda(metricas.montoPendienteTotal, 'ARS') : '—'} color="text-orange-600" />
        <MetricCard label="Liquidadas este mes"  value={metricas?.liquidadasMesCount ?? '—'} color="text-green-600" />
        <MetricCard label="Liquidado este mes"   value={metricas ? formatMoneda(metricas.montoLiquidadoMes, 'ARS') : '—'} color="text-blue-600" />
      </div>

      {/* Filtros */}
      <div className="bg-white rounded-2xl border border-gray-100 shadow-sm mb-4 px-5 py-4">
        <div className="flex flex-wrap gap-3 items-center">
          <div className="flex items-center gap-2 border border-gray-200 rounded-xl px-3 bg-white">
            <Search className="w-4 h-4 text-gray-400 shrink-0" />
            <input
              type="text"
              placeholder="Código de contrato o propietario..."
              value={buscarInput}
              onChange={e => setBuscarInput(e.target.value)}
              onKeyDown={e => e.key === 'Enter' && handleBuscar()}
              className="py-2 text-sm outline-none w-56 text-gray-700 placeholder-gray-400"
            />
          </div>
          <button onClick={handleBuscar} className="bg-blue-900 text-white px-4 py-2 rounded-xl text-sm font-medium hover:bg-blue-800 transition-colors">
            Buscar
          </button>

          <select
            value={filtros.estado ?? ''}
            onChange={e => setFiltros(f => ({ ...f, pagina: 1, estado: e.target.value || undefined }))}
            className={selectClass}
          >
            <option value="">Todos los estados</option>
            <option value="1">Pendiente</option>
            <option value="2">Liquidado</option>
          </select>

          <select
            value={filtros.propietarioId ?? ''}
            onChange={e => setFiltros(f => ({ ...f, pagina: 1, propietarioId: e.target.value ? Number(e.target.value) : undefined }))}
            className={selectClass}
          >
            <option value="">Todos los propietarios</option>
            {propietarios.map(p => <option key={p.id} value={p.id}>{p.nombreCompleto}</option>)}
          </select>

          <span className="ml-auto text-sm text-gray-400">{totalRegistros} registros</span>
        </div>
      </div>

      {error && <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-xl mb-4 text-sm">{error}</div>}

      {/* Tabla */}
      <div className="bg-white rounded-2xl border border-gray-100 shadow-sm overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-gray-100 bg-gray-50">
                <th className="px-4 py-3 text-left font-semibold text-gray-600">Contrato</th>
                <th className="px-4 py-3 text-left font-semibold text-gray-600">Propietario</th>
                <th className="px-4 py-3 text-left font-semibold text-gray-600">Período</th>
                <th className="px-4 py-3 text-right font-semibold text-gray-600">Cobrado</th>
                <th className="px-4 py-3 text-right font-semibold text-gray-600">Comisión</th>
                <th className="px-4 py-3 text-right font-semibold text-gray-600">A liquidar</th>
                <th className="px-4 py-3 text-center font-semibold text-gray-600">Estado</th>
                <th className="px-4 py-3 text-center font-semibold text-gray-600">Acción</th>
              </tr>
            </thead>
            <tbody>
              {cargando ? (
                Array.from({ length: 5 }).map((_, i) => (
                  <tr key={i} className="border-b border-gray-50">
                    {Array.from({ length: 8 }).map((_, j) => (
                      <td key={j} className="px-4 py-4"><div className="h-4 bg-gray-100 rounded animate-pulse" /></td>
                    ))}
                  </tr>
                ))
              ) : items.length === 0 ? (
                <tr><td colSpan={8} className="text-center py-16 text-gray-400">No se encontraron liquidaciones</td></tr>
              ) : items.map(l => (
                <tr key={l.id} className="border-b border-gray-50 hover:bg-gray-50 transition-colors">
                  <td className="px-4 py-4">
                    <span className="font-mono text-blue-600">{l.contratoCodigo}</span>
                    <div className="text-xs text-gray-400">{l.propiedadDireccion}</div>
                  </td>
                  <td className="px-4 py-4 text-gray-700">{l.propietarioApellido}, {l.propietarioNombre}</td>
                  <td className="px-4 py-4 text-gray-600">
                    {mesAnio(l.periodo)}
                    <div className="text-xs text-gray-400">Cuota #{l.numeroCuota}</div>
                  </td>
                  <td className="px-4 py-4 text-right text-gray-700">{formatMoneda(l.montoCobrado, l.moneda)}</td>
                  <td className="px-4 py-4 text-right text-gray-500">
                    -{formatMoneda(l.montoComision, l.moneda)}
                    {l.comisionPorcentaje != null && <div className="text-xs text-gray-400">{l.comisionPorcentaje}%</div>}
                  </td>
                  <td className="px-4 py-4 text-right font-semibold text-gray-800">{formatMoneda(l.montoALiquidar, l.moneda)}</td>
                  <td className="px-4 py-4 text-center">
                    <span className={`text-xs px-2.5 py-1 rounded-full font-medium ${
                      l.estado === 'Liquidado' ? 'bg-green-100 text-green-700' : 'bg-yellow-100 text-yellow-700'
                    }`}>
                      {l.estado === 'Liquidado' ? `Liquidado ${l.fechaLiquidacion ? formatFecha(l.fechaLiquidacion) : ''}` : 'Pendiente'}
                    </span>
                  </td>
                  <td className="px-4 py-4 text-center">
                    {l.estado === 'Pendiente' && (
                      <button
                        onClick={() => handleAbrirLiquidar(l)}
                        className="inline-flex items-center gap-1 text-xs px-3 py-1.5 rounded-lg bg-blue-900 text-white hover:bg-blue-800 transition-colors"
                      >
                        <CheckCircle2 className="w-3.5 h-3.5" /> Marcar liquidado
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        {!cargando && totalRegistros > 0 && (
          <div className="px-5 py-4 border-t border-gray-100 flex items-center justify-between">
            <span className="text-sm text-gray-400">
              {totalRegistros} liquidaci{totalRegistros !== 1 ? 'ones' : 'ón'} · Página {filtros.pagina} de {totalPaginas}
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

      {/* Modal marcar liquidado */}
      {liquidando && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-2xl p-6 max-w-sm w-full shadow-xl">
            <div className="flex items-center gap-3 mb-4">
              <div className="w-10 h-10 bg-blue-100 rounded-xl flex items-center justify-center">
                <CheckCircle2 className="w-5 h-5 text-blue-700" />
              </div>
              <div>
                <h3 className="font-semibold text-gray-800">Marcar como liquidado</h3>
                <p className="text-xs text-gray-400 font-mono">{liquidando.contratoCodigo} · {mesAnio(liquidando.periodo)}</p>
              </div>
            </div>
            <p className="text-sm text-gray-600 mb-4">
              Confirmás que le transferiste <strong>{formatMoneda(liquidando.montoALiquidar, liquidando.moneda)}</strong> a{' '}
              <strong>{liquidando.propietarioNombre} {liquidando.propietarioApellido}</strong>.
            </p>
            <textarea
              value={observaciones}
              onChange={e => setObservaciones(e.target.value)}
              rows={2}
              placeholder="Observaciones (opcional): ej. transferencia N°, CBU..."
              className="w-full border border-gray-200 rounded-xl px-3 py-2 text-sm text-gray-700 outline-none focus:ring-2 focus:ring-blue-900/20 focus:border-blue-900 transition resize-none mb-4"
            />
            <div className="flex gap-3">
              <button onClick={() => setLiquidando(null)}
                className="flex-1 border border-gray-200 text-gray-600 py-2.5 rounded-xl text-sm font-medium hover:bg-gray-50 transition-colors">
                Cancelar
              </button>
              <button onClick={handleConfirmarLiquidar} disabled={guardando}
                className="flex-1 bg-blue-900 text-white py-2.5 rounded-xl text-sm font-medium hover:bg-blue-800 disabled:opacity-60 transition-colors">
                {guardando ? 'Guardando...' : 'Confirmar'}
              </button>
            </div>
          </div>
        </div>
      )}

    </DashboardLayout>
  )
}
