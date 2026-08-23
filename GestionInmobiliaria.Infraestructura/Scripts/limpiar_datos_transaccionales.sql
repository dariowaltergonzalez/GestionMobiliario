/*
  Limpieza de datos de prueba — GestionInmobiliaria
  ---------------------------------------------------
  Borra las tablas TRANSACCIONALES y resetea sus IDENTITY a 0 (el próximo insert arranca en 1).

  Se CONSERVAN (no las toca este script):
    - Propiedades, FotosPropiedad
    - Propietarios
    - Inquilinos
    - Leads
    - Agentes
    - ConfiguracionEmpresa, Tenants, ClausulasContrato
    - AuditLogs, AppLogs (historial/logs)
    - Tablas de Identity/Auth (AspNetUsers, RefreshTokens, etc.)

  Se BORRAN (en orden que respeta las Foreign Keys):
    - Gastos, LiquidacionAbonos, Liquidaciones
    - PagoDetalles, Pagos
    - DocumentosContrato, AjustesContrato, Contratos
    - FotosSolicitud, SolicitudesTasacion
    - Reservas
    - EventosAgenda

  Se ACTUALIZAN (para no dejar datos derivados obsoletos en tablas que SÍ se conservan):
    - Propiedades.Estado: Contrato y Reserva son los que mueven este campo a Alquilada(2),
      Vendida(5), Reservada(6) o BoletoFirmado(7) (ver ContratoRepository.SincronizarEstadoPropiedadAsync
      y ReservaRepository). Al borrar Contratos/Reservas esos valores quedan "pegados" sin ningún
      registro real detrás, así que se resetean a Disponible(1). EnMantenimiento(3) y
      NoDisponible(4) NO se tocan porque son estados manuales, no derivados de Contrato/Reserva.

  ADVERTENCIA: operación irreversible. Correr solo en ambiente de desarrollo/pruebas.
*/

USE GestionInmobiliaria;
SET NOCOUNT ON;

BEGIN TRANSACTION;

BEGIN TRY

    -- 1) Gastos y Liquidaciones (Gastos referencia Liquidaciones con NO ACTION, hay que ir primero)
    DELETE FROM Gastos;
    DELETE FROM LiquidacionAbonos;
    DELETE FROM Liquidaciones;

    -- 2) Pagos y lo que cuelga de un Pago
    DELETE FROM PagoDetalles;
    DELETE FROM Pagos;

    -- 3) Lo que cuelga de un Contrato, y el Contrato en sí
    DELETE FROM DocumentosContrato;
    DELETE FROM AjustesContrato;
    DELETE FROM Contratos;

    -- 4) Tasaciones (con sus fotos)
    DELETE FROM FotosSolicitud;
    DELETE FROM SolicitudesTasacion;

    -- 5) Reservas
    DELETE FROM Reservas;

    -- 6) Agenda
    DELETE FROM EventosAgenda;

    -- 7) Propiedades.Estado: sacar los valores derivados de un Contrato/Reserva que ya no existe
    UPDATE Propiedades
    SET Estado = 1 -- Disponible
    WHERE Estado IN (2, 5, 6, 7); -- Alquilada, Vendida, Reservada, BoletoFirmado

    -- Resetear IDENTITY a 0 (próximo insert = 1)
    DBCC CHECKIDENT ('Gastos',              RESEED, 0);
    DBCC CHECKIDENT ('LiquidacionAbonos',   RESEED, 0);
    DBCC CHECKIDENT ('Liquidaciones',       RESEED, 0);
    DBCC CHECKIDENT ('PagoDetalles',        RESEED, 0);
    DBCC CHECKIDENT ('Pagos',               RESEED, 0);
    DBCC CHECKIDENT ('DocumentosContrato',  RESEED, 0);
    DBCC CHECKIDENT ('AjustesContrato',     RESEED, 0);
    DBCC CHECKIDENT ('Contratos',           RESEED, 0);
    DBCC CHECKIDENT ('FotosSolicitud',      RESEED, 0);
    DBCC CHECKIDENT ('SolicitudesTasacion', RESEED, 0);
    DBCC CHECKIDENT ('Reservas',            RESEED, 0);
    DBCC CHECKIDENT ('EventosAgenda',       RESEED, 0);

    COMMIT TRANSACTION;
    PRINT 'Limpieza completada correctamente.';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'Error, se hizo ROLLBACK. Nada se borró.';
    THROW;
END CATCH
