import client from './client'
import type { ApiResponse } from '../types/api'

export interface PortalContratoDto {
  codigo: string
  propiedadDireccion: string
  montoActual: number
  moneda: string
  fechaInicio: string
  fechaFin: string | null
}

export interface PortalPagoDto {
  numeroCuota: number
  periodo: string
  montoEsperado: number
  montoPagado: number | null
  estado: string
  fechaPago: string | null
  montoPunitorio: number
  diasAtraso: number
}

export interface PortalGastoDto {
  categoria: string
  descripcion: string | null
  monto: number
  fecha: string
  estado: string
}

export interface PortalInquilinoDto {
  nombreEmpresa: string
  logoUrl: string | null
  inquilinoNombre: string
  inquilinoApellido: string
  contrato: PortalContratoDto | null
  pagos: PortalPagoDto[]
  gastos: PortalGastoDto[]
}

export interface PortalAbonoDto {
  monto: number
  fecha: string
  medio: string
  cbuCvuDestino: string | null
  entidadDestino: string | null
  numeroOperacion: string | null
  comprobanteUrl: string | null
}

export interface PortalLiquidacionDto {
  propiedadDireccion: string
  contratoCodigo: string
  periodo: string
  montoCobrado: number
  montoComision: number
  montoGastos: number
  montoALiquidar: number
  montoAbonado: number
  estado: string
  fechaLiquidacion: string | null
  abonos: PortalAbonoDto[]
  gastos: PortalGastoDto[]
}

export interface PortalPropietarioDto {
  nombreEmpresa: string
  logoUrl: string | null
  propietarioNombre: string
  propietarioApellido: string
  liquidaciones: PortalLiquidacionDto[]
}

export const getPortalInquilino = async (token: string) => {
  const res = await client.get<ApiResponse<PortalInquilinoDto>>(`/api/portal/inquilino/${token}`)
  return res.data
}

export const getPortalPropietario = async (token: string) => {
  const res = await client.get<ApiResponse<PortalPropietarioDto>>(`/api/portal/propietario/${token}`)
  return res.data
}
