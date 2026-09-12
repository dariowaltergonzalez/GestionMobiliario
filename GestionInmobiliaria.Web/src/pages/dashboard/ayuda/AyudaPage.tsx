import { useMemo, useState } from 'react'
import { Search, HelpCircle } from 'lucide-react'
import DashboardLayout from '../../../components/layout/DashboardLayout'
import { temasAyuda } from './contenidoAyuda'

function normalizar(texto: string) {
  return texto
    .toLowerCase()
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '')
}

export default function AyudaPage() {
  const [buscar, setBuscar] = useState('')

  const temasFiltrados = useMemo(() => {
    const q = normalizar(buscar.trim())
    if (!q) return temasAyuda
    return temasAyuda.filter(t => normalizar(t.titulo).includes(q) || normalizar(t.texto).includes(q))
  }, [buscar])

  return (
    <DashboardLayout titulo="Ayuda">
      <div className="max-w-3xl mx-auto">
        <div className="flex items-center gap-3 mb-6">
          <div className="w-10 h-10 bg-blue-100 rounded-xl flex items-center justify-center shrink-0">
            <HelpCircle className="w-5 h-5 text-blue-900" />
          </div>
          <div>
            <h2 className="font-bold text-gray-800">¿Para qué sirve cada cosa?</h2>
            <p className="text-sm text-gray-500">Buscá por palabra clave para encontrar cómo funciona un módulo.</p>
          </div>
        </div>

        <div className="relative mb-6">
          <Search className="w-4 h-4 text-gray-400 absolute left-3 top-1/2 -translate-y-1/2" />
          <input
            type="text"
            value={buscar}
            onChange={e => setBuscar(e.target.value)}
            placeholder="Ej: liquidación, WhatsApp, índice, contrato..."
            className="w-full border border-gray-200 rounded-xl pl-10 pr-4 py-2.5 text-sm text-gray-700 outline-none focus:ring-2 focus:ring-blue-900/20 focus:border-blue-900 transition"
          />
        </div>

        {temasFiltrados.length === 0 ? (
          <div className="text-center py-16 text-gray-400 text-sm">
            No encontramos nada que coincida con "{buscar}".
          </div>
        ) : (
          <div className="space-y-3">
            {temasFiltrados.map(tema => (
              <div key={tema.titulo} className="bg-white border border-gray-100 rounded-xl px-5 py-4 shadow-sm">
                <h3 className="font-semibold text-gray-800 text-sm mb-1.5">{tema.titulo}</h3>
                <p className="text-sm text-gray-600 leading-relaxed">{tema.texto}</p>
              </div>
            ))}
          </div>
        )}
      </div>
    </DashboardLayout>
  )
}
