/* ===========================================
   SP: Insertar documento
   Uso: Guarda un nuevo documento subido por un estudiante.
   Guarda el nombre, tipo, ruta, fecha y usuario.
   =========================================== */
CREATE OR ALTER PROCEDURE sp_InsertarDocumento
    @IdUsuario INT,
    @Documento NVARCHAR(255),   -- nombre original del archivo
    @Tipo NVARCHAR(50),         -- extensión o tipo MIME (PDF, DOCX, JPG...)
    @RutaArchivo NVARCHAR(500)  -- ruta en el servidor
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO DocumentosTB (IdUsuario, Documento, Tipo, RutaArchivo, FechaSubida)
    VALUES (@IdUsuario, @Documento, @Tipo, @RutaArchivo, GETDATE());
END
GO


/* ===========================================
   SP: Obtener documentos por usuario
   Uso: Devuelve todos los documentos subidos por un estudiante específico.
   Se usa en la vista de "Documentos" dentro del perfil del estudiante.
   =========================================== */
CREATE OR ALTER PROCEDURE sp_ObtenerDocumentosPorUsuario
    @IdUsuario INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT IdDocumento, Documento, Tipo, RutaArchivo, FechaSubida
    FROM DocumentosTB
    WHERE IdUsuario = @IdUsuario
    ORDER BY FechaSubida DESC;
END
GO


/* ===========================================
   SP: Eliminar documento
   Uso: Permite eliminar un documento (ej. si el estudiante subió uno equivocado).
   =========================================== */
CREATE OR ALTER PROCEDURE sp_EliminarDocumento
    @IdDocumento INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM DocumentosTB WHERE IdDocumento = @IdDocumento)
    BEGIN
        DELETE FROM DocumentosTB
        WHERE IdDocumento = @IdDocumento;
    END
END
GO


/* ===========================================
   SP: Obtener un documento por Id
   Uso: Devuelve un documento específico, 
   se usa normalmente para descargar el archivo.
   =========================================== */
CREATE OR ALTER PROCEDURE sp_ObtenerDocumento
    @IdDocumento INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT IdDocumento, Documento, Tipo, RutaArchivo, FechaSubida, IdUsuario
    FROM DocumentosTB
    WHERE IdDocumento = @IdDocumento;
END
GO

--SP 28 DE OCTUBRE
USE SIGEP
GO

CREATE OR ALTER PROCEDURE IniciarPracticasSP
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE 
        @idEstadoAsignada INT,
        @idEstadoEnCurso INT,
        @idEstadoRetirada INT;

    SELECT @idEstadoAsignada = IdEstado FROM EstadosTB WHERE Descripcion = 'Asignada';
    SELECT @idEstadoEnCurso = IdEstado FROM EstadosTB WHERE Descripcion = 'En Curso';
    SELECT @idEstadoRetirada = IdEstado FROM EstadosTB WHERE Descripcion = 'Retirada';

    IF @idEstadoAsignada IS NULL OR @idEstadoEnCurso IS NULL OR @idEstadoRetirada IS NULL
    BEGIN
        RAISERROR('Faltan estados requeridos en la tabla EstadosTB.', 16, 1);
        RETURN;
    END;

    -- 1️⃣ Pasar a “En Curso” las prácticas Asignadas de estudiantes activos
    UPDATE p
    SET p.IdEstado = @idEstadoEnCurso,
        p.FechaAplicacion = GETDATE()
    FROM PracticaEstudianteTB p
    INNER JOIN UsuariosTB u ON u.IdUsuario = p.IdUsuario
    WHERE p.IdEstado = @idEstadoAsignada
      AND u.EstadoAcademico = 1;

    -- 2️⃣ Pasar a “Retirada” todas las demás prácticas con comentario automático
    DECLARE @mensaje NVARCHAR(400) = N'La práctica fue retirada automáticamente porque nunca fue asignada al iniciar el proceso general.';

    DECLARE @idPractica INT;

    DECLARE c CURSOR FOR
        SELECT IdPractica FROM PracticaEstudianteTB
        WHERE IdEstado NOT IN (@idEstadoAsignada, @idEstadoEnCurso);

    OPEN c;
    FETCH NEXT FROM c INTO @idPractica;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        UPDATE PracticaEstudianteTB
        SET IdEstado = @idEstadoRetirada,
            FechaAplicacion = GETDATE()
        WHERE IdPractica = @idPractica;

        INSERT INTO ComentariosPracticaTB (Comentario, Fecha, IdUsuario, IdPractica)
        VALUES (@mensaje, GETDATE(), 1, @idPractica);

        INSERT INTO AuditoriaGlobalTB (IdUsuario, TablaAfectada, IdRegistro, Accion, CampoAfectado, DatosAnteriores, DatosNuevos)
        VALUES (1, 'PracticaEstudianteTB', @idPractica, 'Cambio automático a Retirada', 'IdEstado', 'N/A', 'Retirada');

        FETCH NEXT FROM c INTO @idPractica;
    END;

    CLOSE c;
    DEALLOCATE c;

END;
GO

CREATE OR ALTER PROCEDURE FinalizarPracticasSP
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE 
        @idEstadoAprobada INT,
        @idEstadoRezagado INT,
        @idEstadoFinalizada INT,
        @idEstadoArchivado INT,
        @idRolEgresado INT;

    SELECT @idEstadoAprobada = IdEstado FROM EstadosTB WHERE Descripcion = 'Aprobada';
    SELECT @idEstadoRezagado = IdEstado FROM EstadosTB WHERE Descripcion = 'Rezagado';
    SELECT @idEstadoFinalizada = IdEstado FROM EstadosTB WHERE Descripcion = 'Finalizada';
    SELECT @idEstadoArchivado = IdEstado FROM EstadosTB WHERE Descripcion = 'Archivado';
    SELECT @idRolEgresado = IdRol FROM RolesTB WHERE Descripcion = 'Egresado';

    IF @idEstadoAprobada IS NULL OR @idEstadoRezagado IS NULL OR @idEstadoFinalizada IS NULL OR @idRolEgresado IS NULL
    BEGIN
        RAISERROR('Faltan estados o roles requeridos.', 16, 1);
        RETURN;
    END;

    -- 1️⃣ Finalizar prácticas en Aprobada o Rezagado
    UPDATE p
    SET p.IdEstado = @idEstadoFinalizada,
        p.FechaAplicacion = GETDATE()
    FROM PracticaEstudianteTB p
    WHERE p.IdEstado IN (@idEstadoAprobada, @idEstadoRezagado);

    -- 2️⃣ Archivar TODAS las vacantes relacionadas
    UPDATE v
    SET v.IdEstado = @idEstadoArchivado
    FROM VacantesPracticasTB v
    WHERE EXISTS (SELECT 1 FROM PracticaEstudianteTB p WHERE p.IdVacante = v.IdVacante);

    -- 3️⃣ Pasar a rol Egresado solo estudiantes aprobados (no rezagados)
    UPDATE u
    SET u.IdRol = @idRolEgresado
    FROM UsuariosTB u
    WHERE u.EstadoAcademico = 1
      AND EXISTS (
          SELECT 1 FROM PracticaEstudianteTB p
          WHERE p.IdUsuario = u.IdUsuario
            AND p.IdEstado = @idEstadoAprobada
      )
      AND NOT EXISTS (
          SELECT 1 FROM PracticaEstudianteTB p2
          WHERE p2.IdUsuario = u.IdUsuario
            AND p2.IdEstado = @idEstadoRezagado
      );

    -- 4️⃣ Auditoría
    INSERT INTO AuditoriaGlobalTB (IdUsuario, TablaAfectada, IdRegistro, Accion, CampoAfectado, DatosAnteriores, DatosNuevos)
    SELECT DISTINCT 1, 'PracticaEstudianteTB', p.IdPractica, 'Finalización de prácticas', 'IdEstado', 'Aprobada/Rezagado', 'Finalizada'
    FROM PracticaEstudianteTB p
    WHERE p.IdEstado = @idEstadoFinalizada;

END;
GO
