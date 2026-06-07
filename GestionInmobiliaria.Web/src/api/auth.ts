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
}

export const login = async (data: LoginRequest, tenant: string): Promise<TokenResponse> => {
  const res = await client.post<TokenResponse>(
    '/api/auth/login',
    data,
    { headers: { 'X-Tenant': tenant } }
  )
  return res.data
}
