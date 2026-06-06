import { useState } from 'react'
import { Link } from 'react-router-dom'
import { MapPin, Home, Search, Phone, Mail, ChevronRight, Star, Building2, TrendingUp, Users } from 'lucide-react'

const propiedadesDestacadas = [
  {
    id: 1,
    titulo: 'Departamento 3 ambientes con balcón',
    barrio: 'Palermo',
    precio: 'USD 145.000',
    tipo: 'Venta',
    superficie: '72 m²',
    ambientes: 3,
    imagen: 'https://images.unsplash.com/photo-1545324418-cc1a3fa10c00?w=600&q=80',
  },
  {
    id: 2,
    titulo: 'Casa con jardín y pileta',
    barrio: 'Belgrano',
    precio: '$ 850.000 / mes',
    tipo: 'Alquiler',
    superficie: '220 m²',
    ambientes: 5,
    imagen: 'https://images.unsplash.com/photo-1568605114967-8130f3a36994?w=600&q=80',
  },
  {
    id: 3,
    titulo: 'PH dúplex a estrenar',
    barrio: 'Villa Crespo',
    precio: 'USD 89.000',
    tipo: 'Venta',
    superficie: '55 m²',
    ambientes: 2,
    imagen: 'https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?w=600&q=80',
  },
]

const stats = [
  { icono: Building2, valor: '500+', label: 'Propiedades' },
  { icono: Users, valor: '1.200+', label: 'Clientes satisfechos' },
  { icono: TrendingUp, valor: '15 años', label: 'De experiencia' },
  { icono: Star, valor: '4.9', label: 'Calificación promedio' },
]

export default function LandingPage() {
  const [busqueda, setBusqueda] = useState('')
  const [tipo, setTipo] = useState('todos')

  return (
    <div className="min-h-screen bg-white font-sans">

      {/* NAVBAR */}
      <nav className="fixed top-0 left-0 right-0 z-50 bg-white/95 backdrop-blur border-b border-gray-100 shadow-sm">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex items-center justify-between h-16">
            <div className="flex items-center gap-2">
              <div className="w-8 h-8 bg-blue-900 rounded-lg flex items-center justify-center">
                <Home className="w-4 h-4 text-yellow-400" />
              </div>
              <span className="text-xl font-bold text-blue-900">García Propiedades</span>
            </div>
            <div className="hidden md:flex items-center gap-8 text-sm text-gray-600 font-medium">
              <a href="#propiedades" className="hover:text-blue-900 transition-colors">Propiedades</a>
              <a href="#tasacion" className="hover:text-blue-900 transition-colors">Tasaciones</a>
              <a href="#contacto" className="hover:text-blue-900 transition-colors">Contacto</a>
            </div>
            <Link
              to="/login"
              className="flex items-center gap-2 bg-blue-900 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-blue-800 transition-colors"
            >
              Ingresar
              <ChevronRight className="w-4 h-4" />
            </Link>
          </div>
        </div>
      </nav>

      {/* HERO */}
      <section className="relative min-h-screen flex items-center pt-16">
        <div
          className="absolute inset-0 bg-cover bg-center bg-no-repeat"
          style={{ backgroundImage: "url('https://images.unsplash.com/photo-1560518883-ce09059eeffa?w=1600&q=90')" }}
        >
          <div className="absolute inset-0 bg-gradient-to-r from-blue-950/90 via-blue-900/75 to-blue-900/40" />
        </div>

        <div className="relative max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-24">
          <div className="max-w-2xl">
            <span className="inline-block bg-yellow-400 text-blue-900 text-xs font-bold uppercase tracking-widest px-3 py-1 rounded-full mb-6">
              Tu inmobiliaria de confianza
            </span>
            <h1 className="text-5xl md:text-6xl font-bold text-white leading-tight mb-6">
              Encontrá tu<br />
              <span className="text-yellow-400">próximo hogar</span>
            </h1>
            <p className="text-xl text-blue-100 mb-10 leading-relaxed">
              Más de 500 propiedades disponibles en Buenos Aires.<br />
              Alquiler, venta y tasaciones online.
            </p>

            {/* BUSCADOR */}
            <div className="bg-white rounded-2xl p-4 shadow-2xl">
              <div className="flex gap-2 mb-3">
                {['todos', 'venta', 'alquiler'].map((t) => (
                  <button
                    key={t}
                    onClick={() => setTipo(t)}
                    className={`px-4 py-1.5 rounded-lg text-sm font-medium transition-colors capitalize ${
                      tipo === t
                        ? 'bg-blue-900 text-white'
                        : 'text-gray-500 hover:bg-gray-100'
                    }`}
                  >
                    {t === 'todos' ? 'Todos' : t.charAt(0).toUpperCase() + t.slice(1)}
                  </button>
                ))}
              </div>
              <div className="flex gap-2">
                <div className="flex-1 flex items-center gap-2 border border-gray-200 rounded-xl px-3">
                  <Search className="w-4 h-4 text-gray-400 shrink-0" />
                  <input
                    type="text"
                    placeholder="Barrio, zona o dirección..."
                    value={busqueda}
                    onChange={(e) => setBusqueda(e.target.value)}
                    className="w-full py-3 text-sm text-gray-700 outline-none placeholder-gray-400"
                  />
                </div>
                <button className="bg-yellow-400 hover:bg-yellow-500 text-blue-900 font-bold px-6 py-3 rounded-xl transition-colors text-sm">
                  Buscar
                </button>
              </div>
            </div>
          </div>
        </div>

        {/* SCROLL INDICATOR */}
        <div className="absolute bottom-8 left-1/2 -translate-x-1/2 flex flex-col items-center gap-2 text-white/60 text-xs">
          <span>Ver más</span>
          <div className="w-px h-8 bg-white/30 animate-pulse" />
        </div>
      </section>

      {/* STATS */}
      <section className="bg-blue-900 py-12">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid grid-cols-2 md:grid-cols-4 gap-8 text-center">
            {stats.map(({ icono: Icon, valor, label }) => (
              <div key={label}>
                <Icon className="w-6 h-6 text-yellow-400 mx-auto mb-2" />
                <div className="text-3xl font-bold text-white">{valor}</div>
                <div className="text-blue-300 text-sm mt-1">{label}</div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* PROPIEDADES DESTACADAS */}
      <section id="propiedades" className="py-20 bg-gray-50">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="text-center mb-12">
            <span className="text-yellow-600 font-semibold text-sm uppercase tracking-wider">Disponibles ahora</span>
            <h2 className="text-3xl font-bold text-blue-900 mt-2">Propiedades destacadas</h2>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
            {propiedadesDestacadas.map((p) => (
              <div key={p.id} className="bg-white rounded-2xl overflow-hidden shadow-md hover:shadow-xl transition-shadow group cursor-pointer">
                <div className="relative overflow-hidden h-52">
                  <img
                    src={p.imagen}
                    alt={p.titulo}
                    className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500"
                  />
                  <span className={`absolute top-3 left-3 px-3 py-1 rounded-full text-xs font-bold ${
                    p.tipo === 'Venta' ? 'bg-blue-900 text-white' : 'bg-yellow-400 text-blue-900'
                  }`}>
                    {p.tipo}
                  </span>
                </div>
                <div className="p-5">
                  <h3 className="font-semibold text-gray-800 mb-1 leading-snug">{p.titulo}</h3>
                  <div className="flex items-center gap-1 text-gray-400 text-sm mb-3">
                    <MapPin className="w-3.5 h-3.5" />
                    <span>{p.barrio}</span>
                  </div>
                  <div className="flex items-center gap-3 text-xs text-gray-500 mb-4">
                    <span className="flex items-center gap-1">
                      <Home className="w-3.5 h-3.5" /> {p.ambientes} amb.
                    </span>
                    <span>•</span>
                    <span>{p.superficie}</span>
                  </div>
                  <div className="flex items-center justify-between">
                    <span className="text-lg font-bold text-blue-900">{p.precio}</span>
                    <button className="text-yellow-600 hover:text-yellow-700 text-sm font-medium flex items-center gap-1">
                      Ver más <ChevronRight className="w-4 h-4" />
                    </button>
                  </div>
                </div>
              </div>
            ))}
          </div>

          <div className="text-center mt-10">
            <button className="border-2 border-blue-900 text-blue-900 px-8 py-3 rounded-xl font-semibold hover:bg-blue-900 hover:text-white transition-colors">
              Ver todas las propiedades
            </button>
          </div>
        </div>
      </section>

      {/* TASACIÓN CTA */}
      <section id="tasacion" className="py-20 bg-white">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="bg-gradient-to-br from-blue-900 to-blue-800 rounded-3xl overflow-hidden">
            <div className="grid md:grid-cols-2 gap-0">
              <div className="p-12 flex flex-col justify-center">
                <span className="text-yellow-400 font-semibold text-sm uppercase tracking-wider mb-4">Servicio gratuito</span>
                <h2 className="text-3xl font-bold text-white mb-4 leading-tight">
                  ¿Querés saber cuánto<br />vale tu propiedad?
                </h2>
                <p className="text-blue-200 mb-8 leading-relaxed">
                  Completá el formulario con los datos de tu propiedad y un agente especializado
                  te contactará para coordinar una tasación presencial u online.
                </p>
                <div className="flex flex-col sm:flex-row gap-3">
                  <Link
                    to="/tasacion"
                    className="inline-flex items-center justify-center gap-2 bg-yellow-400 hover:bg-yellow-500 text-blue-900 font-bold px-8 py-4 rounded-xl transition-colors"
                  >
                    Solicitar tasación gratuita
                    <ChevronRight className="w-5 h-5" />
                  </Link>
                  <a
                    href="https://wa.me/5491112345678"
                    className="inline-flex items-center justify-center gap-2 border-2 border-white/30 text-white hover:border-white px-8 py-4 rounded-xl transition-colors text-sm font-medium"
                  >
                    <Phone className="w-4 h-4" />
                    Llamar ahora
                  </a>
                </div>
              </div>
              <div className="hidden md:block relative">
                <img
                  src="https://images.unsplash.com/photo-1600585154340-be6161a56a0c?w=800&q=80"
                  alt="Tasación de propiedades"
                  className="w-full h-full object-cover"
                />
                <div className="absolute inset-0 bg-blue-900/20" />
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* CONTACTO */}
      <section id="contacto" className="py-20 bg-gray-50">
        <div className="max-w-3xl mx-auto px-4 text-center">
          <h2 className="text-3xl font-bold text-blue-900 mb-4">¿Necesitás ayuda?</h2>
          <p className="text-gray-500 mb-10">Nuestro equipo está disponible de lunes a sábados de 9 a 18 hs.</p>
          <div className="grid sm:grid-cols-2 gap-6">
            <a href="tel:+5491112345678" className="flex items-center gap-4 bg-white p-6 rounded-2xl shadow-sm hover:shadow-md transition-shadow">
              <div className="w-12 h-12 bg-blue-100 rounded-xl flex items-center justify-center shrink-0">
                <Phone className="w-5 h-5 text-blue-900" />
              </div>
              <div className="text-left">
                <div className="text-xs text-gray-400 mb-1">Teléfono</div>
                <div className="font-semibold text-gray-800">+54 11 1234-5678</div>
              </div>
            </a>
            <a href="mailto:info@garciapropiedades.com.ar" className="flex items-center gap-4 bg-white p-6 rounded-2xl shadow-sm hover:shadow-md transition-shadow">
              <div className="w-12 h-12 bg-yellow-100 rounded-xl flex items-center justify-center shrink-0">
                <Mail className="w-5 h-5 text-yellow-600" />
              </div>
              <div className="text-left">
                <div className="text-xs text-gray-400 mb-1">Email</div>
                <div className="font-semibold text-gray-800">info@garciapropiedades.com.ar</div>
              </div>
            </a>
          </div>
        </div>
      </section>

      {/* FOOTER */}
      <footer className="bg-blue-950 text-blue-300 py-8">
        <div className="max-w-7xl mx-auto px-4 flex flex-col sm:flex-row items-center justify-between gap-4 text-sm">
          <div className="flex items-center gap-2">
            <div className="w-6 h-6 bg-yellow-400 rounded flex items-center justify-center">
              <Home className="w-3 h-3 text-blue-900" />
            </div>
            <span className="text-white font-semibold">García Propiedades</span>
          </div>
          <span>© {new Date().getFullYear()} Todos los derechos reservados.</span>
        </div>
      </footer>

    </div>
  )
}
