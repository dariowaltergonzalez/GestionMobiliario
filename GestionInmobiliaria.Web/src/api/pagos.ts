import client from './client'
import type { ApiResponse, PagedResult } from '../types/api'
import type { UpdatePagoRequest, PagoDetalleDto } from './contratos'

export interface PagoListDto {
  id: number
  contratoId: number
  contratoCodigo: string
  propiedadDireccion: string
  locatarioNombre: string
  locatarioApellido: string
  locadorNombre: string
  locadorApellido: string
  locadorEmail: string | null
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
  montoPunitorio: number
  diasAtraso: number
  tasaPunitorioUsada: string | null
  montoPunitorioCobrado: number | null
  diasAtrasoPunitorioCobrado: number | null
  fechaVencimientoPunitorioCobrado: string | null
  detallePunitorioCobrado: string | null
}

export type { UpdatePagoRequest }

export interface PagoMetricasDto {
  pendientesCount: number
  atrasadosCount: number
  pagadosMesCount: number
  montoCobradoMes: number
  montoTotalPendiente: number
}

export interface FiltrosPagos {
  contratoId?: number
  estado?: string
  mes?: number
  anio?: number
  buscar?: string
  pagina: number
  tamano: number
}

export const getPagosConsolidados = async (filtros: FiltrosPagos) => {
  const params: Record<string, string> = {
    pagina: String(filtros.pagina),
    tamano: String(filtros.tamano),
  }
  if (filtros.contratoId) params.contratoId = String(filtros.contratoId)
  if (filtros.estado) params.estado = filtros.estado
  if (filtros.mes) params.mes = String(filtros.mes)
  if (filtros.anio) params.anio = String(filtros.anio)
  if (filtros.buscar) params.buscar = filtros.buscar
  const res = await client.get<ApiResponse<PagedResult<PagoListDto>>>('/api/pagos', { params })
  return res.data
}

export const getPagoMetricas = async () => {
  const res = await client.get<ApiResponse<PagoMetricasDto>>('/api/pagos/metricas')
  return res.data
}

export const updatePagoConsolidado = async (contratoId: number, pagoId: number, data: UpdatePagoRequest) => {
  const res = await client.put<ApiResponse<unknown>>(`/api/pagos/${contratoId}/pagos/${pagoId}`, data)
  return res.data
}

export const descargarReciboPago = async (contratoId: number, pagoId: number, contratoCodigo: string, periodo: string) => {
  const res = await client.get(`/api/pagos/${contratoId}/pagos/${pagoId}/recibo`, { responseType: 'blob' })
  const url = URL.createObjectURL(res.data)
  const a = document.createElement('a')
  a.href = url
  a.download = `Recibo_${contratoCodigo}_${periodo}.pdf`
  a.click()
  URL.revokeObjectURL(url)
}
