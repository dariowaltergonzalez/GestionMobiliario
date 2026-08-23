import { useState } from 'react'
import { Percent, X } from 'lucide-react'
import { actualizarPunitorios } from '../../../api/contratos'
import type { ContratoDto } from '../../../api/contratos'

export default function PunitoriosModal({ contrato, onConfirmado, onCerrar }: {
  contrato: ContratoDto
  onConfirmado: () => void
  onCerrar: () => void
}) {
  const [aplica, setAplica] = useState(contrato.aplicaPunitorios)
  const [porcentaje, setPorcentaje] = useState(contrato.punitorioPorcentaje != null ? String(contrato.punitorioPorcentaje) : '')
  const [guardando, setGuardando] = useState(false)
  const [error, setError] = useState('')

  const handleConfirmar = async () => {
    setGuardando(true)
    setError('')
    try {
      const res = await actualizarPunitorios(contrato.id, {
        aplicaPunitorios: aplica,
        punitorioPorcentaje: aplica && porcentaje ? Number(porcentaje) : undefined,
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

  const inp = 'border border-gray-200 rounded-xl px-4 py-2.5 text-sm w-full outline-none focus:border-red-400'

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-md">

        <div className="flex items-center justify-between px-6 pt-5 pb-4 border-b border-gray-100">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 bg-red-100 rounded-xl flex items-center justify-center shrink-0">
              <Percent className="w-5 h-5 text-red-600" />
            </div>
            <div>
              <h2 className="font-semibold text-gray-800">Punitorios por mora</h2>
              <p className="text-xs text-gray-400 mt-0.5 font-mono">{contrato.codigo} · {contrato.propiedadDireccion}</p>
            </div>
          </div>
          <button onClick={onCerrar} className="text-gray-400 hover:text-gray-600">
            <X className="w-5 h-5" />
          </button>
        </div>

        <div className="px-6 py-5 space-y-4">
          <label className="flex items-center gap-2 text-sm text-gray-700 cursor-pointer">
            <input type="checkbox" checked={aplica} onChange={e => setAplica(e.target.checked)} />
            Aplicar punitorios por mora a este contrato
          </label>

          {aplica && (
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">% diario fijo (opcional)</label>
              <input type="number" min={0} step={0.0001} value={porcentaje} onChange={e => setPorcentaje(e.target.value)}
                className={inp} placeholder="0.0000" />
              <p className="text-xs text-gray-400 mt-1">
                Si lo dejás vacío, el recargo por mora se calcula con la tasa TIM del BCRA en vez de un % fijo.
              </p>
            </div>
          )}

          {!aplica && (
            <p className="text-xs text-gray-400 bg-gray-50 rounded-xl px-4 py-3">
              No se va a calcular ni mostrar ningún punitorio para las cuotas de este contrato.
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
            className="flex-1 bg-red-600 text-white py-2.5 rounded-xl text-sm font-medium hover:bg-red-700 disabled:opacity-60 transition-colors">
            {guardando ? 'Guardando...' : 'Guardar'}
          </button>
        </div>
      </div>
    </div>
  )
}
