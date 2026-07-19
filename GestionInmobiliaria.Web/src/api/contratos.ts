import client from './client'
import type { ApiResponse, PagedResult } from '../types/api'

export type TipoContrato = 1 | 2
export type EstadoContrato = 1 | 2 | 3 | 4
export type TipoAjuste = 1 | 2 | 3 | 4
export type EstadoPago = 1 | 2 | 3 | 4
export type MedioPago = 1 | 2 | 3 | 4

export const TIPOS_CONTRATO: Record<TipoContrato, string> = {
  1: 'Locación',
  2: 'Boleto de Compraventa',
}

export const ESTADOS_CONTRATO: Record<EstadoContrato, { label: string; color: string }> = {
  1: { label: 'Borrador',    color: 'bg-gray-100 text-gray-600' },
  2: { label: 'Vigente',     color: 'bg-green-100 text-green-700' },
  3: { label: 'Finalizado',  color: 'bg-blue-100 text-blue-700' },
  4: { label: 'Rescindido',  color: 'bg-red-100 text-red-600' },
}

export const TIPOS_AJUSTE: Record<TipoAjuste, string> = {
  1: 'Fijo',
  2: 'Índice ICL',
  3: 'Porcentaje',
  4: 'Otro',
}

export const ESTADOS_PAGO: Record<EstadoPago, { label: string; color: string }> = {
  1: { label: 'Pendiente', color: 'bg-yellow-100 text-yellow-700' },
  2: { label: 'Pagado',    color: 'bg-green-100 text-green-700' },
  3: { label: 'Atrasado',  color: 'bg-red-100 text-red-600' },
  4: { label: 'Anulado',   color: 'bg-gray-100 text-gray-500' },
}

export const MEDIOS_PAGO: Record<MedioPago, string> = {
  1: 'Efectivo',
  2: 'Débito',
  3: 'Crédito',
  4: 'Cheque',
}

export const REFERENCIA_PLACEHOLDER: Record<MedioPago, string> = {
  1: '',
  2: 'Nro de comprobante',
  3: 'Nro de comprobante / últimos 4 dígitos',
  4: 'Banco · Nro de cheque · Fecha cheque',
}

export interface PagoDetalleDto {
  id: number
  medio: string
  monto: number
  referencia: string | null
  chequeBanco: string | null
  chequeNumero: string | null
  chequeFechaVencimiento: string | null
}

export interface PagoDetalleRequest {
  medio: number
  monto: number
  referencia?: string
  chequeBanco?: string
  chequeNumero?: string
  chequeFechaVencimiento?: string
}

export interface PagoDto {
  id: number
  contratoId: number
  numeroCuota: number
  periodo: string
  montoEsperado: number
  montoPagado: number | null
  fechaPago: string | null
  estado: string
  observaciones: string | null
  detalles: PagoDetalleDto[]
  fechaCreacion: string
  fechaActualizacion: string
}

export interface ContratoDto {
  id: number
  codigo: string
  tipo: string
  estado: string
  propiedadId: number
  propiedadDireccion: string
  propiedadCodigo: string | null
  reservaId: number | null
  agenteId: number | null
  agenteNombre: string | null
  propietarioRefId: number | null
  inquilinoRefId: number | null
  locadorNombre: string
  locadorApellido: string
  locadorDni: string | null
  locadorEmail: string | null
  locadorTelefono: string | null
  locadorDomicilio: string | null
  locadorBanco: string | null
  locadorCbu: string | null
  locadorCuit: string | null
  locatarioNombre: string
  locatarioApellido: string
  locatarioDni: string | null
  locatarioEmail: string | null
  locatarioTelefono: string | null
  garanteNombre: string | null
  garanteApellido: string | null
  garanteDni: string | null
  garanteTelefono: string | null
  montoBase: number
  moneda: string
  tipoAjuste: string
  periodicidadAjusteMeses: number | null
  diaVencimientoPago: number | null
  comisionLocadorPorcentaje: number | null
  comisionLocadorMonto: number | null
  comisionLocatarioPorcentaje: number | null
  comisionLocatarioMonto: number | null
  administracionCobros: boolean
  fechaInicio: string
  fechaFin: string | null
  fechaEscrituracion: string | null
  observaciones: string | null
  archivoUrl: string | null
  pagos: PagoDto[]
  fechaCreacion: string
  fechaActualizacion: string
}

export interface CreateContratoRequest {
  tipo: number
  estado: number
  propiedadId: number
  reservaId?: number
  agenteId?: number | null
  propietarioRefId?: number | null
  inquilinoRefId?: number | null
  locadorNombre: string
  locadorApellido: string
  locadorDni?: string
  locadorEmail?: string
  locadorTelefono?: string
  locadorDomicilio?: string
  locadorBanco?: string
  locadorCbu?: string
  locadorCuit?: string
  locatarioNombre: string
  locatarioApellido: string
  locatarioDni?: string
  locatarioEmail?: string
  locatarioTelefono?: string
  garanteNombre?: string
  garanteApellido?: string
  garanteDni?: string
  garanteTelefono?: string
  montoBase: number
  moneda: number
  tipoAjuste: number
  periodicidadAjusteMeses?: number
  diaVencimientoPago?: number
  comisionLocadorPorcentaje?: number
  comisionLocadorMonto?: number
  comisionLocatarioPorcentaje?: number
  comisionLocatarioMonto?: number
  administracionCobros: boolean
  fechaInicio: string
  fechaFin?: string
  fechaEscrituracion?: string
  observaciones?: string
}

export interface UpdateContratoRequest extends CreateContratoRequest {}

export interface UpdatePagoRequest {
  estado: number
  fechaPago?: string
  observaciones?: string
  detalles: PagoDetalleRequest[]
}

export interface FiltrosContratos {
  buscar?: string
  tipo?: string
  estado?: string
  pagina: number
  tamano: number
}

export const estadoContratoNumero = (s: string): number =>
  ({ Borrador: 1, Vigente: 2, Finalizado: 3, Rescindido: 4 } as Record<string, number>)[s] ?? 1

export const estadoPagoNumero = (s: string): number =>
  ({ Pendiente: 1, Pagado: 2, Atrasado: 3, Anulado: 4 } as Record<string, number>)[s] ?? 1

export const getContratos = async (filtros: FiltrosContratos) => {
  const params: Record<string, string> = {
    pagina: String(filtros.pagina),
    tamano: String(filtros.tamano),
  }
  if (filtros.buscar) params.buscar = filtros.buscar
  if (filtros.tipo) params.tipo = filtros.tipo
  if (filtros.estado) params.estado = filtros.estado
  const res = await client.get<ApiResponse<PagedResult<ContratoDto>>>('/api/contratos', { params })
  return res.data
}

export const getContrato = async (id: number) => {
  const res = await client.get<ApiResponse<ContratoDto>>(`/api/contratos/${id}`)
  return res.data
}

export const createContrato = async (data: CreateContratoRequest) => {
  const res = await client.post<ApiResponse<ContratoDto>>('/api/contratos', data)
  return res.data
}

export const updateContrato = async (id: number, data: UpdateContratoRequest) => {
  const res = await client.put<ApiResponse<ContratoDto>>(`/api/contratos/${id}`, data)
  return res.data
}

export const deleteContrato = async (id: number) => {
  const res = await client.delete<ApiResponse<null>>(`/api/contratos/${id}`)
  return res.data
}

export const getPagos = async (contratoId: number) => {
  const res = await client.get<ApiResponse<PagoDto[]>>(`/api/contratos/${contratoId}/pagos`)
  return res.data
}

export const updatePago = async (contratoId: number, pagoId: number, data: UpdatePagoRequest) => {
  const res = await client.put<ApiResponse<PagoDto>>(`/api/contratos/${contratoId}/pagos/${pagoId}`, data)
  return res.data
}
