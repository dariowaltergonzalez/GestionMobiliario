import { useState, useEffect, useCallback } from 'react'
import { Plus, Search, Pencil, Trash2, ChevronLeft, ChevronRight, AlertTriangle, ChevronDown, ChevronUp, CheckCircle2, Clock, XCircle, Banknote, FileDown } from 'lucide-react'
import DashboardLayout from '../../../components/layout/DashboardLayout'
import {
  getContratos, getContrato, createContrato, updateContrato, deleteContrato, updatePago,
  TIPOS_CONTRATO, ESTADOS_CONTRATO, ESTADOS_PAGO, MEDIOS_PAGO, REFERENCIA_PLACEHOLDER,
  estadoContratoNumero, estadoPagoNumero,
  type ContratoDto, type PagoDto, type FiltrosContratos,
  type CreateContratoRequest, type UpdateContratoRequest, type MedioPago,
} from '../../../api/contratos'
import { exportarContratoPdf } from '../../../api/reportes'
import client from '../../../api/client'
import type { ApiResponse } from '../../../types/api'

// ─── helpers ────────────────────────────────────────────────────────────────

interface PropiedadCombo {
  id: number; direccion: string
  propietarioId: number
  propietarioNombre: string; propietarioApellido: string
  propietarioDni?: string; propietarioTelefono?: string; propietarioEmail?: string
  propietarioDireccion?: string; propietarioBanco?: string; propietarioCbu?: string; propietarioCuit?: string
}
interface AgenteCombo { id: number; nombreCompleto: string }
interface InquilinoCombo { id: number; nombreCompleto: string }

const toDateInput = (iso: string) => iso.split('T')[0]
const hoy = new Date().toISOString().split('T')[0]
const en24m = new Date(new Date().setMonth(new Date().getMonth() + 24)).toISOString().split('T')[0]

function formatMoneda(monto: number, moneda: string) {
  const fmt = monto.toLocaleString('es-AR')
  return moneda === 'ARS' ? `$${fmt}` : `U$S ${fmt}`
}

function mesAnio(iso: string) {
  const d = new Date(iso)
  return d.toLocaleDateString('es-AR', { month: 'short', year: 'numeric' })
}

// ─── PagoRow ────────────────────────────────────────────────────────────────

function PagoRow({ pago, contratoId, onActualizado }: { pago: PagoDto; contratoId: number; onActualizado: () => void }) {
  const [editando, setEditando] = useState(false)
  const [monto, setMonto] = useState(pago.montoPagado != null ? String(pago.montoPagado) : '')
  const [fecha, setFecha] = useState(pago.fechaPago ? toDateInput(pago.fechaPago) : hoy)
  const [medio, setMedio] = useState<string>('1')
  const [referencia, setReferencia] = useState('')
  const [guardando, setGuardando] = useState(false)
  const estadoNum = estadoPagoNumero(pago.estado)
  const info = ESTADOS_PAGO[estadoNum as keyof typeof ESTADOS_PAGO]
  const medioNum = Number(medio) as MedioPago
  const refPlaceholder = REFERENCIA_PLACEHOLDER[medioNum]

  const handlePagar = async () => {
    setGuardando(true)
    try {
      const montoDetalle = monto ? Number(monto) : pago.montoEsperado
      await updatePago(contratoId, pago.id, {
        estado: 2,
        fechaPago: new Date(fecha).toISOString(),
        observaciones: pago.observaciones ?? undefined,
        detalles: [{
          medio: Number(medio),
          monto: montoDetalle,
          referencia: referencia.trim() || undefined,
        }],
      })
      setEditando(false)
      onActualizado()
    } finally { setGuardando(false) }
  }

  const handleAnular = async () => {
    setGuardando(true)
    try {
      await updatePago(contratoId, pago.id, { estado: 4, detalles: [] })
      onActualizado()
    } finally { setGuardando(false) }
  }

  return (
    <div className="border border-gray-100 rounded-xl p-3">
      <div className="flex items-center justify-between gap-2">
        <div className="flex items-center gap-2 min-w-0">
          <span className="text-xs text-gray-400 w-6 shrink-0 text-right">#{pago.numeroCuota}</span>
          <span className="text-sm font-medium text-gray-700">{mesAnio(pago.periodo)}</span>
          <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${info.color}`}>{info.label}</span>
          {pago.detalles?.length > 0 && (
            <span className="text-xs text-gray-400">{pago.detalles.map(d => d.medio).join(' + ')}</span>
          )}
        </div>
        <div className="flex items-center gap-3 shrink-0">
          <span className="text-sm font-semibold text-gray-800">
            {pago.montoPagado != null
              ? <span className="text-green-700">{pago.montoPagado.toLocaleString('es-AR')}</span>
              : pago.montoEsperado.toLocaleString('es-AR')}
          </span>
          {estadoNum === 1 && !editando && (
            <button onClick={() => setEditando(true)} className="text-xs text-blue-600 hover:underline">Registrar</button>
          )}
          {estadoNum === 1 && (
            <button onClick={handleAnular} disabled={guardando} className="p-1 text-gray-400 hover:text-red-500 transition-colors">
              <XCircle className="w-4 h-4" />
            </button>
          )}
          {estadoNum === 2 && <CheckCircle2 className="w-4 h-4 text-green-500" />}
          {estadoNum === 3 && <Clock className="w-4 h-4 text-red-500" />}
        </div>
      </div>
      {editando && (
        <div className="mt-3 space-y-2">
          <div className="grid grid-cols-2 gap-2">
            <div>
              <label className="block text-xs text-gray-500 mb-1">Monto pagado</label>
              <input type="number" value={monto} onChange={e => setMonto(e.target.value)}
                placeholder={String(pago.montoEsperado)}
                className="border border-gray-200 rounded-lg px-2 py-1.5 text-sm w-full outline-none" />
            </div>
            <div>
              <label className="block text-xs text-gray-500 mb-1">Fecha de pago</label>
              <input type="date" value={fecha} onChange={e => setFecha(e.target.value)}
                className="border border-gray-200 rounded-lg px-2 py-1.5 text-sm w-full outline-none" />
            </div>
          </div>
          <div className="grid grid-cols-2 gap-2">
            <div>
              <label className="block text-xs text-gray-500 mb-1">Medio de pago</label>
              <select value={medio} onChange={e => { setMedio(e.target.value); setReferencia('') }}
                className="border border-gray-200 rounded-lg px-2 py-1.5 text-sm w-full outline-none bg-white">
                {(Object.entries(MEDIOS_PAGO) as [string, string][]).map(([k, v]) => (
                  <option key={k} value={k}>{v}</option>
                ))}
              </select>
            </div>
            {refPlaceholder && (
              <div>
                <label className="block text-xs text-gray-500 mb-1">Referencia</label>
                <input value={referencia} onChange={e => setReferencia(e.target.value)}
                  placeholder={refPlaceholder}
                  className="border border-gray-200 rounded-lg px-2 py-1.5 text-sm w-full outline-none" />
              </div>
            )}
          </div>
          <div className="flex gap-2 justify-end">
            <button onClick={() => setEditando(false)} className="text-xs text-gray-500 hover:text-gray-700">Cancelar</button>
            <button onClick={handlePagar} disabled={guardando}
              className="text-xs bg-green-600 text-white px-3 py-1.5 rounded-lg hover:bg-green-700 disabled:opacity-60">
              {guardando ? 'Guardando...' : 'Confirmar pago'}
            </button>
          </div>
        </div>
      )}
    </div>
  )
}

// ─── CuotasModal ─────────────────────────────────────────────────────────────

function CuotasModal({ contrato, onCerrar, onActualizado }: {
  contrato: ContratoDto
  onCerrar: () => void
  onActualizado: () => void
}) {
  const pagados = contrato.pagos.filter(p => p.estado === 'Pagado').length
  const total = contrato.pagos.length
  const totalPagado = contrato.pagos.filter(p => p.estado === 'Pagado').reduce((s, p) => s + (p.montoPagado ?? 0), 0)
  const totalEsperado = contrato.pagos.reduce((s, p) => s + p.montoEsperado, 0)

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-lg max-h-[90vh] flex flex-col">
        <div className="flex items-center justify-between px-6 pt-5 pb-4 border-b border-gray-100">
          <div>
            <h2 className="font-semibold text-gray-800">Cuotas — <span className="font-mono text-blue-600">{contrato.codigo}</span></h2>
            <p className="text-xs text-gray-400 mt-0.5">{contrato.propiedadDireccion}</p>
          </div>
          <button onClick={onCerrar} className="text-gray-400 hover:text-gray-600 text-xl leading-none">&times;</button>
        </div>

        <div className="px-6 py-3 bg-gray-50 border-b border-gray-100">
          <div className="flex items-center justify-between mb-1.5">
            <span className="text-xs text-gray-500">{pagados} de {total} cuotas pagadas</span>
            <span className="text-xs text-gray-500">{formatMoneda(totalPagado, contrato.moneda)} / {formatMoneda(totalEsperado, contrato.moneda)}</span>
          </div>
          <div className="h-2 bg-gray-200 rounded-full overflow-hidden">
            <div className="h-full bg-green-500 rounded-full transition-all" style={{ width: `${total ? (pagados / total) * 100 : 0}%` }} />
          </div>
        </div>

        <div className="overflow-y-auto flex-1 px-6 py-4 space-y-2">
          {contrato.pagos.length === 0 ? (
            <p className="text-center text-gray-400 py-8 text-sm">Este contrato no tiene administración de cobros activa.</p>
          ) : (
            contrato.pagos.map(p => (
              <PagoRow key={p.id} pago={p} contratoId={contrato.id} onActualizado={onActualizado} />
            ))
          )}
        </div>
      </div>
    </div>
  )
}

// ─── ContratoForm ─────────────────────────────────────────────────────────────

function ContratoForm({ contrato, onGuardado, onCerrar }: {
  contrato: ContratoDto | null
  onGuardado: () => void
  onCerrar: () => void
}) {
  const [propiedades, setPropiedades] = useState<PropiedadCombo[]>([])
  const [agentes, setAgentes] = useState<AgenteCombo[]>([])
  const [inquilinos, setInquilinos] = useState<InquilinoCombo[]>([])

  const [form, setForm] = useState({
    tipo: contrato ? (contrato.tipo === 'Locacion' ? '1' : '2') : '1',
    estado: contrato ? String(estadoContratoNumero(contrato.estado)) : '1',
    propiedadId: contrato ? String(contrato.propiedadId) : '',
    agenteId: contrato?.agenteId ? String(contrato.agenteId) : '',
    propietarioRefId: contrato?.propietarioRefId ? String(contrato.propietarioRefId) : '',
    inquilinoRefId: contrato?.inquilinoRefId ? String(contrato.inquilinoRefId) : '',
    // Locador
    locadorNombre: contrato?.locadorNombre ?? '',
    locadorApellido: contrato?.locadorApellido ?? '',
    locadorDni: contrato?.locadorDni ?? '',
    locadorEmail: contrato?.locadorEmail ?? '',
    locadorTelefono: contrato?.locadorTelefono ?? '',
    locadorDomicilio: contrato?.locadorDomicilio ?? '',
    locadorBanco: contrato?.locadorBanco ?? '',
    locadorCbu: contrato?.locadorCbu ?? '',
    locadorCuit: contrato?.locadorCuit ?? '',
    // Locatario
    locatarioNombre: contrato?.locatarioNombre ?? '',
    locatarioApellido: contrato?.locatarioApellido ?? '',
    locatarioDni: contrato?.locatarioDni ?? '',
    locatarioEmail: contrato?.locatarioEmail ?? '',
    locatarioTelefono: contrato?.locatarioTelefono ?? '',
    // Garante
    garanteNombre: contrato?.garanteNombre ?? '',
    garanteApellido: contrato?.garanteApellido ?? '',
    garanteDni: contrato?.garanteDni ?? '',
    garanteTelefono: contrato?.garanteTelefono ?? '',
    // Económico
    montoBase: contrato ? String(contrato.montoBase) : '',
    moneda: contrato ? (contrato.moneda === 'ARS' ? '1' : '2') : '1',
    tipoAjuste: contrato ? String(['Fijo','IndiceICL','Porcentaje','Otro'].indexOf(contrato.tipoAjuste) + 1) : '1',
    periodicidadAjusteMeses: contrato?.periodicidadAjusteMeses ? String(contrato.periodicidadAjusteMeses) : '',
    diaVencimientoPago: contrato?.diaVencimientoPago ? String(contrato.diaVencimientoPago) : '',
    comisionLocadorPorcentaje: contrato?.comisionLocadorPorcentaje != null ? String(contrato.comisionLocadorPorcentaje) : '',
    comisionLocadorMonto: contrato?.comisionLocadorMonto != null ? String(contrato.comisionLocadorMonto) : '',
    comisionLocatarioPorcentaje: contrato?.comisionLocatarioPorcentaje != null ? String(contrato.comisionLocatarioPorcentaje) : '',
    comisionLocatarioMonto: contrato?.comisionLocatarioMonto != null ? String(contrato.comisionLocatarioMonto) : '',
    administracionCobros: contrato?.administracionCobros ?? false,
    // Vigencia
    fechaInicio: contrato ? toDateInput(contrato.fechaInicio) : hoy,
    fechaFin: contrato?.fechaFin ? toDateInput(contrato.fechaFin) : en24m,
    fechaEscrituracion: contrato?.fechaEscrituracion ? toDateInput(contrato.fechaEscrituracion) : '',
    observaciones: contrato?.observaciones ?? '',
  })

  const [guardando, setGuardando] = useState(false)
  const [error, setError] = useState('')
  const [secGarante, setSecGarante] = useState(!!contrato?.garanteNombre)

  const set = (f: string, v: string | boolean) => setForm(p => ({ ...p, [f]: v }))

  useEffect(() => {
    Promise.all([
      client.get<ApiResponse<PropiedadCombo[]>>('/api/propiedades/para-contrato').then(r => r.data.data ?? []),
      client.get<ApiResponse<AgenteCombo[]>>('/api/agentes/activos').then(r => r.data.data ?? []),
      client.get<ApiResponse<InquilinoCombo[]>>('/api/inquilinos/activos').then(r => r.data.data ?? []),
    ]).then(([props, ags, inqs]) => {
      setPropiedades(props)
      setAgentes(ags)
      setInquilinos(inqs)
    }).catch(() => {})
  }, [])

  const handlePropiedadChange = (propId: string) => {
    const prop = propiedades.find(p => String(p.id) === propId)
    setForm(prev => ({
      ...prev,
      propiedadId: propId,
      propietarioRefId: prop ? String(prop.propietarioId) : prev.propietarioRefId,
      locadorNombre: prop?.propietarioNombre ?? prev.locadorNombre,
      locadorApellido: prop?.propietarioApellido ?? prev.locadorApellido,
      locadorDni: prop?.propietarioDni ?? prev.locadorDni,
      locadorTelefono: prop?.propietarioTelefono ?? prev.locadorTelefono,
      locadorEmail: prop?.propietarioEmail ?? prev.locadorEmail,
      locadorDomicilio: prop?.propietarioDireccion ?? prev.locadorDomicilio,
      locadorBanco: prop?.propietarioBanco ?? prev.locadorBanco,
      locadorCbu: prop?.propietarioCbu ?? prev.locadorCbu,
      locadorCuit: prop?.propietarioCuit ?? prev.locadorCuit,
    }))
  }

  const handleInquilinoChange = (iqId: string) => {
    const inq = inquilinos.find(i => String(i.id) === iqId)
    if (inq) {
      const [apellido, nombre] = inq.nombreCompleto.includes(',')
        ? inq.nombreCompleto.split(',').map(s => s.trim())
        : [inq.nombreCompleto, '']
      setForm(prev => ({ ...prev, inquilinoRefId: iqId, locatarioApellido: apellido, locatarioNombre: nombre }))
    } else {
      set('inquilinoRefId', iqId)
    }
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!form.propiedadId) { setError('Seleccioná una propiedad.'); return }
    if (!form.locadorNombre || !form.locadorApellido) { setError('Datos del locador requeridos.'); return }
    if (!form.locatarioNombre || !form.locatarioApellido) { setError('Datos del locatario requeridos.'); return }
    if (!form.montoBase || Number(form.montoBase) <= 0) { setError('El monto base debe ser mayor a cero.'); return }
    if (form.tipo === '1' && form.fechaFin && form.fechaFin <= form.fechaInicio) {
      setError('La fecha de fin debe ser posterior a la de inicio.'); return
    }

    setGuardando(true); setError('')
    try {
      const payload: CreateContratoRequest = {
        tipo: Number(form.tipo),
        estado: Number(form.estado),
        propiedadId: Number(form.propiedadId),
        agenteId: form.agenteId ? Number(form.agenteId) : null,
        propietarioRefId: form.propietarioRefId ? Number(form.propietarioRefId) : null,
        inquilinoRefId: form.inquilinoRefId ? Number(form.inquilinoRefId) : null,
        locadorNombre: form.locadorNombre.trim(),
        locadorApellido: form.locadorApellido.trim(),
        locadorDni: form.locadorDni.trim() || undefined,
        locadorEmail: form.locadorEmail.trim() || undefined,
        locadorTelefono: form.locadorTelefono.trim() || undefined,
        locadorDomicilio: form.locadorDomicilio.trim() || undefined,
        locadorBanco: form.locadorBanco.trim() || undefined,
        locadorCbu: form.locadorCbu.trim() || undefined,
        locadorCuit: form.locadorCuit.trim() || undefined,
        locatarioNombre: form.locatarioNombre.trim(),
        locatarioApellido: form.locatarioApellido.trim(),
        locatarioDni: form.locatarioDni.trim() || undefined,
        locatarioEmail: form.locatarioEmail.trim() || undefined,
        locatarioTelefono: form.locatarioTelefono.trim() || undefined,
        garanteNombre: form.garanteNombre.trim() || undefined,
        garanteApellido: form.garanteApellido.trim() || undefined,
        garanteDni: form.garanteDni.trim() || undefined,
        garanteTelefono: form.garanteTelefono.trim() || undefined,
        montoBase: Number(form.montoBase),
        moneda: Number(form.moneda),
        tipoAjuste: Number(form.tipoAjuste),
        periodicidadAjusteMeses: form.periodicidadAjusteMeses ? Number(form.periodicidadAjusteMeses) : undefined,
        diaVencimientoPago: form.diaVencimientoPago ? Number(form.diaVencimientoPago) : undefined,
        comisionLocadorPorcentaje: form.comisionLocadorPorcentaje ? Number(form.comisionLocadorPorcentaje) : undefined,
        comisionLocadorMonto: form.comisionLocadorMonto ? Number(form.comisionLocadorMonto) : undefined,
        comisionLocatarioPorcentaje: form.comisionLocatarioPorcentaje ? Number(form.comisionLocatarioPorcentaje) : undefined,
        comisionLocatarioMonto: form.comisionLocatarioMonto ? Number(form.comisionLocatarioMonto) : undefined,
        administracionCobros: form.administracionCobros,
        fechaInicio: new Date(form.fechaInicio).toISOString(),
        fechaFin: form.tipo === '1' && form.fechaFin ? new Date(form.fechaFin).toISOString() : undefined,
        fechaEscrituracion: form.tipo === '2' && form.fechaEscrituracion ? new Date(form.fechaEscrituracion).toISOString() : undefined,
        observaciones: form.observaciones.trim() || undefined,
      }
      if (contrato) await updateContrato(contrato.id, payload as UpdateContratoRequest)
      else await createContrato(payload)
      onGuardado()
    } catch {
      setError('No se pudo guardar el contrato.')
    } finally { setGuardando(false) }
  }

  const inp = 'border border-gray-200 rounded-xl px-3 py-2 text-sm outline-none w-full text-gray-700'
  const sec = 'text-xs font-semibold text-gray-400 uppercase tracking-wide mb-2 mt-1'

  return (
    <div className="fixed inset-0 bg-black/50 flex items-start justify-center z-50 p-4 overflow-y-auto">
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-2xl my-6">
        <div className="flex items-center justify-between px-6 pt-5 pb-4 border-b border-gray-100">
          <h2 className="font-semibold text-gray-800">{contrato ? `Editar — ${contrato.codigo}` : 'Nuevo contrato'}</h2>
          <button onClick={onCerrar} className="text-gray-400 hover:text-gray-600 text-xl leading-none">&times;</button>
        </div>

        <form onSubmit={handleSubmit} className="px-6 py-4 space-y-4">

          {/* Tipo y Estado */}
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Tipo *</label>
              <select value={form.tipo} onChange={e => set('tipo', e.target.value)} className={inp}>
                <option value="1">Locación</option>
                <option value="2">Boleto de Compraventa</option>
              </select>
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Estado</label>
              <select value={form.estado} onChange={e => set('estado', e.target.value)} className={inp}>
                <option value="1">Borrador</option>
                <option value="2">Vigente</option>
                <option value="3">Finalizado</option>
                <option value="4">Rescindido</option>
              </select>
            </div>
          </div>

          {/* Propiedad y Agente */}
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Propiedad *</label>
              <select value={form.propiedadId} onChange={e => handlePropiedadChange(e.target.value)} className={inp}>
                <option value="">Seleccionar...</option>
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
          </div>

          {/* Locador */}
          <div>
            <p className={sec}>Locador / Vendedor</p>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Nombre *</label>
                <input value={form.locadorNombre} onChange={e => set('locadorNombre', e.target.value)} className={inp} />
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Apellido *</label>
                <input value={form.locadorApellido} onChange={e => set('locadorApellido', e.target.value)} className={inp} />
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">DNI</label>
                <input value={form.locadorDni} onChange={e => set('locadorDni', e.target.value)} className={inp} />
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Teléfono</label>
                <input value={form.locadorTelefono} onChange={e => set('locadorTelefono', e.target.value)} className={inp} />
              </div>
              <div className="col-span-2">
                <label className="block text-xs font-medium text-gray-600 mb-1">Email</label>
                <input type="email" value={form.locadorEmail} onChange={e => set('locadorEmail', e.target.value)} className={inp} />
              </div>
              <div className="col-span-2">
                <label className="block text-xs font-medium text-gray-600 mb-1">Domicilio</label>
                <input value={form.locadorDomicilio} onChange={e => set('locadorDomicilio', e.target.value)} className={inp} placeholder="Calle 123, Ciudad" />
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Banco</label>
                <input value={form.locadorBanco} onChange={e => set('locadorBanco', e.target.value)} className={inp} placeholder="Banco Nación" />
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">CUIT</label>
                <input value={form.locadorCuit} onChange={e => set('locadorCuit', e.target.value)} className={inp} placeholder="20-12345678-3" />
              </div>
              <div className="col-span-2">
                <label className="block text-xs font-medium text-gray-600 mb-1">CBU</label>
                <input value={form.locadorCbu} onChange={e => set('locadorCbu', e.target.value)} className={inp} placeholder="0000000000000000000000" />
              </div>
            </div>
          </div>

          {/* Locatario */}
          <div>
            <p className={sec}>Locatario / Comprador</p>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Inquilino en sistema</label>
                <select value={form.inquilinoRefId} onChange={e => handleInquilinoChange(e.target.value)} className={inp}>
                  <option value="">No vinculado</option>
                  {inquilinos.map(i => <option key={i.id} value={i.id}>{i.nombreCompleto}</option>)}
                </select>
              </div>
              <div />
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Nombre *</label>
                <input value={form.locatarioNombre} onChange={e => set('locatarioNombre', e.target.value)} className={inp} />
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Apellido *</label>
                <input value={form.locatarioApellido} onChange={e => set('locatarioApellido', e.target.value)} className={inp} />
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">DNI</label>
                <input value={form.locatarioDni} onChange={e => set('locatarioDni', e.target.value)} className={inp} />
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Teléfono</label>
                <input value={form.locatarioTelefono} onChange={e => set('locatarioTelefono', e.target.value)} className={inp} />
              </div>
              <div className="col-span-2">
                <label className="block text-xs font-medium text-gray-600 mb-1">Email</label>
                <input type="email" value={form.locatarioEmail} onChange={e => set('locatarioEmail', e.target.value)} className={inp} />
              </div>
            </div>
          </div>

          {/* Garante (colapsable) */}
          <div>
            <button type="button" onClick={() => setSecGarante(v => !v)}
              className="flex items-center gap-1 text-xs font-semibold text-gray-400 uppercase tracking-wide">
              Garante (opcional)
              {secGarante ? <ChevronUp className="w-3 h-3" /> : <ChevronDown className="w-3 h-3" />}
            </button>
            {secGarante && (
              <div className="grid grid-cols-2 gap-3 mt-2">
                <div><label className="block text-xs font-medium text-gray-600 mb-1">Nombre</label>
                  <input value={form.garanteNombre} onChange={e => set('garanteNombre', e.target.value)} className={inp} /></div>
                <div><label className="block text-xs font-medium text-gray-600 mb-1">Apellido</label>
                  <input value={form.garanteApellido} onChange={e => set('garanteApellido', e.target.value)} className={inp} /></div>
                <div><label className="block text-xs font-medium text-gray-600 mb-1">DNI</label>
                  <input value={form.garanteDni} onChange={e => set('garanteDni', e.target.value)} className={inp} /></div>
                <div><label className="block text-xs font-medium text-gray-600 mb-1">Teléfono</label>
                  <input value={form.garanteTelefono} onChange={e => set('garanteTelefono', e.target.value)} className={inp} /></div>
              </div>
            )}
          </div>

          {/* Condiciones económicas */}
          <div>
            <p className={sec}>Condiciones económicas</p>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Moneda</label>
                <select value={form.moneda} onChange={e => set('moneda', e.target.value)} className={inp}>
                  <option value="1">ARS (pesos)</option>
                  <option value="2">USD (dólares)</option>
                </select>
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Monto base *</label>
                <input type="number" min={0} value={form.montoBase} onChange={e => set('montoBase', e.target.value)} className={inp} placeholder="0" />
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Tipo de ajuste</label>
                <select value={form.tipoAjuste} onChange={e => set('tipoAjuste', e.target.value)} className={inp}>
                  <option value="1">Fijo</option>
                  <option value="2">Índice ICL</option>
                  <option value="3">Porcentaje</option>
                  <option value="4">Otro</option>
                </select>
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Periodicidad ajuste (meses)</label>
                <input type="number" min={1} value={form.periodicidadAjusteMeses} onChange={e => set('periodicidadAjusteMeses', e.target.value)} className={inp} placeholder="Ej: 6" />
              </div>
              {form.tipo === '1' && (
                <div>
                  <label className="block text-xs font-medium text-gray-600 mb-1">Día vencimiento pago</label>
                  <input type="number" min={1} max={31} value={form.diaVencimientoPago} onChange={e => set('diaVencimientoPago', e.target.value)} className={inp} placeholder="Ej: 5" />
                </div>
              )}
            </div>
          </div>

          {/* Comisiones */}
          <div>
            <p className={sec}>Comisiones de la inmobiliaria</p>
            <div className="grid grid-cols-2 gap-3">
              <div><label className="block text-xs font-medium text-gray-600 mb-1">Comisión locador %</label>
                <input type="number" min={0} step={0.01} value={form.comisionLocadorPorcentaje} onChange={e => set('comisionLocadorPorcentaje', e.target.value)} className={inp} placeholder="0.00" /></div>
              <div><label className="block text-xs font-medium text-gray-600 mb-1">Comisión locador monto</label>
                <input type="number" min={0} value={form.comisionLocadorMonto} onChange={e => set('comisionLocadorMonto', e.target.value)} className={inp} placeholder="0" /></div>
              <div><label className="block text-xs font-medium text-gray-600 mb-1">Comisión locatario %</label>
                <input type="number" min={0} step={0.01} value={form.comisionLocatarioPorcentaje} onChange={e => set('comisionLocatarioPorcentaje', e.target.value)} className={inp} placeholder="0.00" /></div>
              <div><label className="block text-xs font-medium text-gray-600 mb-1">Comisión locatario monto</label>
                <input type="number" min={0} value={form.comisionLocatarioMonto} onChange={e => set('comisionLocatarioMonto', e.target.value)} className={inp} placeholder="0" /></div>
            </div>
          </div>

          {/* Vigencia */}
          <div>
            <p className={sec}>Vigencia</p>
            <div className="grid grid-cols-2 gap-3">
              <div><label className="block text-xs font-medium text-gray-600 mb-1">Fecha inicio *</label>
                <input type="date" value={form.fechaInicio} onChange={e => set('fechaInicio', e.target.value)} className={inp} /></div>
              {form.tipo === '1' && (
                <div><label className="block text-xs font-medium text-gray-600 mb-1">Fecha fin</label>
                  <input type="date" value={form.fechaFin} onChange={e => set('fechaFin', e.target.value)} className={inp} /></div>
              )}
              {form.tipo === '2' && (
                <div><label className="block text-xs font-medium text-gray-600 mb-1">Fecha escrituración</label>
                  <input type="date" value={form.fechaEscrituracion} onChange={e => set('fechaEscrituracion', e.target.value)} className={inp} /></div>
              )}
            </div>
          </div>

          {/* Administración de cobros */}
          <label className="flex items-center gap-3 cursor-pointer">
            <input type="checkbox" checked={form.administracionCobros}
              onChange={e => set('administracionCobros', e.target.checked)}
              className="w-4 h-4 rounded" />
            <span className="text-sm text-gray-700">
              La inmobiliaria administra los cobros
              <span className="text-xs text-gray-400 ml-1">(genera cuotas automáticamente al activar)</span>
            </span>
          </label>

          {/* Observaciones */}
          <div>
            <label className="block text-xs font-medium text-gray-600 mb-1">Observaciones</label>
            <textarea rows={3} value={form.observaciones} onChange={e => set('observaciones', e.target.value)}
              className={`${inp} resize-none`} placeholder="Condiciones especiales, aclaraciones..." />
          </div>

          {error && <p className="text-sm text-red-600 bg-red-50 px-3 py-2 rounded-xl">{error}</p>}

          <div className="flex gap-3 pt-1">
            <button type="button" onClick={onCerrar}
              className="flex-1 border border-gray-200 text-gray-600 py-2.5 rounded-xl text-sm font-medium hover:bg-gray-50 transition-colors">
              Cancelar
            </button>
            <button type="submit" disabled={guardando}
              className="flex-1 bg-blue-900 text-white py-2.5 rounded-xl text-sm font-semibold hover:bg-blue-800 disabled:opacity-60 transition-colors">
              {guardando ? 'Guardando...' : contrato ? 'Guardar cambios' : 'Crear contrato'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

// ─── ContratosPage ────────────────────────────────────────────────────────────

export default function ContratosPage() {
  const [contratos, setContratos] = useState<ContratoDto[]>([])
  const [totalRegistros, setTotalRegistros] = useState(0)
  const [totalPaginas, setTotalPaginas] = useState(1)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const [filtros, setFiltros] = useState<FiltrosContratos>({ pagina: 1, tamano: 10 })
  const [buscarInput, setBuscarInput] = useState('')

  const [modalAbierto, setModalAbierto] = useState(false)
  const [contratoEditar, setContratoEditar] = useState<ContratoDto | null>(null)
  const [cuotasContrato, setCuotasContrato] = useState<ContratoDto | null>(null)
  const [confirmDelete, setConfirmDelete] = useState<ContratoDto | null>(null)
  const [deletando, setDeletando] = useState(false)

  const cargar = useCallback(async () => {
    setLoading(true); setError('')
    try {
      const res = await getContratos(filtros)
      if (res.success) {
        setContratos(res.data.items)
        setTotalRegistros(res.data.totalRegistros)
        setTotalPaginas(res.data.totalPaginas)
      }
    } catch { setError('No se pudieron cargar los contratos.') }
    finally { setLoading(false) }
  }, [filtros])

  useEffect(() => { cargar() }, [cargar])

  const recargarCuotas = useCallback(async () => {
    if (!cuotasContrato) return
    const res = await getContrato(cuotasContrato.id)
    if (res.success) setCuotasContrato(res.data)
    cargar()
  }, [cuotasContrato, cargar])

  const handleBuscar = () => setFiltros(f => ({ ...f, buscar: buscarInput, pagina: 1 }))
  const handleFiltro = (campo: keyof FiltrosContratos, valor: string) =>
    setFiltros(f => ({ ...f, [campo]: valor, pagina: 1 }))

  const handleNuevo = () => { setContratoEditar(null); setModalAbierto(true) }
  const handleEditar = (c: ContratoDto) => { setContratoEditar(c); setModalAbierto(true) }
  const handleGuardado = () => { setModalAbierto(false); cargar() }

  const handleVerCuotas = async (c: ContratoDto) => {
    const res = await getContrato(c.id)
    if (res.success) setCuotasContrato(res.data)
  }

  const handleDescargarPdf = async (c: ContratoDto) => {
    try { await exportarContratoPdf(c.id, c.codigo) }
    catch { setError('No se pudo generar el PDF del contrato.') }
  }

  const handleConfirmarDelete = async () => {
    if (!confirmDelete) return
    setDeletando(true)
    try {
      await deleteContrato(confirmDelete.id)
      setConfirmDelete(null)
      cargar()
    } catch { setError('No se pudo eliminar el contrato.') }
    finally { setDeletando(false) }
  }

  const selectClass = 'border border-gray-200 rounded-xl px-3 py-2.5 text-sm text-gray-600 bg-white outline-none'

  return (
    <DashboardLayout titulo="Contratos">
      <div className="flex flex-col sm:flex-row gap-3 mb-6">
        <div className="flex gap-2 flex-1">
          <div className="flex items-center gap-2 border border-gray-200 rounded-xl px-3 bg-white flex-1 max-w-xs">
            <Search className="w-4 h-4 text-gray-400 shrink-0" />
            <input type="text" placeholder="Código, partes, dirección..."
              value={buscarInput} onChange={e => setBuscarInput(e.target.value)}
              onKeyDown={e => e.key === 'Enter' && handleBuscar()}
              className="py-2.5 text-sm outline-none w-full text-gray-700 placeholder-gray-400" />
          </div>
          <button onClick={handleBuscar}
            className="bg-blue-900 text-white px-4 py-2.5 rounded-xl text-sm font-medium hover:bg-blue-800 transition-colors">
            Buscar
          </button>
        </div>
        <div className="flex gap-2 flex-wrap">
          <select value={filtros.tipo ?? ''} onChange={e => handleFiltro('tipo', e.target.value)} className={selectClass}>
            <option value="">Todos los tipos</option>
            <option value="1">Locación</option>
            <option value="2">Boleto de Compraventa</option>
          </select>
          <select value={filtros.estado ?? ''} onChange={e => handleFiltro('estado', e.target.value)} className={selectClass}>
            <option value="">Todos los estados</option>
            <option value="1">Borrador</option>
            <option value="2">Vigente</option>
            <option value="3">Finalizado</option>
            <option value="4">Rescindido</option>
          </select>
          <button onClick={handleNuevo}
            className="flex items-center gap-2 bg-yellow-400 hover:bg-yellow-500 text-blue-900 font-semibold px-4 py-2.5 rounded-xl text-sm transition-colors">
            <Plus className="w-4 h-4" /> Nuevo
          </button>
        </div>
      </div>

      {error && <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-xl mb-4 text-sm">{error}</div>}

      <div>
        {/* Tabla */}
        <div className="bg-white rounded-2xl shadow-sm border border-gray-100 overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-gray-100 bg-gray-50">
                  <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Código</th>
                  <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Propiedad</th>
                  <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Tipo · Estado</th>
                  <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Locatario</th>
                  <th className="text-right px-5 py-3.5 font-semibold text-gray-600">Monto base</th>
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
                ) : contratos.length === 0 ? (
                  <tr><td colSpan={6} className="text-center py-16 text-gray-400">No se encontraron contratos</td></tr>
                ) : contratos.map(c => {
                  const estadoNum = estadoContratoNumero(c.estado)
                  const estadoInfo = ESTADOS_CONTRATO[estadoNum as keyof typeof ESTADOS_CONTRATO]
                  return (
                    <tr key={c.id} className="border-b border-gray-50 hover:bg-gray-50 transition-colors">
                      <td className="px-5 py-4">
                        <span className="font-mono text-sm text-blue-600">{c.codigo}</span>
                      </td>
                      <td className="px-5 py-4">
                        <div className="text-gray-700">{c.propiedadDireccion}</div>
                        {c.propiedadCodigo && <div className="text-xs text-blue-500 font-mono">{c.propiedadCodigo}</div>}
                      </td>
                      <td className="px-5 py-4">
                        <div className="text-gray-600 text-xs">{TIPOS_CONTRATO[c.tipo === 'Locacion' ? 1 : 2]}</div>
                        <span className={`mt-0.5 inline-block text-xs px-2 py-0.5 rounded-full font-medium ${estadoInfo.color}`}>
                          {estadoInfo.label}
                        </span>
                      </td>
                      <td className="px-5 py-4 text-gray-600">{c.locatarioApellido}, {c.locatarioNombre}</td>
                      <td className="px-5 py-4 text-right font-semibold text-gray-800">
                        {formatMoneda(c.montoBase, c.moneda)}
                      </td>
                      <td className="px-5 py-4">
                        <div className="flex items-center justify-center gap-2">
                          <button onClick={() => handleDescargarPdf(c)} className="p-1.5 text-gray-500 hover:bg-gray-100 rounded-lg transition-colors cursor-pointer" title="Descargar contrato PDF">
                            <FileDown className="w-4 h-4" />
                          </button>
                          <button onClick={() => handleEditar(c)} className="p-1.5 text-blue-600 hover:bg-blue-50 rounded-lg transition-colors cursor-pointer" title="Editar">
                            <Pencil className="w-4 h-4" />
                          </button>
                          <button onClick={() => setConfirmDelete(c)} className="p-1.5 text-red-500 hover:bg-red-50 rounded-lg transition-colors cursor-pointer" title="Eliminar">
                            <Trash2 className="w-4 h-4" />
                          </button>
                          {c.administracionCobros && (
                            <button onClick={() => handleVerCuotas(c)} className="p-1.5 text-green-600 hover:bg-green-50 rounded-lg transition-colors cursor-pointer" title="Ver cuotas">
                              <Banknote className="w-4 h-4" />
                            </button>
                          )}
                        </div>
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>

          {!loading && totalRegistros > 0 && (
            <div className="px-5 py-4 border-t border-gray-100 flex items-center justify-between">
              <span className="text-sm text-gray-400">
                {totalRegistros} contrato{totalRegistros !== 1 ? 's' : ''} · Página {filtros.pagina} de {totalPaginas}
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

      </div>

      {modalAbierto && (
        <ContratoForm contrato={contratoEditar} onGuardado={handleGuardado} onCerrar={() => setModalAbierto(false)} />
      )}

      {cuotasContrato && (
        <CuotasModal contrato={cuotasContrato} onCerrar={() => setCuotasContrato(null)} onActualizado={recargarCuotas} />
      )}

      {confirmDelete && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-2xl p-6 max-w-sm w-full shadow-xl">
            <div className="flex items-center gap-3 mb-4">
              <div className="w-10 h-10 bg-red-100 rounded-xl flex items-center justify-center">
                <AlertTriangle className="w-5 h-5 text-red-600" />
              </div>
              <h3 className="font-semibold text-gray-800">Eliminar contrato</h3>
            </div>
            <p className="text-sm text-gray-600 mb-6">
              ¿Confirmás eliminar el contrato <strong>{confirmDelete.codigo}</strong>?
              Si estaba vigente, la propiedad volverá a <strong>Disponible</strong>.
            </p>
            <div className="flex gap-3">
              <button onClick={() => setConfirmDelete(null)}
                className="flex-1 border border-gray-200 text-gray-600 py-2.5 rounded-xl text-sm font-medium hover:bg-gray-50 transition-colors">
                Cancelar
              </button>
              <button onClick={handleConfirmarDelete} disabled={deletando}
                className="flex-1 bg-red-600 text-white py-2.5 rounded-xl text-sm font-medium hover:bg-red-700 disabled:opacity-60 transition-colors">
                {deletando ? 'Procesando...' : 'Eliminar'}
              </button>
            </div>
          </div>
        </div>
      )}
    </DashboardLayout>
  )
}
