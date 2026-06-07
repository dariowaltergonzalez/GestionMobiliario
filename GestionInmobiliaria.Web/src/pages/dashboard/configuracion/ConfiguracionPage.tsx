import { useState, useEffect } from 'react'
import { Loader2, Save, Check } from 'lucide-react'
import DashboardLayout from '../../../components/layout/DashboardLayout'
import { getConfiguracion, updateConfiguracion, type ConfiguracionEmpresaDto } from '../../../api/configuracion'

type FormData = Omit<ConfiguracionEmpresaDto, 'id' | 'fechaActualizacion'>

const formVacio: FormData = {
  nombreComercial: '', razonSocial: '', cuit: '', condicionFiscal: '',
  logoUrl: '', slogan: '', telefono: '', whatsApp: '', email: '',
  sitioWeb: '', direccion: '', ciudad: '', provincia: '', codigoPostal: '',
  pais: '', instagram: '', facebook: '', twitter: '',
}

const Input = ({ label, ...props }: { label: string } & React.InputHTMLAttributes<HTMLInputElement>) => (
  <div>
    <label className="block text-xs font-medium text-gray-600 mb-1">{label}</label>
    <input
      {...props}
      className="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm text-gray-700 outline-none focus:ring-2 focus:ring-blue-900/20 focus:border-blue-900 transition"
    />
  </div>
)

const Seccion = ({ titulo, children }: { titulo: string; children: React.ReactNode }) => (
  <div className="bg-white rounded-2xl border border-gray-100 shadow-sm p-6">
    <h2 className="text-sm font-semibold text-gray-500 uppercase tracking-wider mb-4">{titulo}</h2>
    <div className="space-y-4">{children}</div>
  </div>
)

export default function ConfiguracionPage() {
  const [form, setForm] = useState<FormData>(formVacio)
  const [loading, setLoading] = useState(true)
  const [guardando, setGuardando] = useState(false)
  const [guardado, setGuardado] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    getConfiguracion().then(res => {
      if (res.success && res.data) {
        const d = res.data
        setForm({
          nombreComercial: d.nombreComercial ?? '',
          razonSocial: d.razonSocial ?? '',
          cuit: d.cuit ?? '',
          condicionFiscal: d.condicionFiscal ?? '',
          logoUrl: d.logoUrl ?? '',
          slogan: d.slogan ?? '',
          telefono: d.telefono ?? '',
          whatsApp: d.whatsApp ?? '',
          email: d.email ?? '',
          sitioWeb: d.sitioWeb ?? '',
          direccion: d.direccion ?? '',
          ciudad: d.ciudad ?? '',
          provincia: d.provincia ?? '',
          codigoPostal: d.codigoPostal ?? '',
          pais: d.pais ?? '',
          instagram: d.instagram ?? '',
          facebook: d.facebook ?? '',
          twitter: d.twitter ?? '',
        })
      }
    }).catch(() => setError('No se pudo cargar la configuración.'))
    .finally(() => setLoading(false))
  }, [])

  const set = (campo: keyof FormData, valor: string) =>
    setForm(f => ({ ...f, [campo]: valor }))

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!form.nombreComercial.trim()) { setError('El nombre comercial es requerido.'); return }
    setError('')
    setGuardando(true)
    try {
      await updateConfiguracion(form)
      setGuardado(true)
      setTimeout(() => setGuardado(false), 3000)
    } catch {
      setError('No se pudo guardar la configuración.')
    } finally {
      setGuardando(false)
    }
  }

  if (loading) return (
    <DashboardLayout titulo="Configuración de Empresa">
      <div className="flex items-center justify-center py-24 text-gray-400">Cargando...</div>
    </DashboardLayout>
  )

  return (
    <DashboardLayout titulo="Configuración de Empresa">
      <form onSubmit={handleSubmit}>
        <div className="space-y-6 max-w-3xl">

          {error && (
            <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-xl text-sm">{error}</div>
          )}

          <Seccion titulo="Datos de la empresa">
            <Input label="Nombre comercial *" value={form.nombreComercial} onChange={e => set('nombreComercial', e.target.value)} placeholder="García Propiedades" required />
            <Input label="Razón social" value={form.razonSocial ?? ''} onChange={e => set('razonSocial', e.target.value)} placeholder="García Propiedades S.R.L." />
            <div className="grid grid-cols-2 gap-4">
              <Input label="CUIT" value={form.cuit ?? ''} onChange={e => set('cuit', e.target.value)} placeholder="20-12345678-3" />
              <Input label="Condición fiscal" value={form.condicionFiscal ?? ''} onChange={e => set('condicionFiscal', e.target.value)} placeholder="Responsable Inscripto" />
            </div>
            <Input label="Slogan" value={form.slogan ?? ''} onChange={e => set('slogan', e.target.value)} placeholder="Tu hogar, nuestra pasión" />
          </Seccion>

          <Seccion titulo="Contacto">
            <div className="grid grid-cols-2 gap-4">
              <Input label="Teléfono" value={form.telefono ?? ''} onChange={e => set('telefono', e.target.value)} placeholder="2664 123456" />
              <Input label="WhatsApp" value={form.whatsApp ?? ''} onChange={e => set('whatsApp', e.target.value)} placeholder="2664 123456" />
            </div>
            <Input label="Email" type="email" value={form.email ?? ''} onChange={e => set('email', e.target.value)} placeholder="contacto@empresa.com" />
            <Input label="Sitio web" value={form.sitioWeb ?? ''} onChange={e => set('sitioWeb', e.target.value)} placeholder="https://www.empresa.com" />
          </Seccion>

          <Seccion titulo="Ubicación">
            <Input label="Dirección" value={form.direccion ?? ''} onChange={e => set('direccion', e.target.value)} placeholder="Av. San Martín 456" />
            <div className="grid grid-cols-3 gap-4">
              <Input label="Ciudad" value={form.ciudad ?? ''} onChange={e => set('ciudad', e.target.value)} placeholder="San Luis" />
              <Input label="Provincia" value={form.provincia ?? ''} onChange={e => set('provincia', e.target.value)} placeholder="San Luis" />
              <Input label="Código postal" value={form.codigoPostal ?? ''} onChange={e => set('codigoPostal', e.target.value)} placeholder="5700" />
            </div>
            <Input label="País" value={form.pais ?? ''} onChange={e => set('pais', e.target.value)} placeholder="Argentina" />
          </Seccion>

          <Seccion titulo="Redes sociales">
            <div className="grid grid-cols-3 gap-4">
              <Input label="Instagram" value={form.instagram ?? ''} onChange={e => set('instagram', e.target.value)} placeholder="@empresa" />
              <Input label="Facebook" value={form.facebook ?? ''} onChange={e => set('facebook', e.target.value)} placeholder="empresa" />
              <Input label="Twitter / X" value={form.twitter ?? ''} onChange={e => set('twitter', e.target.value)} placeholder="@empresa" />
            </div>
          </Seccion>

          <Seccion titulo="Personalización">
            <Input label="URL del logo" value={form.logoUrl ?? ''} onChange={e => set('logoUrl', e.target.value)} placeholder="https://..." />
            {form.logoUrl && (
              <img src={form.logoUrl} alt="Logo" className="h-16 object-contain rounded-lg border border-gray-100 p-2" onError={e => (e.currentTarget.style.display = 'none')} />
            )}
          </Seccion>

          <div className="flex justify-end">
            <button
              type="submit"
              disabled={guardando}
              className={`flex items-center gap-2 px-6 py-2.5 rounded-xl text-sm font-semibold transition-colors ${
                guardado
                  ? 'bg-green-600 text-white'
                  : 'bg-blue-900 hover:bg-blue-800 text-white disabled:opacity-60'
              }`}
            >
              {guardando ? <><Loader2 className="w-4 h-4 animate-spin" /> Guardando...</>
               : guardado ? <><Check className="w-4 h-4" /> Guardado</>
               : <><Save className="w-4 h-4" /> Guardar cambios</>}
            </button>
          </div>

        </div>
      </form>
    </DashboardLayout>
  )
}
