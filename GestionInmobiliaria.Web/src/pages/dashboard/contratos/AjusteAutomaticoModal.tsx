import { useState } from 'react'
import { RefreshCw, X } from 'lucide-react'
import { actualizarAjusteAutomatico } from '../../../api/contratos'
import type { ContratoDto } from '../../../api/contratos'

export default function AjusteAutomaticoModal({ contrato, onConfirmado, onCerrar }: {
  contrato: ContratoDto
  onConfirmado: () => void
  onCerrar: () => void
}) {
  const [automatico, setAutomatico] = useState(contrato.ajusteAutomatico)
  const [tipoAjuste, setTipoAjuste] = useState(String(['Fijo', 'IndiceICL', 'Porcentaje', 'Otro', 'IndiceIPC', 'IndiceUVA'].indexOf(contrato.tipoAjuste) + 1))
  const [periodicidad, setPeriodicidad] = useState(contrato.periodicidadAjusteMeses ? String(contrato.periodicidadAjusteMeses) : '')
  const [porcentaje, setPorcentaje] = useState(contrato.porcentajeAjuste != null ? String(contrato.porcentajeAjuste) : '')
  const [guardando, setGuardando] = useState(false)
  const [error, setError] = useState('')

  const handleConfirmar = async () => {
    if (automatico && !periodicidad) {
      setError('Para ajustar automáticamente necesitás definir la periodicidad en meses.')
      return
    }
    setGuardando(true)
    setError('')
    try {
      const res = await actualizarAjusteAutomatico(contrato.id, {
        ajusteAutomatico: automatico,
        tipoAjuste: Number(tipoAjuste),
        periodicidadAjusteMeses: periodicidad ? Number(periodicidad) : undefined,
        porcentajeAjuste: porcentaje ? Number(porcentaje) : undefined,
      })
      if (res.success) onConfirmado()
      else setError(res.errors?.[0] ?? res.message ?? 'No se pudo actualizar la configuración.')
    } catch (err: unknown) {
      const axErr = err as { response?: { data?: { errors?: string[]; message?: string } } }
      setError(axErr.response?.data?.errors?.[0] ?? axErr.response?.data?.message ?? 'No se pudo actualizar la configuración.')
    } finally {
      setGuardando(false)
    }
  }

  const inp = 'border border-gray-200 rounded-xl px-4 py-2.5 text-sm w-full outline-none focus:border-blue-400'

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-md">

        <div className="flex items-center justify-between px-6 pt-5 pb-4 border-b border-gray-100">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 bg-blue-100 rounded-xl flex items-center justify-center shrink-0">
              <RefreshCw className="w-5 h-5 text-blue-600" />
            </div>
            <div>
              <h2 className="font-semibold text-gray-800">Ajuste automático</h2>
              <p className="text-xs text-gray-400 mt-0.5 font-mono">{contrato.codigo} · {contrato.propiedadDireccion}</p>
            </div>
          </div>
          <button onClick={onCerrar} className="text-gray-400 hover:text-gray-600">
            <X className="w-5 h-5" />
          </button>
        </div>

        <div className="px-6 py-5 space-y-4">
          <label className="flex items-center gap-2 text-sm text-gray-700 cursor-pointer">
            <input type="checkbox" checked={automatico} onChange={e => setAutomatico(e.target.checked)} />
            Aplicar el ajuste periódico automáticamente, sin confirmación manual
          </label>

          {automatico && (
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Tipo de ajuste</label>
                <select value={tipoAjuste} onChange={e => setTipoAjuste(e.target.value)} className={inp}>
                  <option value="1">Fijo</option>
                  <option value="2">Índice ICL</option>
                  <option value="3">Porcentaje</option>
                  <option value="4">Otro</option>
                  <option value="5">Índice IPC</option>
                  <option value="6">Índice UVA</option>
                </select>
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Periodicidad (meses)</label>
                <input type="number" min={1} value={periodicidad} onChange={e => setPeriodicidad(e.target.value)}
                  className={inp} placeholder="Ej: 6" />
              </div>
              {tipoAjuste === '3' && (
                <div className="col-span-2">
                  <label className="block text-xs font-medium text-gray-600 mb-1">Porcentaje de ajuste (%)</label>
                  <input type="number" min={0} step="0.01" value={porcentaje} onChange={e => setPorcentaje(e.target.value)}
                    className={inp} placeholder="Ej: 10" />
                </div>
              )}
              {tipoAjuste === '2' && (
                <p className="col-span-2 text-xs text-gray-400">
                  El monto se recalcula solo con la variación del índice ICL del BCRA entre el ajuste anterior y hoy.
                </p>
              )}
              {tipoAjuste === '5' && (
                <p className="col-span-2 text-xs text-gray-400">
                  El monto se recalcula solo con la variación del IPC (INDEC) entre el ajuste anterior y hoy.
                </p>
              )}
              {tipoAjuste === '6' && (
                <p className="col-span-2 text-xs text-gray-400">
                  El monto se recalcula solo con la variación del índice UVA del BCRA entre el ajuste anterior y hoy.
                </p>
              )}
            </div>
          )}

          {!automatico && (
            <p className="text-xs text-gray-400 bg-gray-50 rounded-xl px-4 py-3">
              El ajuste de este contrato va a seguir requiriendo aplicación manual desde "Aplicar ajuste".
            </p>
          )}

          {error && <p className="text-sm text-red-600">{error}</p>}
        </div>

        <div className="flex gap-3 px-6 pb-5">
          <button onClick={onCerrar}
            className="flex-1 border border-gray-200 text-gray-600 py-2.5 rounded-xl text-sm font-medium hover:bg-gray-50 transition-colors">
            Cancelar
          </button>
          <button onClick={handleConfirmar} disabled={guardando}
            className="flex-1 bg-blue-600 text-white py-2.5 rounded-xl text-sm font-medium hover:bg-blue-700 disabled:opacity-60 transition-colors">
            {guardando ? 'Guardando...' : 'Guardar'}
          </button>
        </div>
      </div>
    </div>
  )
}
