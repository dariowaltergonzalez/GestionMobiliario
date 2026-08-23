import client from './client'
import type { ApiResponse, PagedResult } from '../types/api'

export const CATEGORIAS_GASTO: Record<number, string> = {
  1: 'Reparación',
  2: 'Impuesto',
  3: 'Expensas',
  4: 'Seguro',
  5: 'Otro',
}

export const RESPONSABLES_GASTO: Record<number, string> = {
  1: 'Propietario',
  2: 'Inquilino',
}

export interface GastoDto {
  id: number
  propiedadId: number
  propiedadDireccion: string
  contratoId: number | null
  contratoCodigo: string | null
  categoria: string
  descripcion: string | null
  monto: number
  fecha: string
  responsable: string
  estado: string
  fechaResolucion: string | null
  medioCobro: string | null
  fechaCobro: string | null
  referenciaCobro: string | null
  chequeBanco: string | null
  chequeNumero: string | null
  chequeFechaVencimiento: string | null
  observacionesResolucion: string | null
  liquidacionId: number | null
  visibleParaInquilino: boolean
  fechaCreacion: string
}

export interface ResolverGastoData {
  medio: number
  fecha?: string
  referenciaCobro?: string
  chequeBanco?: string
  chequeNumero?: string
  chequeFechaVencimiento?: string
  observaciones?: string
}

export interface GastoFormData {
  propiedadId: number | ''
  contratoId: number | ''
  categoria: number
  descripcion: string
  monto: number
  fecha: string
  responsable: number
  visibleParaInquilino: boolean
}

export const gastoFormVacio = (): GastoFormData => ({
  propiedadId: '',
  contratoId: '',
  categoria: 1,
  descripcion: '',
  monto: 0,
  fecha: new Date().toISOString().slice(0, 10),
  responsable: 1,
  visibleParaInquilino: true,
})

const formToRequest = (f: GastoFormData) => ({
  propiedadId: Number(f.propiedadId),
  contratoId: f.contratoId ? Number(f.contratoId) : null,
  categoria: f.categoria,
  descripcion: f.descripcion || null,
  monto: f.monto,
  fecha: f.fecha,
  responsable: f.responsable,
  visibleParaInquilino: f.visibleParaInquilino,
})

export interface FiltrosGastos {
  propiedadId?: number
  contratoId?: number
  responsable?: number
  estado?: number
  categoria?: number
  buscar?: string
  pagina: number
  tamano: number
}

export const getGastos = async (filtros: FiltrosGastos) => {
  const params: Record<string, string> = {
    pagina: String(filtros.pagina),
    tamano: String(filtros.tamano),
  }
  if (filtros.propiedadId) params.propiedadId = String(filtros.propiedadId)
  if (filtros.contratoId) params.contratoId = String(filtros.contratoId)
  if (filtros.responsable) params.responsable = String(filtros.responsable)
  if (filtros.estado) params.estado = String(filtros.estado)
  if (filtros.categoria) params.categoria = String(filtros.categoria)
  if (filtros.buscar) params.buscar = filtros.buscar
  const res = await client.get<ApiResponse<PagedResult<GastoDto>>>('/api/gastos', { params })
  return res.data
}

export const getGasto = async (id: number) => {
  const res = await client.get<ApiResponse<GastoDto>>(`/api/gastos/${id}`)
  return res.data
}

export const createGasto = async (form: GastoFormData) => {
  const res = await client.post<ApiResponse<GastoDto>>('/api/gastos', formToRequest(form))
  return res.data
}

export const updateGasto = async (id: number, form: GastoFormData) => {
  const res = await client.put<ApiResponse<GastoDto>>(`/api/gastos/${id}`, formToRequest(form))
  return res.data
}

export const marcarGastoResuelto = async (id: number, datos: ResolverGastoData) => {
  const res = await client.put<ApiResponse<GastoDto>>(`/api/gastos/${id}/resolver`, datos)
  return res.data
}

export const deleteGasto = async (id: number) => {
  const res = await client.delete<ApiResponse<string>>(`/api/gastos/${id}`)
  return res.data
}
