import { useState, useEffect, useCallback } from 'react'
import { Plus, Search, X, Save, Trash2, AlertTriangle, ChevronLeft, ChevronRight, CalendarClock, FileDown } from 'lucide-react'
import DashboardLayout from '../../../components/layout/DashboardLayout'
import {
  getReservas, getReserva, createReserva, updateReserva, deleteReserva,
  ESTADOS_RESERVA, MEDIOS_DEPOSITO, estadoReservaNumero,
  type ReservaDto, type FiltrosReservas, type CreateReservaRequest, type UpdateReservaRequest,
} from '../../../api/reservas'
import { exportarReservasPdf } from '../../../api/reportes'
import client from '../../../api/client'
import type { ApiResponse } from '../../../types/api'

interface PropiedadCombo { id: number; direccion: string; propietarioNombre: string; propietarioApellido: string; propietarioDni: string | null; propietarioTelefono: string | null; propietarioEmail: string | null }
interface AgenteCombo { id: number; nombreCompleto: string }
interface LeadCombo { id: number; nombre: string; apellido: string; dni?: string; telefono?: string; email?: string }

function formatFecha(iso: string) {
  const utc = iso.endsWith('Z') ? iso : iso + 'Z'
  return new Date(utc).toLocaleDateString('es-AR', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

function toDateInput(iso: string) {
  return iso.slice(0, 10)
}

function formatMoneda(v: number | null | undefined, moneda: string) {
  if (v == null) return '—'
  const simbolo = moneda === 'USD' ? 'U$S' : '$'
  return `${simbolo} ${new Intl.NumberFormat('es-AR', { maximumFractionDigits: 0 }).format(v)}`
}

function EstadoBadge({ estado }: { estado: string }) {
  const num = estadoReservaNumero(estado) as keyof typeof ESTADOS_RESERVA
  const info = ESTADOS_RESERVA[num] ?? { label: estado, color: 'bg-gray-100 text-gray-500' }
  return <span className={`text-xs font-semibold px-2.5 py-1 rounded-full ${info.color}`}>{info.label}</span>
}

function diasRestantes(fechaVenc: string) {
  const hoy = new Date(); hoy.setHours(0, 0, 0, 0)
  const venc = new Date(fechaVenc.endsWith('Z') ? fechaVenc : fechaVenc + 'Z'); venc.setHours(0, 0, 0, 0)
  return Math.round((venc.getTime() - hoy.getTime()) / 86400000)
}

// ── Formulario ──────────────────────────────────────────────────────────────

interface ReservaFormProps {
  reserva: ReservaDto | null
  propiedades: PropiedadCombo[]
  agentes: AgenteCombo[]
  leads: LeadCombo[]
  onGuardado: () => void
  onCerrar: () => void
}

function ReservaForm({ reserva, propiedades, agentes, leads, onGuardado, onCerrar }: ReservaFormProps) {
  const hoy = new Date().toISOString().slice(0, 10)
  const en15 = new Date(Date.now() + 15 * 86400000).toISOString().slice(0, 10)

  const [form, setForm] = useState({
    propiedadId: reserva ? String(reserva.propiedadId) : '',
    leadId: reserva?.leadId ? String(reserva.leadId) : '',
    agenteId: reserva?.agenteId ? String(reserva.agenteId) : '',
    // Comprador
    compradorNombre: reserva?.compradorNombre ?? '',
    compradorApellido: reserva?.compradorApellido ?? '',
    compradorDni: reserva?.compradorDni ?? '',
    compradorTelefono: reserva?.compradorTelefono ?? '',
    compradorEmail: reserva?.compradorEmail ?? '',
    // Vendedor
    vendedorNombre: reserva?.vendedorNombre ?? '',
    vendedorApellido: reserva?.vendedorApellido ?? '',
    vendedorDni: reserva?.vendedorDni ?? '',
    vendedorTelefono: reserva?.vendedorTelefono ?? '',
    vendedorEmail: reserva?.vendedorEmail ?? '',
    // Económico
    montoSenia: reserva ? String(reserva.montoSenia) : '',
    precioTotal: reserva?.precioTotal ? String(reserva.precioTotal) : '',
    moneda: reserva ? (reserva.moneda === 'ARS' ? '1' : '2') : '2',
    medioDeposito: reserva ? String(Object.entries(MEDIOS_DEPOSITO).find(([, v]) => v === reserva.medioDeposito)?.[0] ?? '1') : '1',
    comisionVendedorPorcentaje: reserva?.comisionVendedorPorcentaje != null ? String(reserva.comisionVendedorPorcentaje) : '',
    comisionVendedorMonto: reserva?.comisionVendedorMonto != null ? String(reserva.comisionVendedorMonto) : '',
    comisionCompradorPorcentaje: reserva?.comisionCompradorPorcentaje != null ? String(reserva.comisionCompradorPorcentaje) : '',
    comisionCompradorMonto: reserva?.comisionCompradorMonto != null ? String(reserva.comisionCompradorMonto) : '',
    // Vigencia
    fechaReserva: reserva ? toDateInput(reserva.fechaReserva) : hoy,
    fechaVencimiento: reserva ? toDateInput(reserva.fechaVencimiento) : en15,
    estado: reserva ? String(estadoReservaNumero(reserva.estado)) : '1',
    observaciones: reserva?.observaciones ?? '',
  })

  const [guardando, setGuardando] = useState(false)
  const [error, setError] = useState('')

  const set = (f: string, v: string) => setForm(p => ({ ...p, [f]: v }))

  // Auto-poblar vendedor cuando cambia la propiedad
  const handlePropiedadChange = (propId: string) => {
    set('propiedadId', propId)
    const prop = propiedades.find(p => String(p.id) === propId)
    if (prop) {
      setForm(prev => ({
        ...prev,
        propiedadId: propId,
        vendedorNombre: prop.propietarioNombre,
        vendedorApellido: prop.propietarioApellido,
        vendedorDni: prop.propietarioDni ?? '',
        vendedorTelefono: prop.propietarioTelefono ?? '',
        vendedorEmail: prop.propietarioEmail ?? '',
      }))
    }
  }

  // Auto-poblar comprador cuando cambia el lead
  const handleLeadChange = (leadId: string) => {
    set('leadId', leadId)
    const lead = leads.find(l => String(l.id) === leadId)
    if (lead) {
      setForm(prev => ({
        ...prev,
        leadId,
        compradorNombre: lead.nombre,
        compradorApellido: lead.apellido,
        compradorDni: lead.dni ?? '',
        compradorTelefono: lead.telefono ?? '',
        compradorEmail: lead.email ?? '',
      }))
    }
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!form.propiedadId) { setError('Seleccioná una propiedad.'); return }
    if (!form.compradorNombre || !form.compradorApellido) { setError('Datos del comprador requeridos.'); return }
    if (!form.vendedorNombre || !form.vendedorApellido) { setError('Datos del vendedor requeridos.'); return }
    if (!form.montoSenia || Number(form.montoSenia) <= 0) { setError('El monto de la seña debe ser mayor a cero.'); return }
    if (form.fechaVencimiento <= form.fechaReserva) { setError('La fecha de vencimiento debe ser posterior a la de reserva.'); return }

    setGuardando(true); setError('')
    try {
      const payload: CreateReservaRequest = {
        propiedadId: Number(form.propiedadId),
        leadId: form.leadId ? Number(form.leadId) : null,
        agenteId: form.agenteId ? Number(form.agenteId) : null,
        compradorNombre: form.compradorNombre.trim(),
        compradorApellido: form.compradorApellido.trim(),
        compradorDni: form.compradorDni.trim() || undefined,
        compradorTelefono: form.compradorTelefono.trim() || undefined,
        compradorEmail: form.compradorEmail.trim() || undefined,
        vendedorNombre: form.vendedorNombre.trim(),
        vendedorApellido: form.vendedorApellido.trim(),
        vendedorDni: form.vendedorDni.trim() || undefined,
        vendedorTelefono: form.vendedorTelefono.trim() || undefined,
        vendedorEmail: form.vendedorEmail.trim() || undefined,
        montoSenia: Number(form.montoSenia),
        precioTotal: form.precioTotal ? Number(form.precioTotal) : undefined,
        moneda: Number(form.moneda),
        medioDeposito: Number(form.medioDeposito),
        comisionVendedorPorcentaje: form.comisionVendedorPorcentaje ? Number(form.comisionVendedorPorcentaje) : undefined,
        comisionVendedorMonto: form.comisionVendedorMonto ? Number(form.comisionVendedorMonto) : undefined,
        comisionCompradorPorcentaje: form.comisionCompradorPorcentaje ? Number(form.comisionCompradorPorcentaje) : undefined,
        comisionCompradorMonto: form.comisionCompradorMonto ? Number(form.comisionCompradorMonto) : undefined,
        fechaReserva: new Date(form.fechaReserva).toISOString(),
        fechaVencimiento: new Date(form.fechaVencimiento).toISOString(),
        observaciones: form.observaciones.trim() || undefined,
      }
      if (reserva) {
        await updateReserva(reserva.id, { ...payload, estado: Number(form.estado) } as UpdateReservaRequest)
      } else {
        await createReserva(payload)
      }
      onGuardado()
    } catch {
      setError('No se pudo guardar la reserva.')
    } finally {
      setGuardando(false)
    }
  }

  const inp = 'w-full border border-gray-200 rounded-xl px-3 py-2 text-sm text-gray-700 focus:outline-none focus:ring-2 focus:ring-blue-900/30'
  const sec = 'text-xs font-semibold text-gray-400 uppercase tracking-wide mb-3 mt-1'

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-2xl max-h-[92vh] flex flex-col">
        <div className="flex items-center justify-between px-6 py-4 border-b border-gray-100 shrink-0">
          <h2 className="font-semibold text-gray-800">{reserva ? 'Editar reserva' : 'Nueva reserva'}</h2>
          <button onClick={onCerrar} className="p-1.5 hover:bg-gray-100 rounded-lg transition-colors cursor-pointer">
            <X className="w-4 h-4 text-gray-500" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="flex-1 overflow-y-auto p-6 space-y-5">

          {/* Propiedad y agente */}
          <div>
            <p className={sec}>Propiedad</p>
            <div className="grid grid-cols-2 gap-3">
              <div className="col-span-2">
                <label className="block text-xs font-medium text-gray-600 mb-1">Propiedad *</label>
                <select
                  value={form.propiedadId}
                  onChange={e => handlePropiedadChange(e.target.value)}
                  className={inp}
                  disabled={!!reserva}
                >
                  <option value="">Seleccionar propiedad...</option>
                  {propiedades.map(p => (
                    <option key={p.id} value={p.id}>{p.direccion}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Agente</label>
                <select value={form.agenteId} onChange={e => set('agenteId', e.target.value)} className={inp}>
                  <option value="">Sin asignar</option>
                  {agentes.map(a => <option key={a.id} value={a.id}>{a.nombreCompleto}</option>)}
                </select>
              </div>
              {reserva && (
                <div>
                  <label className="block text-xs font-medium text-gray-600 mb-1">Estado</label>
                  <select value={form.estado} onChange={e => set('estado', e.target.value)} className={inp}>
                    {Object.entries(ESTADOS_RESERVA).map(([k, v]) => (
                      <option key={k} value={k}>{v.label}</option>
                    ))}
                  </select>
                </div>
              )}
            </div>
          </div>

          {/* Comprador */}
          <div>
            <p className={sec}>Comprador</p>
            <div className="mb-2">
              <label className="block text-xs font-medium text-gray-600 mb-1">Lead (autocompletar)</label>
              <select value={form.leadId} onChange={e => handleLeadChange(e.target.value)} className={inp}>
                <option value="">Seleccionar lead...</option>
                {leads.map(l => <option key={l.id} value={l.id}>{l.apellido}, {l.nombre}</option>)}
              </select>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Nombre *</label>
                <input type="text" value={form.compradorNombre} onChange={e => set('compradorNombre', e.target.value)} className={inp} />
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Apellido *</label>
                <input type="text" value={form.compradorApellido} onChange={e => set('compradorApellido', e.target.value)} className={inp} />
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">DNI</label>
                <input type="text" value={form.compradorDni} onChange={e => set('compradorDni', e.target.value)} className={inp} />
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Teléfono</label>
                <input type="tel" value={form.compradorTelefono} onChange={e => set('compradorTelefono', e.target.value)} className={inp} />
              </div>
              <div className="col-span-2">
                <label className="block text-xs font-medium text-gray-600 mb-1">Email</label>
                <input type="email" value={form.compradorEmail} onChange={e => set('compradorEmail', e.target.value)} className={inp} />
              </div>
            </div>
          </div>

          {/* Vendedor */}
          <div>
            <p className={sec}>Vendedor (propietario)</p>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Nombre *</label>
                <input type="text" value={form.vendedorNombre} onChange={e => set('vendedorNombre', e.target.value)} className={inp} />
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Apellido *</label>
                <input type="text" value={form.vendedorApellido} onChange={e => set('vendedorApellido', e.target.value)} className={inp} />
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">DNI</label>
                <input type="text" value={form.vendedorDni} onChange={e => set('vendedorDni', e.target.value)} className={inp} />
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Teléfono</label>
                <input type="tel" value={form.vendedorTelefono} onChange={e => set('vendedorTelefono', e.target.value)} className={inp} />
              </div>
              <div className="col-span-2">
                <label className="block text-xs font-medium text-gray-600 mb-1">Email</label>
                <input type="email" value={form.vendedorEmail} onChange={e => set('vendedorEmail', e.target.value)} className={inp} />
              </div>
            </div>
          </div>

          {/* Condiciones económicas */}
          <div>
            <p className={sec}>Condiciones económicas</p>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Moneda</label>
                <select value={form.moneda} onChange={e => set('moneda', e.target.value)} className={inp}>
                  <option value="2">USD (dólares)</option>
                  <option value="1">ARS (pesos)</option>
                </select>
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Medio de depósito</label>
                <select value={form.medioDeposito} onChange={e => set('medioDeposito', e.target.value)} className={inp}>
                  {Object.entries(MEDIOS_DEPOSITO).map(([k, v]) => (
                    <option key={k} value={k}>{v}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Monto seña *</label>
                <input type="number" min={0} value={form.montoSenia} onChange={e => set('montoSenia', e.target.value)} className={inp} placeholder="0" />
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Precio total pactado</label>
                <input type="number" min={0} value={form.precioTotal} onChange={e => set('precioTotal', e.target.value)} className={inp} placeholder="0" />
              </div>
            </div>
          </div>

          {/* Comisiones */}
          <div>
            <p className={sec}>Comisiones de la inmobiliaria</p>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Comisión vendedor %</label>
                <input type="number" min={0} step={0.01} value={form.comisionVendedorPorcentaje} onChange={e => set('comisionVendedorPorcentaje', e.target.value)} className={inp} placeholder="0.00" />
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Comisión vendedor monto</label>
                <input type="number" min={0} value={form.comisionVendedorMonto} onChange={e => set('comisionVendedorMonto', e.target.value)} className={inp} placeholder="0" />
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Comisión comprador %</label>
                <input type="number" min={0} step={0.01} value={form.comisionCompradorPorcentaje} onChange={e => set('comisionCompradorPorcentaje', e.target.value)} className={inp} placeholder="0.00" />
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Comisión comprador monto</label>
                <input type="number" min={0} value={form.comisionCompradorMonto} onChange={e => set('comisionCompradorMonto', e.target.value)} className={inp} placeholder="0" />
              </div>
            </div>
          </div>

          {/* Vigencia */}
          <div>
            <p className={sec}>Vigencia</p>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Fecha de reserva</label>
                <input type="date" value={form.fechaReserva} onChange={e => set('fechaReserva', e.target.value)} className={inp} />
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Fecha de vencimiento</label>
                <input type="date" value={form.fechaVencimiento} onChange={e => set('fechaVencimiento', e.target.value)} className={inp} />
              </div>
            </div>
          </div>

          {/* Observaciones */}
          <div>
            <label className="block text-xs font-medium text-gray-600 mb-1">Observaciones</label>
            <textarea rows={3} value={form.observaciones} onChange={e => set('observaciones', e.target.value)} className={`${inp} resize-none`} placeholder="Condiciones especiales, aclaraciones..." />
          </div>

          {error && <p className="text-sm text-red-600 bg-red-50 px-3 py-2 rounded-xl">{error}</p>}

          <div className="flex gap-3 pt-1">
            <button type="button" onClick={onCerrar} className="flex-1 border border-gray-200 text-gray-600 py-2.5 rounded-xl text-sm font-medium hover:bg-gray-50 transition-colors cursor-pointer">
              Cancelar
            </button>
            <button type="submit" disabled={guardando} className="flex-1 flex items-center justify-center gap-2 bg-blue-900 text-white py-2.5 rounded-xl text-sm font-semibold hover:bg-blue-800 disabled:opacity-60 transition-colors cursor-pointer">
              <Save className="w-4 h-4" />
              {guardando ? 'Guardando...' : 'Guardar'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

// ── Page ────────────────────────────────────────────────────────────────────

export default function ReservasPage() {
  const [reservas, setReservas] = useState<ReservaDto[]>([])
  const [totalRegistros, setTotalRegistros] = useState(0)
  const [totalPaginas, setTotalPaginas] = useState(1)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const [propiedades, setPropiedades] = useState<PropiedadCombo[]>([])
  const [agentes, setAgentes] = useState<AgenteCombo[]>([])
  const [leads, setLeads] = useState<LeadCombo[]>([])

  const [filtros, setFiltros] = useState<FiltrosReservas>({ buscar: '', estado: '', pagina: 1, tamano: 10 })
  const [buscarInput, setBuscarInput] = useState('')

  const [modalAbierto, setModalAbierto] = useState(false)
  const [reservaEditar, setReservaEditar] = useState<ReservaDto | null>(null)
  const [confirmDelete, setConfirmDelete] = useState<ReservaDto | null>(null)
  const [deletando, setDeletando] = useState(false)
  const [exportando, setExportando] = useState(false)

  const cargar = useCallback(async () => {
    setLoading(true); setError('')
    try {
      const res = await getReservas(filtros)
      if (res.success) {
        setReservas(res.data.items)
        setTotalRegistros(res.data.totalRegistros)
        setTotalPaginas(res.data.totalPaginas)
      }
    } catch {
      setError('No se pudieron cargar las reservas.')
    } finally {
      setLoading(false)
    }
  }, [filtros])

  useEffect(() => { cargar() }, [cargar])

  useEffect(() => {
    client.get<ApiResponse<PropiedadCombo[]>>('/api/propiedades/para-reserva')
      .then(res => { if (res.data.success) setPropiedades(res.data.data) })
      .catch(() => {})
    client.get<ApiResponse<AgenteCombo[]>>('/api/agentes/activos')
      .then(res => { if (res.data.success) setAgentes(res.data.data) })
      .catch(() => {})
    client.get<ApiResponse<LeadCombo[]>>('/api/leads/para-reserva')
      .then(res => { if (res.data.success) setLeads(res.data.data) })
      .catch(() => {})
  }, [])

  const handleBuscar = () => setFiltros(f => ({ ...f, buscar: buscarInput, pagina: 1 }))

  const handleExportarPdf = async () => {
    setExportando(true)
    try {
      await exportarReservasPdf({ buscar: filtros.buscar, estado: filtros.estado })
    } catch {
      setError('No se pudo generar el PDF.')
    } finally {
      setExportando(false)
    }
  }

  const handleEditar = async (r: ReservaDto) => {
    const res = await getReserva(r.id)
    if (res.success) { setReservaEditar(res.data); setModalAbierto(true) }
  }

  const handleGuardado = () => { setModalAbierto(false); setReservaEditar(null); cargar() }

  const handleConfirmarDelete = async () => {
    if (!confirmDelete) return
    setDeletando(true)
    try {
      await deleteReserva(confirmDelete.id)
      setConfirmDelete(null); cargar()
    } catch {
      setError('No se pudo eliminar la reserva.')
    } finally {
      setDeletando(false)
    }
  }

  const selectClass = 'border border-gray-200 rounded-xl px-3 py-2.5 text-sm text-gray-600 bg-white outline-none'

  return (
    <DashboardLayout titulo="Reservas">

      {/* TOOLBAR */}
      <div className="flex flex-col sm:flex-row gap-3 mb-6">
        <div className="flex gap-2 flex-1">
          <div className="flex items-center gap-2 border border-gray-200 rounded-xl px-3 bg-white flex-1 max-w-xs">
            <Search className="w-4 h-4 text-gray-400 shrink-0" />
            <input
              type="text"
              placeholder="Comprador, propiedad, DNI..."
              value={buscarInput}
              onChange={e => setBuscarInput(e.target.value)}
              onKeyDown={e => e.key === 'Enter' && handleBuscar()}
              className="py-2.5 text-sm outline-none w-full text-gray-700 placeholder-gray-400"
            />
          </div>
          <button onClick={handleBuscar} className="bg-blue-900 text-white px-4 py-2.5 rounded-xl text-sm font-medium hover:bg-blue-800 transition-colors cursor-pointer">
            Buscar
          </button>
        </div>

        <div className="flex gap-2">
          <select value={filtros.estado} onChange={e => setFiltros(f => ({ ...f, estado: e.target.value, pagina: 1 }))} className={selectClass}>
            <option value="">Todos los estados</option>
            {Object.entries(ESTADOS_RESERVA).map(([k, v]) => <option key={k} value={k}>{v.label}</option>)}
          </select>
          <button
            onClick={handleExportarPdf}
            disabled={exportando}
            className="flex items-center gap-2 border border-gray-200 text-gray-600 px-4 py-2.5 rounded-xl text-sm font-medium hover:bg-gray-50 disabled:opacity-60 transition-colors cursor-pointer"
          >
            <FileDown className="w-4 h-4" />
            {exportando ? 'Generando...' : 'Exportar PDF'}
          </button>
          <button
            onClick={() => { setReservaEditar(null); setModalAbierto(true) }}
            className="flex items-center gap-2 bg-yellow-400 hover:bg-yellow-500 text-blue-900 font-semibold px-4 py-2.5 rounded-xl text-sm transition-colors cursor-pointer"
          >
            <Plus className="w-4 h-4" />
            Nueva reserva
          </button>
        </div>
      </div>

      {error && <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-xl mb-4 text-sm">{error}</div>}

      {/* TABLA */}
      <div className="bg-white rounded-2xl shadow-sm border border-gray-100 overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-gray-100 bg-gray-50">
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Comprador</th>
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Propiedad</th>
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Seña</th>
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Estado</th>
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Vencimiento</th>
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Agente</th>
                <th className="text-center px-5 py-3.5 font-semibold text-gray-600">Acciones</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                Array.from({ length: 4 }).map((_, i) => (
                  <tr key={i} className="border-b border-gray-50">
                    {Array.from({ length: 7 }).map((_, j) => (
                      <td key={j} className="px-5 py-4"><div className="h-4 bg-gray-100 rounded animate-pulse" /></td>
                    ))}
                  </tr>
                ))
              ) : reservas.length === 0 ? (
                <tr><td colSpan={7} className="text-center py-16 text-gray-400">No se encontraron reservas</td></tr>
              ) : (
                reservas.map(r => {
                  const dias = diasRestantes(r.fechaVencimiento)
                  const estadoNum = estadoReservaNumero(r.estado)
                  const vigente = estadoNum === 1
                  return (
                    <tr key={r.id} className="border-b border-gray-50 hover:bg-gray-50 transition-colors">
                      <td className="px-5 py-4">
                        <div className="font-medium text-gray-800">{r.compradorApellido}, {r.compradorNombre}</div>
                        <div className="text-xs text-gray-400 mt-0.5">{r.compradorTelefono ?? r.compradorEmail ?? ''}</div>
                      </td>
                      <td className="px-5 py-4 text-xs text-gray-600">{r.propiedadDireccion}</td>
                      <td className="px-5 py-4 text-xs font-semibold text-gray-700">
                        {formatMoneda(r.montoSenia, r.moneda)}
                        {r.precioTotal && <div className="text-gray-400 font-normal">Total: {formatMoneda(r.precioTotal, r.moneda)}</div>}
                      </td>
                      <td className="px-5 py-4"><EstadoBadge estado={r.estado} /></td>
                      <td className="px-5 py-4 text-xs">
                        <div className="text-gray-600">{formatFecha(r.fechaVencimiento)}</div>
                        {vigente && (
                          <div className={`mt-0.5 font-medium ${dias < 0 ? 'text-red-500' : dias <= 3 ? 'text-orange-500' : 'text-gray-400'}`}>
                            {dias < 0 ? `Vencida hace ${Math.abs(dias)}d` : dias === 0 ? 'Vence hoy' : `${dias}d restantes`}
                          </div>
                        )}
                      </td>
                      <td className="px-5 py-4 text-xs text-gray-500">{r.agenteNombre ?? <span className="text-gray-300">—</span>}</td>
                      <td className="px-5 py-4">
                        <div className="flex items-center justify-center gap-2">
                          <button onClick={() => handleEditar(r)} className="p-1.5 text-blue-600 hover:bg-blue-50 rounded-lg transition-colors cursor-pointer" title="Editar">
                            <CalendarClock className="w-4 h-4" />
                          </button>
                          <button onClick={() => setConfirmDelete(r)} className="p-1.5 text-red-500 hover:bg-red-50 rounded-lg transition-colors cursor-pointer" title="Eliminar">
                            <Trash2 className="w-4 h-4" />
                          </button>
                        </div>
                      </td>
                    </tr>
                  )
                })
              )}
            </tbody>
          </table>
        </div>

        {!loading && totalRegistros > 0 && (
          <div className="px-5 py-4 border-t border-gray-100 flex items-center justify-between">
            <span className="text-sm text-gray-400">
              {totalRegistros} reserva{totalRegistros !== 1 ? 's' : ''} · Página {filtros.pagina} de {totalPaginas}
            </span>
            <div className="flex gap-1">
              <button onClick={() => setFiltros(f => ({ ...f, pagina: f.pagina - 1 }))} disabled={filtros.pagina <= 1} className="p-2 rounded-lg text-gray-400 hover:bg-gray-100 disabled:opacity-30 disabled:cursor-not-allowed transition-colors cursor-pointer">
                <ChevronLeft className="w-4 h-4" />
              </button>
              <button onClick={() => setFiltros(f => ({ ...f, pagina: f.pagina + 1 }))} disabled={filtros.pagina >= totalPaginas} className="p-2 rounded-lg text-gray-400 hover:bg-gray-100 disabled:opacity-30 disabled:cursor-not-allowed transition-colors cursor-pointer">
                <ChevronRight className="w-4 h-4" />
              </button>
            </div>
          </div>
        )}
      </div>

      {/* MODAL FORM */}
      {modalAbierto && (
        <ReservaForm
          reserva={reservaEditar}
          propiedades={propiedades}
          agentes={agentes}
          leads={leads}
          onGuardado={handleGuardado}
          onCerrar={() => { setModalAbierto(false); setReservaEditar(null) }}
        />
      )}

      {/* CONFIRM DELETE */}
      {confirmDelete && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-2xl p-6 max-w-sm w-full shadow-xl">
            <div className="flex items-center gap-3 mb-4">
              <div className="w-10 h-10 bg-red-100 rounded-xl flex items-center justify-center">
                <AlertTriangle className="w-5 h-5 text-red-600" />
              </div>
              <h3 className="font-semibold text-gray-800">Eliminar reserva</h3>
            </div>
            <p className="text-sm text-gray-600 mb-2">
              ¿Eliminás la reserva de <strong>{confirmDelete.compradorApellido}, {confirmDelete.compradorNombre}</strong>?
            </p>
            <p className="text-xs text-gray-400 mb-6">La propiedad volverá al estado <strong>Disponible</strong>.</p>
            <div className="flex gap-3">
              <button onClick={() => setConfirmDelete(null)} className="flex-1 border border-gray-200 text-gray-600 py-2.5 rounded-xl text-sm font-medium hover:bg-gray-50 transition-colors cursor-pointer">
                Cancelar
              </button>
              <button onClick={handleConfirmarDelete} disabled={deletando} className="flex-1 bg-red-600 text-white py-2.5 rounded-xl text-sm font-medium hover:bg-red-700 disabled:opacity-60 transition-colors cursor-pointer">
                {deletando ? 'Eliminando...' : 'Eliminar'}
              </button>
            </div>
          </div>
        </div>
      )}

    </DashboardLayout>
  )
}
