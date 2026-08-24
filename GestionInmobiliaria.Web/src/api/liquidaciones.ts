import client from './client'
import type { ApiResponse, PagedResult } from '../types/api'

export interface LiquidacionAbonoDto {
  id: number
  monto: number
  fecha: string
  medio: string
  cbuCvuDestino: string | null
  entidadDestino: string | null
  numeroOperacion: string | null
  observaciones: string | null
  comprobanteUrl: string | null
}

export interface LiquidacionGastoDto {
  id: number
  categoria: string
  descripcion: string | null
  monto: number
}

export interface LiquidacionDto {
  id: number
  pagoId: number
  contratoId: number
  contratoCodigo: string
  propiedadDireccion: string
  propietarioRefId: number | null
  propietarioNombre: string
  propietarioApellido: string
  numeroCuota: number
  periodo: string
  moneda: string
  montoCobrado: number
  comisionPorcentaje: number | null
  comisionMonto: number | null
  montoComision: number
  montoGastos: number
  montoALiquidar: number
  montoAbonado: number
  montoRestante: number
  estado: string
  fechaLiquidacion: string | null
  observaciones: string | null
  fechaCreacion: string
  abonos: LiquidacionAbonoDto[]
  gastos: LiquidacionGastoDto[]
}

export interface LiquidacionMetricasDto {
  pendientesCount: number
  montoPendienteTotal: number
  liquidadasMesCount: number
  montoLiquidadoMes: number
}

export interface FiltrosLiquidaciones {
  estado?: string
  propietarioId?: number
  buscar?: string
  pagina: number
  tamano: number
}

export const getLiquidaciones = async (filtros: FiltrosLiquidaciones) => {
  const params: Record<string, string> = {
    pagina: String(filtros.pagina),
    tamano: String(filtros.tamano),
  }
  if (filtros.estado) params.estado = filtros.estado
  if (filtros.propietarioId) params.propietarioId = String(filtros.propietarioId)
  if (filtros.buscar) params.buscar = filtros.buscar
  const res = await client.get<ApiResponse<PagedResult<LiquidacionDto>>>('/api/liquidaciones', { params })
  return res.data
}

export const getLiquidacionMetricas = async () => {
  const res = await client.get<ApiResponse<LiquidacionMetricasDto>>('/api/liquidaciones/metricas')
  return res.data
}

export const eliminarLiquidacion = async (id: number) => {
  const res = await client.delete<ApiResponse<string>>(`/api/liquidaciones/${id}`)
  return res.data
}

export interface AbonoFormData {
  monto: number
  fecha?: string
  medio: number
  cbuCvuDestino?: string
  entidadDestino?: string
  numeroOperacion?: string
  observaciones?: string
  comprobanteUrl?: string
}

const toAbonoPayload = (datos: AbonoFormData) => ({
  monto: datos.monto,
  fecha: datos.fecha || new Date().toISOString(),
  medio: datos.medio,
  cbuCvuDestino: datos.cbuCvuDestino || undefined,
  entidadDestino: datos.entidadDestino || undefined,
  numeroOperacion: datos.numeroOperacion || undefined,
  observaciones: datos.observaciones || undefined,
  comprobanteUrl: datos.comprobanteUrl || undefined,
})

export interface ExtraccionComprobanteDto {
  comprobanteUrl: string
  monto: number | null
  fecha: string | null
  cbuCvuDestino: string | null
  entidadDestino: string | null
  numeroOperacion: string | null
}

export const extraerComprobante = async (archivo: File) => {
  const formData = new FormData()
  formData.append('archivo', archivo)
  const res = await client.post<ApiResponse<ExtraccionComprobanteDto>>(
    '/api/liquidaciones/comprobantes/extraer', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
  return res.data
}

export const agregarAbono = async (liquidacionId: number, datos: AbonoFormData) => {
  const res = await client.post<ApiResponse<LiquidacionDto>>(
    `/api/liquidaciones/${liquidacionId}/abonos`, toAbonoPayload(datos))
  return res.data
}

export const editarAbono = async (liquidacionId: number, abonoId: number, datos: AbonoFormData) => {
  const res = await client.put<ApiResponse<LiquidacionDto>>(
    `/api/liquidaciones/${liquidacionId}/abonos/${abonoId}`, toAbonoPayload(datos))
  return res.data
}

export const eliminarAbono = async (liquidacionId: number, abonoId: number) => {
  const res = await client.delete<ApiResponse<LiquidacionDto>>(
    `/api/liquidaciones/${liquidacionId}/abonos/${abonoId}`)
  return res.data
}
