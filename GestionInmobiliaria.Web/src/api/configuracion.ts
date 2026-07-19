import client from './client'
import type { ApiResponse } from '../types/api'

export interface ConfiguracionEmpresaDto {
  id: number
  nombreComercial: string
  razonSocial: string | null
  cuit: string | null
  condicionFiscal: string | null
  logoUrl: string | null
  slogan: string | null
  telefono: string | null
  whatsApp: string | null
  email: string | null
  sitioWeb: string | null
  direccion: string | null
  ciudad: string | null
  provincia: string | null
  codigoPostal: string | null
  pais: string | null
  instagram: string | null
  facebook: string | null
  twitter: string | null
  fechaActualizacion: string
  // SMTP
  emailHabilitado: boolean
  emailSmtpHost: string | null
  emailSmtpPuerto: number
  emailSmtpUsuario: string | null
  emailNombreRemitente: string | null
}

export interface ConfiguracionPublicaDto {
  nombreComercial: string
  slogan: string | null
  telefono: string | null
  whatsApp: string | null
  email: string | null
  sitioWeb: string | null
  logoUrl: string | null
  instagram: string | null
  facebook: string | null
  twitter: string | null
  tenantSlug: string | null
}

export const getConfiguracionPublica = async () => {
  const res = await client.get<ApiResponse<ConfiguracionPublicaDto>>('/api/configuracion/publica')
  return res.data
}

export const getConfiguracion = async () => {
  const res = await client.get<ApiResponse<ConfiguracionEmpresaDto>>('/api/configuracion')
  return res.data
}

export interface UpdateConfiguracionRequest extends Omit<ConfiguracionEmpresaDto, 'id' | 'fechaActualizacion'> {
  emailSmtpPassword?: string
}

export const updateConfiguracion = async (data: UpdateConfiguracionRequest) => {
  const res = await client.put<ApiResponse<ConfiguracionEmpresaDto>>('/api/configuracion', data)
  return res.data
}
