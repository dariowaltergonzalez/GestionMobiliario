import { useState } from 'react'
import { TrendingUp, X, AlertTriangle, History, ChevronDown, ChevronUp } from 'lucide-react'
import client from '../../../api/client'
import type { ApiResponse } from '../../../types/api'
import type { ContratoDto, AjusteContratoDto } from '../../../api/contratos'

function formatMoneda(monto: number, moneda: string) {
  const fmt = monto.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
  return moneda === 'ARS' ? `$${fmt}` : `U$S ${fmt}`
}

function formatFecha(iso: string) {
  return new Date(iso).toLocaleDateString('es-AR', { day: '2-digit', month: 'short', year: 'numeric' })
}

export default function AjusteModal({ contrato, onConfirmado, onCerrar }: {
  contrato: ContratoDto
  onConfirmado: () => void
  onCerrar: () => void
}) {
  const [tipo, setTipo] = useState<'Porcentaje' | 'MontoFijo'>('Porcentaje')
  const [valor, setValor] = useState(contrato.porcentajeAjuste != null ? String(contrato.porcentajeAjuste) : '')
  const [observaciones, setObservaciones] = useState('')
  const [guardando, setGuardando] = useState(false)
  const [error, setError] = useState('')
  const [mostrarHistorial, setMostrarHistorial] = useState(false)

  const historial = contrato.ajustes ?? []

  const valorNum = parseFloat(valor) || 0
  const montoActual = contrato.montoActual || contrato.montoBase

  const montoNuevo = tipo === 'Porcentaje'
    ? Math.round(montoActual * (1 + valorNum / 100) * 100) / 100
    : Math.round((montoActual + valorNum) * 100) / 100

  const cuotasPendientes = contrato.pagos.filter(
    p => p.estado === 'Pendiente' || p.estado === 'Atrasado'
  ).length

  const diferencia = montoNuevo - montoActual
  const esAumento = diferencia > 0

  const handleConfirmar = async () => {
    if (!valorNum || valorNum === 0) { setError('Ingresá un valor distinto de cero.'); return }
    if (montoNuevo <= 0) { setError('El nuevo monto no puede ser menor o igual a cero.'); return }
    setGuardando(true); setError('')
    try {
      const res = await client.post<ApiResponse<AjusteContratoDto>>(
        `/api/contratos/${contrato.id}/ajustes`,
        { tipo, valor: valorNum, observaciones: observaciones.trim() || undefined }
      )
      if (res.data.success) {
        onConfirmado()
      } else {
        setError(res.data.errors?.[0] ?? 'Error al aplicar el ajuste.')
      }
    } catch { setError('No se pudo aplicar el ajuste.') }
    finally { setGuardando(false) }
  }

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-md">

        {/* Header */}
        <div className="flex items-center justify-between px-6 pt-5 pb-4 border-b border-gray-100">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 bg-blue-100 rounded-xl flex items-center justify-center shrink-0">
              <TrendingUp className="w-5 h-5 text-blue-600" />
            </div>
            <div>
              <h2 className="font-semibold text-gray-800">Aplicar ajuste de cuotas</h2>
              <p className="text-xs text-gray-400 mt-0.5 font-mono">{contrato.codigo} · {contrato.propiedadDireccion}</p>
            </div>
          </div>
          <button onClick={onCerrar} className="text-gray-400 hover:text-gray-600">
            <X className="w-5 h-5" />
          </button>
        </div>

        <div className="px-6 py-5 space-y-4">

          {/* Monto actual */}
          <div className="bg-gray-50 rounded-xl px-4 py-3">
            <p className="text-xs text-gray-500 mb-0.5">Monto actual por cuota</p>
            <p className="text-xl font-bold text-gray-800">{formatMoneda(montoActual, contrato.moneda)}</p>
            <p className="text-xs text-gray-400 mt-0.5">{cuotasPendientes} cuota(s) pendiente(s) a actualizar</p>
          </div>

          {/* Historial de ajustes */}
          {historial.length > 0 && (
            <div className="border border-gray-100 rounded-xl overflow-hidden">
              <button
                type="button"
                onClick={() => setMostrarHistorial(v => !v)}
                className="w-full flex items-center justify-between px-4 py-2.5 bg-gray-50 hover:bg-gray-100 transition-colors"
              >
                <span className="flex items-center gap-2 text-sm font-medium text-gray-600">
                  <History className="w-4 h-4 text-gray-400" />
                  Historial de ajustes ({historial.length})
                </span>
                {mostrarHistorial ? <ChevronUp className="w-4 h-4 text-gray-400" /> : <ChevronDown className="w-4 h-4 text-gray-400" />}
              </button>
              {mostrarHistorial && (
                <div className="max-h-48 overflow-y-auto divide-y divide-gray-50">
                  {historial.map(a => (
                    <div key={a.id} className="px-4 py-2.5">
                      <div className="flex items-center justify-between gap-2">
                        <span className="text-xs text-gray-400">{formatFecha(a.fechaAplicacion)}</span>
                        <span className="text-xs px-2 py-0.5 rounded-full font-medium bg-blue-50 text-blue-600">
                          {a.tipoAjuste === 'Porcentaje' ? `${a.porcentaje}%` : 'Monto fijo'}
                        </span>
                      </div>
                      <p className="text-sm text-gray-700 mt-1">
                        {formatMoneda(a.montoPrevio, contrato.moneda)}
                        <span className="text-gray-400 mx-1.5">→</span>
                        <span className="font-semibold">{formatMoneda(a.montoNuevo, contrato.moneda)}</span>
                      </p>
                      {a.observaciones && (
                        <p className="text-xs text-gray-400 mt-0.5">{a.observaciones}</p>
                      )}
                    </div>
                  ))}
                </div>
              )}
            </div>
          )}

          {/* Tipo de ajuste */}
          <div className="grid grid-cols-2 gap-2">
            <button
              onClick={() => setTipo('Porcentaje')}
              className={`py-2.5 rounded-xl text-sm font-medium border transition-colors ${
                tipo === 'Porcentaje'
                  ? 'bg-blue-600 text-white border-blue-600'
                  : 'border-gray-200 text-gray-600 hover:bg-gray-50'
              }`}
            >
              Por porcentaje (%)
            </button>
            <button
              onClick={() => setTipo('MontoFijo')}
              className={`py-2.5 rounded-xl text-sm font-medium border transition-colors ${
                tipo === 'MontoFijo'
                  ? 'bg-blue-600 text-white border-blue-600'
                  : 'border-gray-200 text-gray-600 hover:bg-gray-50'
              }`}
            >
              Monto fijo ({contrato.moneda})
            </button>
          </div>

          {/* Valor */}
          <div>
            <label className="block text-xs font-medium text-gray-600 mb-1">
              {tipo === 'Porcentaje' ? 'Porcentaje (positivo = aumento, negativo = baja)' : 'Importe a sumar o restar'}
            </label>
            <div className="relative">
              <input
                type="number"
                step={tipo === 'Porcentaje' ? '0.01' : '1'}
                value={valor}
                onChange={e => setValor(e.target.value)}
                placeholder={tipo === 'Porcentaje' ? 'Ej: 15 o -10' : 'Ej: 30000 o -5000'}
                className="border border-gray-200 rounded-xl px-4 py-2.5 text-sm w-full outline-none focus:border-blue-400 pr-12"
              />
              <span className="absolute right-4 top-1/2 -translate-y-1/2 text-sm text-gray-400 font-medium">
                {tipo === 'Porcentaje' ? '%' : contrato.moneda}
              </span>
            </div>
            {contrato.porcentajeAjuste != null && tipo === 'Porcentaje' && (
              <p className="text-xs text-gray-400 mt-1">
                Sugerido del contrato: <button className="text-blue-600 underline" onClick={() => setValor(String(contrato.porcentajeAjuste))}>
                  {contrato.porcentajeAjuste}%
                </button>
              </p>
            )}
          </div>

          {/* Preview */}
          {valorNum !== 0 && montoNuevo > 0 && (
            <div className={`rounded-xl px-4 py-3 border ${esAumento ? 'bg-green-50 border-green-200' : 'bg-orange-50 border-orange-200'}`}>
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-xs text-gray-500">Nuevo monto por cuota</p>
                  <p className={`text-xl font-bold ${esAumento ? 'text-green-700' : 'text-orange-700'}`}>
                    {formatMoneda(montoNuevo, contrato.moneda)}
                  </p>
                </div>
                <div className="text-right">
                  <p className="text-xs text-gray-500">Diferencia</p>
                  <p className={`text-sm font-semibold ${esAumento ? 'text-green-600' : 'text-orange-600'}`}>
                    {esAumento ? '+' : ''}{formatMoneda(diferencia, contrato.moneda)}
                  </p>
                  {tipo === 'Porcentaje' && (
                    <p className="text-xs text-gray-400">{esAumento ? '+' : ''}{valorNum}%</p>
                  )}
                </div>
              </div>
            </div>
          )}

          {montoNuevo <= 0 && valorNum !== 0 && (
            <div className="flex items-center gap-2 bg-red-50 border border-red-200 rounded-xl px-4 py-3">
              <AlertTriangle className="w-4 h-4 text-red-500 shrink-0" />
              <p className="text-sm text-red-700">El nuevo monto no puede ser cero o negativo.</p>
            </div>
          )}

          {/* Observaciones */}
          <div>
            <label className="block text-xs font-medium text-gray-600 mb-1">Observaciones (opcional)</label>
            <input
              type="text"
              value={observaciones}
              onChange={e => setObservaciones(e.target.value)}
              placeholder="Ej: Actualización ICL enero 2026"
              className="border border-gray-200 rounded-xl px-4 py-2.5 text-sm w-full outline-none focus:border-blue-400"
            />
          </div>

          {error && (
            <p className="text-sm text-red-600">{error}</p>
          )}
        </div>

        {/* Footer */}
        <div className="flex gap-3 px-6 pb-5">
          <button onClick={onCerrar}
            className="flex-1 border border-gray-200 text-gray-600 py-2.5 rounded-xl text-sm font-medium hover:bg-gray-50 transition-colors">
            Cancelar
          </button>
          <button
            onClick={handleConfirmar}
            disabled={guardando || !valorNum || montoNuevo <= 0 || cuotasPendientes === 0}
            className="flex-1 bg-blue-600 text-white py-2.5 rounded-xl text-sm font-medium hover:bg-blue-700 disabled:opacity-60 transition-colors"
          >
            {guardando ? 'Aplicando...' : `Aplicar a ${cuotasPendientes} cuota(s)`}
          </button>
        </div>
      </div>
    </div>
  )
}
