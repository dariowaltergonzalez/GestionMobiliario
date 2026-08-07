import client from './client'
import type { ApiResponse, PagedResult } from '../types/api'

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
  montoALiquidar: number
  estado: string
  fechaLiquidacion: string | null
  observaciones: string | null
  fechaCreacion: string
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

export const marcarLiquidada = async (id: number, observaciones?: string) => {
  const res = await client.put<ApiResponse<LiquidacionDto>>(`/api/liquidaciones/${id}/liquidar`, {
    fecha: new Date().toISOString(),
    observaciones: observaciones || undefined,
  })
  return res.data
}
