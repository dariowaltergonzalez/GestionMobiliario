import client from './client'
import type { ApiResponse, PagedResult } from '../types/api'
import { fechaLocal } from '../utils/fecha'

export interface ComisionAgenteDto {
  id: number
  contratoId: number
  contratoCodigo: string
  propiedadDireccion: string
  agenteId: number
  agenteNombre: string
  montoComisionInmobiliaria: number
  porcentajeAgente: number
  montoAgente: number
  estado: string
  fechaPago: string | null
  medio: string | null
  observaciones: string | null
  fechaCreacion: string
}

export interface ComisionAgenteMetricasDto {
  pendientesCount: number
  montoTotalPendiente: number
  pagadasMesCount: number
  montoPagadoMes: number
}

export interface FiltrosComisionesAgentes {
  estado?: string
  agenteId?: number
  buscar?: string
  pagina: number
  tamano: number
}

export const getComisionesAgentes = async (filtros: FiltrosComisionesAgentes) => {
  const params: Record<string, string> = {
    pagina: String(filtros.pagina),
    tamano: String(filtros.tamano),
  }
  if (filtros.estado) params.estado = filtros.estado
  if (filtros.agenteId) params.agenteId = String(filtros.agenteId)
  if (filtros.buscar) params.buscar = filtros.buscar
  const res = await client.get<ApiResponse<PagedResult<ComisionAgenteDto>>>('/api/comisiones-agentes', { params })
  return res.data
}

export const getComisionAgenteMetricas = async () => {
  const res = await client.get<ApiResponse<ComisionAgenteMetricasDto>>('/api/comisiones-agentes/metricas')
  return res.data
}

export interface PagoComisionFormData {
  fecha?: string
  medio: number
  observaciones?: string
}

const toPagoPayload = (datos: PagoComisionFormData) => ({
  fecha: datos.fecha || fechaLocal(),
  medio: datos.medio,
  observaciones: datos.observaciones || undefined,
})

export const registrarPagoComision = async (id: number, datos: PagoComisionFormData) => {
  const res = await client.post<ApiResponse<ComisionAgenteDto>>(`/api/comisiones-agentes/${id}/pago`, toPagoPayload(datos))
  return res.data
}

export const actualizarPagoComision = async (id: number, datos: PagoComisionFormData) => {
  const res = await client.put<ApiResponse<ComisionAgenteDto>>(`/api/comisiones-agentes/${id}/pago`, toPagoPayload(datos))
  return res.data
}

export const deshacerPagoComision = async (id: number) => {
  const res = await client.delete<ApiResponse<ComisionAgenteDto>>(`/api/comisiones-agentes/${id}/pago`)
  return res.data
}
