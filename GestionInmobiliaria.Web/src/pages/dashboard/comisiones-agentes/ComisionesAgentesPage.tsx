import { useState, useEffect, useCallback } from 'react'
import { ChevronLeft, ChevronRight, Search, Wallet, X, Undo2 } from 'lucide-react'
import DashboardLayout from '../../../components/layout/DashboardLayout'
import { fechaLocal } from '../../../utils/fecha'
import {
  getComisionesAgentes, getComisionAgenteMetricas,
  registrarPagoComision, actualizarPagoComision, deshacerPagoComision,
  type ComisionAgenteDto, type ComisionAgenteMetricasDto,
  type FiltrosComisionesAgentes, type PagoComisionFormData,
} from '../../../api/comisionesAgentes'
import { MEDIOS_PAGO, medioPagoNumero } from '../../../api/contratos'

function formatFecha(iso: string) {
  return new Date(iso).toLocaleDateString('es-AR', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

function formatMonto(monto: number) {
  return `$ ${monto.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`
}

const ESTADO_BADGE: Record<string, string> = {
  Pendiente: 'bg-yellow-100 text-yellow-700',
  Pagado: 'bg-green-100 text-green-700',
}

// ── Modal: Registrar / editar pago ──────────────────────────────────────────

function PagoModal({ comision, onGuardado, onCerrar }: { comision: ComisionAgenteDto; onGuardado: () => void; onCerrar: () => void }) {
  const yaPagada = comision.estado === 'Pagado'
  const [fecha, setFecha] = useState(comision.fechaPago ? comision.fechaPago.split('T')[0] : fechaLocal())
  const [medio, setMedio] = useState(comision.medio ? medioPagoNumero(comision.medio) : 1)
  const [observaciones, setObservaciones] = useState(comision.observaciones ?? '')
  const [guardando, setGuardando] = useState(false)
  const [error, setError] = useState('')

  const handleGuardar = async () => {
    setGuardando(true)
    setError('')
    try {
      const datos: PagoComisionFormData = { fecha: new Date(fecha).toISOString(), medio, observaciones: observaciones.trim() || undefined }
      if (yaPagada) await actualizarPagoComision(comision.id, datos)
      else await registrarPagoComision(comision.id, datos)
      onGuardado()
    } catch {
      setError('No se pudo guardar el pago.')
    } finally {
      setGuardando(false)
    }
  }

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-2xl w-full max-w-md shadow-2xl">
        <div className="flex items-center justify-between px-6 py-4 border-b border-gray-100">
          <h2 className="font-bold text-gray-800">{yaPagada ? 'Editar pago' : 'Registrar pago'} de comisión</h2>
          <button onClick={onCerrar} className="p-1.5 hover:bg-gray-100 rounded-lg transition-colors">
            <X className="w-5 h-5 text-gray-500" />
          </button>
        </div>

        <div className="px-6 py-5 space-y-4">
          <p className="text-sm text-gray-500">
            {comision.agenteNombre} · {comision.contratoCodigo} · {formatMonto(comision.montoAgente)}
          </p>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Fecha de pago</label>
              <input
                type="date"
                value={fecha}
                onChange={e => setFecha(e.target.value)}
                className="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-blue-900/20 focus:border-blue-900"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Medio</label>
              <select
                value={medio}
                onChange={e => setMedio(Number(e.target.value))}
                className="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-blue-900/20 focus:border-blue-900"
              >
                {Object.entries(MEDIOS_PAGO).map(([num, label]) => (
                  <option key={num} value={num}>{label}</option>
                ))}
              </select>
            </div>
          </div>

          <div>
            <label className="block text-xs font-medium text-gray-600 mb-1">Observaciones</label>
            <textarea
              value={observaciones}
              onChange={e => setObservaciones(e.target.value)}
              rows={2}
              className="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-blue-900/20 focus:border-blue-900 resize-none"
            />
          </div>

          {error && <p className="text-sm text-red-600">{error}</p>}
        </div>

        <div className="px-6 py-4 border-t border-gray-100 flex justify-end gap-3">
          <button onClick={onCerrar} className="px-4 py-2 text-sm text-gray-600 hover:text-gray-800 rounded-xl border border-gray-200 hover:bg-gray-50 transition-colors">
            Cancelar
          </button>
          <button
            onClick={handleGuardar}
            disabled={guardando}
            className="px-5 py-2 text-sm font-semibold bg-blue-600 text-white rounded-xl hover:bg-blue-700 disabled:opacity-50 transition-colors"
          >
            {guardando ? 'Guardando...' : 'Guardar'}
          </button>
        </div>
      </div>
    </div>
  )
}

// ── Página ────────────────────────────────────────────────────────────────

export default function ComisionesAgentesPage() {
  const [items, setItems] = useState<ComisionAgenteDto[]>([])
  const [metricas, setMetricas] = useState<ComisionAgenteMetricasDto | null>(null)
  const [totalPaginas, setTotalPaginas] = useState(1)
  const [totalRegistros, setTotalRegistros] = useState(0)
  const [cargando, setCargando] = useState(false)
  const [pagoModal, setPagoModal] = useState<ComisionAgenteDto | null>(null)
  const [deshaciendo, setDeshaciendo] = useState<number | null>(null)

  const [buscarInput, setBuscarInput] = useState('')
  const [filtros, setFiltros] = useState<FiltrosComisionesAgentes>({ pagina: 1, tamano: 15 })

  const cargar = useCallback(async () => {
    setCargando(true)
    try {
      const res = await getComisionesAgentes(filtros)
      if (res.success && res.data) {
        setItems(res.data.items)
        setTotalPaginas(res.data.totalPaginas)
        setTotalRegistros(res.data.totalRegistros)
      }
    } finally {
      setCargando(false)
    }
  }, [filtros])

  const cargarMetricas = useCallback(async () => {
    const res = await getComisionAgenteMetricas()
    if (res.success && res.data) setMetricas(res.data)
  }, [])

  useEffect(() => { cargar() }, [cargar])
  useEffect(() => { cargarMetricas() }, [cargarMetricas])

  const handleGuardadoPago = () => {
    setPagoModal(null)
    cargar()
    cargarMetricas()
  }

  const handleDeshacer = async (id: number) => {
    if (!confirm('¿Deshacer el pago? La comisión vuelve a Pendiente.')) return
    setDeshaciendo(id)
    try {
      await deshacerPagoComision(id)
      cargar()
      cargarMetricas()
    } finally {
      setDeshaciendo(null)
    }
  }

  return (
    <DashboardLayout titulo="Comisiones de Agentes">
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        <div className="bg-white rounded-2xl border border-gray-100 p-4">
          <p className="text-xs text-yellow-600 font-semibold uppercase">Pendientes</p>
          <p className="text-2xl font-bold text-gray-800 mt-1">{metricas?.pendientesCount ?? '—'}</p>
        </div>
        <div className="bg-white rounded-2xl border border-gray-100 p-4">
          <p className="text-xs text-orange-600 font-semibold uppercase">Total pendiente</p>
          <p className="text-2xl font-bold text-gray-800 mt-1">{metricas ? formatMonto(metricas.montoTotalPendiente) : '—'}</p>
        </div>
        <div className="bg-white rounded-2xl border border-gray-100 p-4">
          <p className="text-xs text-green-600 font-semibold uppercase">Pagadas este mes</p>
          <p className="text-2xl font-bold text-gray-800 mt-1">{metricas?.pagadasMesCount ?? '—'}</p>
        </div>
        <div className="bg-white rounded-2xl border border-gray-100 p-4">
          <p className="text-xs text-blue-600 font-semibold uppercase">Pagado este mes</p>
          <p className="text-2xl font-bold text-gray-800 mt-1">{metricas ? formatMonto(metricas.montoPagadoMes) : '—'}</p>
        </div>
      </div>

      <div className="bg-white rounded-2xl border border-gray-100 overflow-hidden">
        <div className="p-4 border-b border-gray-100 flex flex-wrap items-center gap-3">
          <div className="relative flex-1 min-w-[220px]">
            <Search className="w-4 h-4 text-gray-400 absolute left-3 top-1/2 -translate-y-1/2" />
            <input
              type="text"
              value={buscarInput}
              onChange={e => setBuscarInput(e.target.value)}
              onKeyDown={e => e.key === 'Enter' && setFiltros(f => ({ ...f, pagina: 1, buscar: buscarInput || undefined }))}
              placeholder="Contrato, propiedad o agente..."
              className="w-full border border-gray-200 rounded-xl pl-10 pr-4 py-2 text-sm outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
          <select
            value={filtros.estado ?? ''}
            onChange={e => setFiltros(f => ({ ...f, pagina: 1, estado: e.target.value || undefined }))}
            className="border border-gray-200 rounded-xl px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            <option value="">Todos los estados</option>
            <option value="1">Pendiente</option>
            <option value="2">Pagado</option>
          </select>
          <span className="ml-auto text-sm text-gray-400">{totalRegistros} registros</span>
        </div>

        <table className="w-full text-sm">
          <thead>
            <tr className="text-left text-xs text-gray-400 uppercase border-b border-gray-100">
              <th className="px-5 py-3">Agente</th>
              <th className="px-5 py-3">Contrato</th>
              <th className="px-5 py-3">Propiedad</th>
              <th className="px-5 py-3 text-right">Comisión inmobiliaria</th>
              <th className="px-5 py-3 text-right">% agente</th>
              <th className="px-5 py-3 text-right">Monto agente</th>
              <th className="px-5 py-3 text-center">Estado</th>
              <th className="px-5 py-3">Fecha de pago</th>
              <th className="px-5 py-3 text-center">Acciones</th>
            </tr>
          </thead>
          <tbody>
            {cargando ? (
              <tr><td colSpan={9} className="py-16 text-center text-gray-400">Cargando...</td></tr>
            ) : items.length === 0 ? (
              <tr><td colSpan={9} className="py-16 text-center text-gray-400">No hay comisiones que coincidan con los filtros.</td></tr>
            ) : items.map(c => (
              <tr key={c.id} className="border-b border-gray-50 hover:bg-gray-50 transition-colors">
                <td className="px-5 py-3.5 font-medium text-gray-700">{c.agenteNombre}</td>
                <td className="px-5 py-3.5 text-blue-900 font-medium">{c.contratoCodigo}</td>
                <td className="px-5 py-3.5 text-gray-500">{c.propiedadDireccion}</td>
                <td className="px-5 py-3.5 text-right text-gray-600">{formatMonto(c.montoComisionInmobiliaria)}</td>
                <td className="px-5 py-3.5 text-right text-gray-600">{c.porcentajeAgente}%</td>
                <td className="px-5 py-3.5 text-right font-semibold text-gray-800">{formatMonto(c.montoAgente)}</td>
                <td className="px-5 py-3.5 text-center">
                  <span className={`inline-block px-2.5 py-1 rounded-full text-xs font-medium ${ESTADO_BADGE[c.estado] ?? 'bg-gray-100 text-gray-600'}`}>
                    {c.estado}
                  </span>
                </td>
                <td className="px-5 py-3.5 text-gray-500">{c.fechaPago ? formatFecha(c.fechaPago) : '—'}</td>
                <td className="px-5 py-3.5">
                  <div className="flex items-center justify-center gap-2">
                    {c.estado === 'Pendiente' ? (
                      <button
                        onClick={() => setPagoModal(c)}
                        className="flex items-center gap-1 text-xs font-semibold text-white bg-blue-600 hover:bg-blue-700 px-3 py-1.5 rounded-lg transition-colors whitespace-nowrap"
                      >
                        <Wallet className="w-3.5 h-3.5" /> Registrar pago
                      </button>
                    ) : (
                      <>
                        <button
                          onClick={() => setPagoModal(c)}
                          className="text-xs text-gray-500 hover:text-blue-600 transition-colors whitespace-nowrap"
                        >
                          Editar
                        </button>
                        <button
                          onClick={() => handleDeshacer(c.id)}
                          disabled={deshaciendo === c.id}
                          title="Deshacer el pago"
                          className="flex items-center gap-1 text-xs text-gray-400 hover:text-red-600 disabled:opacity-40 transition-colors whitespace-nowrap"
                        >
                          <Undo2 className="w-3.5 h-3.5" /> {deshaciendo === c.id ? '...' : 'Deshacer'}
                        </button>
                      </>
                    )}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>

        <div className="flex items-center justify-between px-5 py-3 border-t border-gray-100">
          <span className="text-xs text-gray-400">Página {filtros.pagina} de {totalPaginas}</span>
          <div className="flex gap-2">
            <button
              onClick={() => setFiltros(f => ({ ...f, pagina: Math.max(1, f.pagina - 1) }))}
              disabled={filtros.pagina <= 1}
              className="p-1.5 rounded-lg border border-gray-200 disabled:opacity-40 hover:bg-gray-50 transition-colors"
            >
              <ChevronLeft className="w-4 h-4" />
            </button>
            <button
              onClick={() => setFiltros(f => ({ ...f, pagina: Math.min(totalPaginas, f.pagina + 1) }))}
              disabled={filtros.pagina >= totalPaginas}
              className="p-1.5 rounded-lg border border-gray-200 disabled:opacity-40 hover:bg-gray-50 transition-colors"
            >
              <ChevronRight className="w-4 h-4" />
            </button>
          </div>
        </div>
      </div>

      {pagoModal && <PagoModal comision={pagoModal} onGuardado={handleGuardadoPago} onCerrar={() => setPagoModal(null)} />}
    </DashboardLayout>
  )
}
