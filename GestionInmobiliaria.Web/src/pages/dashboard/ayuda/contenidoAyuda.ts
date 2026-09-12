export interface TemaAyuda {
  titulo: string
  texto: string
}

export const temasAyuda: TemaAyuda[] = [
  {
    titulo: 'Dashboard',
    texto: 'Pantalla de inicio con un resumen general: cuántas propiedades hay disponibles, contratos vigentes, cobros pendientes del mes y alertas importantes, todo de un vistazo.',
  },
  {
    titulo: 'Propiedades',
    texto: 'Alta y gestión de cada inmueble: dirección, tipo (casa, depto, local...), operación (alquiler o venta), características (ambientes, dormitorios, cochera, pileta...), fotos y video, y su estado (disponible, alquilada, vendida, en mantenimiento). Se puede filtrar por propietario para ver todo lo que tiene una persona.',
  },
  {
    titulo: 'Propietarios / Inquilinos',
    texto: 'Fichas con sus datos de contacto, y las preferencias de qué avisos quieren recibir y por qué medio (email, WhatsApp o ambos).',
  },
  {
    titulo: 'Contratos',
    texto: 'Se generan a partir de una plantilla parametrizable: la inmobiliaria arma una vez el texto legal del contrato con variables (nombre, dirección, monto, fechas...) y el sistema completa esos datos automáticamente en cada contrato nuevo, generando el PDF listo para firmar. Soporta alquiler y venta, con cláusulas propias por contrato si hace falta.',
  },
  {
    titulo: 'Cobros y pagos',
    texto: 'Cada contrato genera sus cuotas automáticamente. Al registrar un cobro, se emite el recibo en PDF y se avisa por email/WhatsApp. Si una cuota se paga tarde, el sistema calcula solo el interés por mora (usando la tasa oficial del Banco Central), sin que nadie tenga que calcular nada a mano.',
  },
  {
    titulo: 'Ajuste automático de cuotas',
    texto: 'Cuando un contrato tiene el ajuste automático activado, el valor de la cuota se actualiza solo, según el índice elegido: ICL (Índice para Contratos de Locación, del Banco Central, pensado específicamente para alquileres), UVA (Unidad de Valor Adquisitivo, también del Banco Central, sigue la inflación) o IPC (Índice de Precios al Consumidor, del INDEC, el índice de inflación oficial del país). El sistema trae esos valores automáticamente todos los días — no hay que cargar nada a mano. El ajuste se aplica solo cuando corresponde, según la periodicidad definida en el contrato (por ejemplo, cada 6 o 12 meses), calculando: nuevo valor = valor actual × (índice de hoy ÷ índice del último ajuste). Solo se actualizan las cuotas que todavía no se cobraron. Propietario e inquilino reciben el aviso automáticamente. También se puede aplicar un ajuste manual (por un % o un monto fijo) para los casos que no se manejan por índice.',
  },
  {
    titulo: 'Liquidaciones',
    texto: 'Cuando un contrato tiene la inmobiliaria a cargo de administrar los cobros, cada vez que se cobra una cuota se calcula automáticamente cuánto le corresponde transferir al propietario (el cobro menos la comisión de gestión), y queda registrado cuándo y cómo se le pagó.',
  },
  {
    titulo: 'Gastos',
    texto: 'Registro de gastos de una propiedad (reparaciones, expensas, etc.), asignados a quien corresponda pagarlos: propietario, inquilino o la inmobiliaria.',
  },
  {
    titulo: 'Tasaciones',
    texto: 'Gestión de las solicitudes de tasación que llegan, con seguimiento hasta que se resuelven.',
  },
  {
    titulo: 'Leads y Agenda',
    texto: 'Seguimiento de personas interesadas en alquilar o comprar, con historial de contacto y conversión a inquilino cuando cierran. La agenda organiza visitas y tareas pendientes.',
  },
  {
    titulo: 'Agentes',
    texto: 'Gestión del equipo de ventas/alquileres — quién atiende qué propiedades y contratos.',
  },
  {
    titulo: 'Notificaciones automáticas',
    texto: 'El sistema avisa solo por email y WhatsApp ante los eventos importantes: nuevo contrato, cobro registrado, aumento de cuota, vencimiento próximo, cambio de estado del contrato, gasto a cargo, liquidación transferida. Cada propietario/inquilino elige qué avisos quiere recibir y por qué medio.',
  },
  {
    titulo: 'Autocompletar comprobante con Inteligencia Artificial',
    texto: 'Al cargar el pago de una liquidación, en vez de tipear los datos a mano, se puede sacar una foto del comprobante de transferencia y el sistema completa el monto, la fecha y demás datos automáticamente.',
  },
  {
    titulo: 'Bot de WhatsApp',
    texto: 'Cualquier persona interesada (no hace falta que esté registrada) puede escribirle al WhatsApp de la inmobiliaria preguntando, por ejemplo, "¿hay casas en alquiler con cochera?", y el sistema responde al instante con las propiedades disponibles que coinciden — las 24 horas, sin que nadie tenga que atender.',
  },
  {
    titulo: 'Portal de autoservicio',
    texto: 'Propietarios e inquilinos pueden consultar su estado de cuenta (pagos, deuda, próximos vencimientos) por su cuenta, sin necesidad de llamar a la inmobiliaria.',
  },
  {
    titulo: 'Roles y permisos',
    texto: 'Cada persona entra con su propio usuario y contraseña, y el sistema le muestra solo lo que le corresponde según su rol: Administrador, Operador o Agente.',
  },
]
