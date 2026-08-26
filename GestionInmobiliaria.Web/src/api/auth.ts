import client from './client'

export interface LoginRequest {
  email: string
  password: string
}

export interface TokenResponse {
  accessToken: string
  refreshToken: string
  email: string
  nombre: string
  apellido: string
  roles: string[]
  tenantId: number
  agenteId: number | null
}

export interface TenantLoginDto {
  nombre: string
  slug: string
}

export const login = async (data: LoginRequest, tenant: string): Promise<TokenResponse> => {
  const res = await client.post<TokenResponse>(
    '/api/auth/login',
    data,
    { headers: { 'X-Tenant': tenant } }
  )
  return res.data
}

export const resolverTenant = async (email: string): Promise<TenantLoginDto[]> => {
  const res = await client.post<{ success: boolean; data: TenantLoginDto[] }>(
    '/api/auth/resolver-tenant',
    { email },
    { headers: { 'X-Tenant': 'public' } }
  )
  return res.data.data ?? []
}
