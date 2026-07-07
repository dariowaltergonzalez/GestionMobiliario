import client from './client'
import type { ApiResponse, PagedResult } from '../types/api'

export interface ReservaDto {
  id: number
  propiedadId: number
  propiedadDireccion: string
  leadId: number | null
  agenteId: number | null
  agenteNombre: string | null
  compradorNombre: string
  compradorApellido: string
  compradorDni: string | null
  compradorTelefono: string | null
  compradorEmail: string | null
  vendedorNombre: string
  vendedorApellido: string
  vendedorDni: string | null
  vendedorTelefono: string | null
  vendedorEmail: string | null
  montoSenia: number
  precioTotal: number | null
  moneda: string
  medioDeposito: string
  fechaReserva: string
  fechaVencimiento: string
  estado: string
  observaciones: string | null
  fechaCreacion: string
  fechaActualizacion: string
}

export interface CreateReservaRequest {
  propiedadId: number
  leadId: number | null
  agenteId: number | null
  compradorNombre: string
  compradorApellido: string
  compradorDni?: string
  compradorTelefono?: string
  compradorEmail?: string
  vendedorNombre: string
  vendedorApellido: string
  vendedorDni?: string
  vendedorTelefono?: string
  vendedorEmail?: string
  montoSenia: number
  precioTotal?: number
  moneda: number
  medioDeposito: number
  fechaReserva: string
  fechaVencimiento: string
  observaciones?: string
}

export interface UpdateReservaRequest extends CreateReservaRequest {
  estado: number
}

export interface FiltrosReservas {
  buscar?: string
  estado?: string
  pagina: number
  tamano: number
}

export const ESTADOS_RESERVA: Record<number, { label: string; color: string }> = {
  1: { label: 'Vigente',    color: 'bg-green-100 text-green-700' },
  2: { label: 'Vencida',    color: 'bg-gray-100 text-gray-500' },
  3: { label: 'Cancelada',  color: 'bg-red-100 text-red-600' },
  4: { label: 'Convertida', color: 'bg-blue-100 text-blue-700' },
}

export const MONEDAS = { 1: 'ARS', 2: 'USD' }
export const MEDIOS_DEPOSITO: Record<number, string> = {
  1: 'Efectivo',
  2: 'Transferencia',
  3: 'Cuenta inmobiliaria',
  4: 'Escribanía',
}

export function estadoReservaNumero(s: string): number {
  return ({ Vigente: 1, Vencida: 2, Cancelada: 3, Convertida: 4 } as Record<string, number>)[s] ?? 1
}

export const getReservas = async (filtros: FiltrosReservas) => {
  const params: Record<string, string> = {
    pagina: String(filtros.pagina),
    tamano: String(filtros.tamano),
  }
  if (filtros.buscar) params.buscar = filtros.buscar
  if (filtros.estado) params.estado = filtros.estado
  const res = await client.get<ApiResponse<PagedResult<ReservaDto>>>('/api/reservas', { params })
  return res.data
}

export const getReserva = async (id: number) => {
  const res = await client.get<ApiResponse<ReservaDto>>(`/api/reservas/${id}`)
  return res.data
}

export const createReserva = async (data: CreateReservaRequest) => {
  const res = await client.post<ApiResponse<ReservaDto>>('/api/reservas', data)
  return res.data
}

export const updateReserva = async (id: number, data: UpdateReservaRequest) => {
  const res = await client.put<ApiResponse<ReservaDto>>(`/api/reservas/${id}`, data)
  return res.data
}

export const deleteReserva = async (id: number) => {
  const res = await client.delete<ApiResponse<null>>(`/api/reservas/${id}`)
  return res.data
}
