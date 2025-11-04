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
USE SIGEP;
GO
CREATE OR ALTER PROCEDURE IniciarPracticasSP
    @IdUsuarioCoordinador INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE 
        @idEstadoAsignada INT,
        @idEstadoEnCurso INT,
        @idEstadoRetirada INT;

    SELECT @idEstadoAsignada = IdEstado FROM EstadosTB WHERE Descripcion = 'Asignada';
    SELECT @idEstadoEnCurso   = IdEstado FROM EstadosTB WHERE Descripcion = 'En Curso';
    SELECT @idEstadoRetirada  = IdEstado FROM EstadosTB WHERE Descripcion = 'Retirada';

    IF @idEstadoAsignada IS NULL OR @idEstadoEnCurso IS NULL OR @idEstadoRetirada IS NULL
    BEGIN
        RAISERROR('Faltan estados requeridos en EstadosTB.', 16, 1);
        RETURN;
    END;

    BEGIN TRY
        BEGIN TRAN;

        --------------------------------------------------------
        -- 1️⃣ Cambiar a “En Curso” solo las prácticas asignadas de estudiantes activos
        --------------------------------------------------------
        UPDATE p
        SET p.IdEstado = @idEstadoEnCurso,
            p.FechaAplicacion = GETDATE()
        FROM PracticaEstudianteTB p
        INNER JOIN UsuariosTB u ON u.IdUsuario = p.IdUsuario
        WHERE p.IdEstado = @idEstadoAsignada
          AND u.EstadoAcademico = 1;

        --------------------------------------------------------
        -- 2️⃣ Cambiar a “Retirada” las demás prácticas (no asignadas o no en curso)
        --------------------------------------------------------
        DECLARE @mensaje NVARCHAR(400) = 
            N'La práctica fue retirada automáticamente porque nunca fue asignada al iniciar el proceso general.';

        UPDATE p
        SET p.IdEstado = @idEstadoRetirada,
            p.FechaAplicacion = GETDATE()
        FROM PracticaEstudianteTB p
        WHERE p.IdEstado NOT IN (@idEstadoAsignada, @idEstadoEnCurso);

        --------------------------------------------------------
        -- 3️⃣ Insertar comentario automático para las retiradas
        --------------------------------------------------------
        INSERT INTO ComentariosPracticaTB (Comentario, Fecha, IdUsuario, IdPractica)
        SELECT @mensaje, GETDATE(), @IdUsuarioCoordinador, p.IdPractica
        FROM PracticaEstudianteTB p
        WHERE p.IdEstado = @idEstadoRetirada;

        --------------------------------------------------------
        -- 4️⃣ Auditoría: Cambio a Retirada
        --------------------------------------------------------
        INSERT INTO AuditoriaGlobalTB 
            (IdUsuario, TablaAfectada, IdRegistro, Accion, CampoAfectado, DatosAnteriores, DatosNuevos)
        SELECT DISTINCT 
            @IdUsuarioCoordinador, 
            'PracticaEstudianteTB', 
            p.IdPractica,
            'Cambio automático a Retirada', 
            'IdEstado',
            CONCAT('Estudiante: ', u.Nombre, ' ', u.Apellido1, ' ', u.Apellido2,
                   ' | Cédula: ', u.Cedula,
                   ' | Estado anterior: Asignada',
                   ' | Empresa: ', ISNULL(emp.NombreEmpresa, 'Sin empresa')),
            'Retirada'
        FROM PracticaEstudianteTB p
        INNER JOIN UsuariosTB u ON u.IdUsuario = p.IdUsuario
        LEFT JOIN VacantesPracticasTB v ON v.IdVacante = p.IdVacante
        LEFT JOIN EmpresasTB emp ON emp.IdEmpresa = v.IdEmpresa
        WHERE p.IdEstado = @idEstadoRetirada;

        --------------------------------------------------------
        -- 5️⃣ Auditoría: Cambio a En Curso
        --------------------------------------------------------
        INSERT INTO AuditoriaGlobalTB 
            (IdUsuario, TablaAfectada, IdRegistro, Accion, CampoAfectado, DatosAnteriores, DatosNuevos)
        SELECT DISTINCT 
            @IdUsuarioCoordinador, 
            'PracticaEstudianteTB', 
            p.IdPractica,
            'Cambio automático a En Curso', 
            'IdEstado',
            CONCAT('Estudiante: ', u.Nombre, ' ', u.Apellido1, ' ', u.Apellido2,
                   ' | Cédula: ', u.Cedula,
                   ' | Estado anterior: Asignada',
                   ' | Empresa: ', ISNULL(emp.NombreEmpresa, 'Sin empresa')),
            'En Curso'
        FROM PracticaEstudianteTB p
        INNER JOIN UsuariosTB u ON u.IdUsuario = p.IdUsuario
        LEFT JOIN VacantesPracticasTB v ON v.IdVacante = p.IdVacante
        LEFT JOIN EmpresasTB emp ON emp.IdEmpresa = v.IdEmpresa
        WHERE p.IdEstado = @idEstadoEnCurso;

        COMMIT TRAN;
    END TRY
    BEGIN CATCH
        ROLLBACK TRAN;
        THROW;
    END CATCH
END;
GO



USE SIGEP;
GO
CREATE OR ALTER PROCEDURE FinalizarPracticasSP
    @IdUsuarioCoordinador INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE 
        @idEstadoAprobada   INT,
        @idEstadoRezagado   INT,
        @idEstadoFinalizada INT,
        @idEstadoArchivado  INT,
        @idRolEgresado      INT;

    SELECT @idEstadoAprobada   = IdEstado FROM EstadosTB WHERE Descripcion = 'Aprobada';
    SELECT @idEstadoRezagado   = IdEstado FROM EstadosTB WHERE Descripcion = 'Rezagado';
    SELECT @idEstadoFinalizada = IdEstado FROM EstadosTB WHERE Descripcion = 'Finalizada';
    SELECT @idEstadoArchivado  = IdEstado FROM EstadosTB WHERE Descripcion = 'Archivado';
    SELECT @idRolEgresado      = IdRol    FROM RolesTB   WHERE Descripcion = 'Egresado';

    IF @idEstadoAprobada IS NULL OR @idEstadoRezagado IS NULL 
       OR @idEstadoFinalizada IS NULL OR @idEstadoArchivado IS NULL 
       OR @idRolEgresado IS NULL
    BEGIN
        RAISERROR('Faltan estados o roles requeridos. Verifique EstadosTB y RolesTB.', 16, 1);
        RETURN;
    END;

    BEGIN TRY
        BEGIN TRAN;

        --------------------------------------------------------
        -- 1️⃣ Cambiar prácticas en Aprobada o Rezagado → Finalizada
        --------------------------------------------------------
        UPDATE p
        SET p.IdEstado = @idEstadoFinalizada,
            p.FechaAplicacion = GETDATE()
        FROM PracticaEstudianteTB p
        WHERE p.IdEstado IN (@idEstadoAprobada, @idEstadoRezagado);

        --------------------------------------------------------
        -- 2️⃣ Archivar vacantes que ya tuvieron prácticas
        --------------------------------------------------------
        UPDATE v
        SET v.IdEstado = @idEstadoArchivado
        FROM VacantesPracticasTB v
        WHERE EXISTS (
            SELECT 1 FROM PracticaEstudianteTB p WHERE p.IdVacante = v.IdVacante
        );

        --------------------------------------------------------
        -- 3️⃣ Pasar estudiantes aprobados a Rol Egresado
        --     Solo si tienen práctica aprobada finalizada y no están rezagados
        --------------------------------------------------------
        UPDATE u
        SET u.IdRol = @idRolEgresado
        FROM UsuariosTB u
        WHERE 
            u.EstadoAcademico = 1
            AND EXISTS (
                SELECT 1 
                FROM PracticaEstudianteTB p
                WHERE p.IdUsuario = u.IdUsuario
                  AND p.IdEstado = @idEstadoFinalizada
                  AND EXISTS (
                      SELECT 1
                      FROM PracticaEstudianteTB p2
                      WHERE p2.IdUsuario = u.IdUsuario
                        AND p2.IdEstado = @idEstadoAprobada
                  )
            )
            AND NOT EXISTS (
                SELECT 1 
                FROM PracticaEstudianteTB p3
                WHERE p3.IdUsuario = u.IdUsuario
                  AND p3.IdEstado = @idEstadoRezagado
            );

        --------------------------------------------------------
        -- 4️⃣ Auditoría de finalización
        --------------------------------------------------------
        INSERT INTO AuditoriaGlobalTB 
            (IdUsuario, TablaAfectada, IdRegistro, Accion, CampoAfectado, DatosAnteriores, DatosNuevos)
        SELECT DISTINCT 
            @IdUsuarioCoordinador, 
            'PracticaEstudianteTB', 
            p.IdPractica,
            'Finalización de prácticas', 
            'IdEstado',
            CONCAT(
                'Estudiante: ', u.Nombre, ' ', u.Apellido1, ' ', u.Apellido2,
                ' | Cédula: ', u.Cedula,
                ' | Estado anterior: ', eAnt.Descripcion,
                ' | Empresa: ', ISNULL(emp.NombreEmpresa, 'Sin empresa')
            ),
            'Finalizada'
        FROM PracticaEstudianteTB p
        INNER JOIN UsuariosTB u ON u.IdUsuario = p.IdUsuario
        LEFT JOIN VacantesPracticasTB v ON v.IdVacante = p.IdVacante
        LEFT JOIN EmpresasTB emp ON emp.IdEmpresa = v.IdEmpresa
        LEFT JOIN EstadosTB eAnt ON eAnt.IdEstado IN (@idEstadoAprobada, @idEstadoRezagado)
        WHERE p.IdEstado = @idEstadoFinalizada;

        --------------------------------------------------------
        -- 5️⃣ Auditoría de cambio de rol
        --------------------------------------------------------
        INSERT INTO AuditoriaGlobalTB 
            (IdUsuario, TablaAfectada, IdRegistro, Accion, CampoAfectado, DatosAnteriores, DatosNuevos)
        SELECT DISTINCT 
            @IdUsuarioCoordinador, 
            'UsuariosTB', 
            u.IdUsuario,
            'Cambio de rol por finalización', 
            'IdRol',
            CONCAT('Estudiante: ', u.Nombre, ' ', u.Apellido1, ' ', u.Apellido2, 
                   ' | Cédula: ', u.Cedula),
            'Egresado'
        FROM UsuariosTB u
        WHERE u.IdRol = @idRolEgresado;

        COMMIT TRAN;
    END TRY
    BEGIN CATCH
        ROLLBACK TRAN;
        THROW;
    END CATCH
END;
GO
