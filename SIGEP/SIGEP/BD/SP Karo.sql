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


--11-11-25
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
        @idRolEgresado      INT,
        @idRolEstudiante    INT;

    -- Obtener IDs de estados y roles
    SELECT @idEstadoAprobada   = IdEstado FROM EstadosTB WHERE Descripcion = 'Aprobada';
    SELECT @idEstadoRezagado   = IdEstado FROM EstadosTB WHERE Descripcion = 'Rezagado';
    SELECT @idEstadoFinalizada = IdEstado FROM EstadosTB WHERE Descripcion = 'Finalizada';
    SELECT @idEstadoArchivado  = IdEstado FROM EstadosTB WHERE Descripcion = 'Archivado';
    SELECT @idRolEgresado      = IdRol FROM RolesTB WHERE Descripcion = 'Egresado';
    SELECT @idRolEstudiante    = IdRol FROM RolesTB WHERE Descripcion = 'Estudiante';

    IF @idEstadoAprobada IS NULL OR @idEstadoRezagado IS NULL 
       OR @idEstadoFinalizada IS NULL OR @idEstadoArchivado IS NULL 
       OR @idRolEgresado IS NULL OR @idRolEstudiante IS NULL
    BEGIN
        RAISERROR('Faltan estados o roles requeridos. Verifique EstadosTB y RolesTB.', 16, 1);
        RETURN;
    END;

    BEGIN TRY
        BEGIN TRAN;

        --------------------------------------------------------
        -- Solo las prácticas APROBADAS → Finalizada
        --    Las REZAGADAS se mantienen como Rezagado
        --------------------------------------------------------
        UPDATE p
        SET p.IdEstado = @idEstadoFinalizada,
            p.FechaAplicacion = GETDATE()
        FROM PracticaEstudianteTB p
        WHERE p.IdEstado = @idEstadoAprobada;

        --------------------------------------------------------
        -- 2️ TODAS las vacantes → Archivado (tengan o no prácticas)
        --------------------------------------------------------
        UPDATE VacantesPracticasTB
        SET IdEstado = @idEstadoArchivado
        WHERE IdEstado != @idEstadoArchivado;

        --------------------------------------------------------
        -- 3️ Estudiantes con práctica FINALIZADA (aprobados) → Egresado
        --------------------------------------------------------
        UPDATE u
        SET u.IdRol = @idRolEgresado
        FROM UsuariosTB u
        WHERE u.EstadoAcademico = 1
          AND EXISTS (
              SELECT 1 
              FROM PracticaEstudianteTB p
              WHERE p.IdUsuario = u.IdUsuario
                AND p.IdEstado = @idEstadoFinalizada
          )
          -- Verificar que NO tenga prácticas rezagadas
          AND NOT EXISTS (
              SELECT 1 
              FROM PracticaEstudianteTB p2
              WHERE p2.IdUsuario = u.IdUsuario
                AND p2.IdEstado = @idEstadoRezagado
          );

        --------------------------------------------------------
        -- 4️ Estudiantes con práctica REZAGADA → Mantener como Estudiante
        --------------------------------------------------------
        UPDATE u
        SET u.IdRol = @idRolEstudiante,
            u.EstadoAcademico = 1  -- Activos para reasignación
        FROM UsuariosTB u
        WHERE EXISTS (
            SELECT 1 
            FROM PracticaEstudianteTB p
            WHERE p.IdUsuario = u.IdUsuario
              AND p.IdEstado = @idEstadoRezagado
        )
        AND u.IdRol != @idRolEgresado;

        --------------------------------------------------------
        -- 5️⃣ Auditoría de prácticas finalizadas (aprobadas)
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
                ' | Estado anterior: Aprobada',
                ' | Empresa: ', ISNULL(emp.NombreEmpresa, 'Sin empresa')
            ),
            'Finalizada'
        FROM PracticaEstudianteTB p
        INNER JOIN UsuariosTB u ON u.IdUsuario = p.IdUsuario
        LEFT JOIN VacantesPracticasTB v ON v.IdVacante = p.IdVacante
        LEFT JOIN EmpresasTB emp ON emp.IdEmpresa = v.IdEmpresa
        WHERE p.IdEstado = @idEstadoFinalizada;

        --------------------------------------------------------
        -- 6️ Auditoría de vacantes archivadas
        --------------------------------------------------------
        INSERT INTO AuditoriaGlobalTB 
            (IdUsuario, TablaAfectada, IdRegistro, Accion, CampoAfectado, DatosAnteriores, DatosNuevos)
        SELECT DISTINCT 
            @IdUsuarioCoordinador, 
            'VacantesPracticasTB', 
            v.IdVacante,
            'Archivado de vacantes', 
            'IdEstado',
            CONCAT(
                'Vacante: ', v.Nombre,
                ' | Empresa: ', emp.NombreEmpresa,
                ' | Cupos: ', v.NumCupos
            ),
            'Archivado'
        FROM VacantesPracticasTB v
        INNER JOIN EmpresasTB emp ON emp.IdEmpresa = v.IdEmpresa
        WHERE v.IdEstado = @idEstadoArchivado;

        --------------------------------------------------------
        -- 7️ Auditoría de cambio de rol a Egresado
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
                   ' | Cédula: ', u.Cedula,
                   ' | Resultado: Aprobado'),
            'Egresado'
        FROM UsuariosTB u
        WHERE u.IdRol = @idRolEgresado
          AND EXISTS (
              SELECT 1 FROM PracticaEstudianteTB p 
              WHERE p.IdUsuario = u.IdUsuario 
                AND p.IdEstado = @idEstadoFinalizada
          );

        --------------------------------------------------------
        -- 8️ Auditoría de estudiantes rezagados
        --------------------------------------------------------
        INSERT INTO AuditoriaGlobalTB 
            (IdUsuario, TablaAfectada, IdRegistro, Accion, CampoAfectado, DatosAnteriores, DatosNuevos)
        SELECT DISTINCT 
            @IdUsuarioCoordinador, 
            'UsuariosTB', 
            u.IdUsuario,
            'Mantener como estudiante por rezago', 
            'IdRol',
            CONCAT('Estudiante: ', u.Nombre, ' ', u.Apellido1, ' ', u.Apellido2, 
                   ' | Cédula: ', u.Cedula,
                   ' | Resultado: Rezagado - debe repetir'),
            'Estudiante'
        FROM UsuariosTB u
        WHERE u.IdRol = @idRolEstudiante
          AND EXISTS (
              SELECT 1 FROM PracticaEstudianteTB p 
              WHERE p.IdUsuario = u.IdUsuario 
                AND p.IdEstado = @idEstadoRezagado
          );

        COMMIT TRAN;
    END TRY
    BEGIN CATCH
        ROLLBACK TRAN;
        THROW;
    END CATCH
END;
GO


CREATE OR ALTER PROCEDURE [dbo].[ObtenerVacantesAsignarSP]
    @IdUsuario INT
AS
BEGIN
    SET NOCOUNT ON;

    
    DECLARE @EspecialidadesEst TABLE (IdEspecialidad INT);
    INSERT INTO @EspecialidadesEst (IdEspecialidad)
    SELECT DISTINCT IdEspecialidad
    FROM UsuarioEspecialidadTB
    WHERE IdUsuario = @IdUsuario
      AND IdEstado = 1;

    
    DECLARE @EstadosOcupados TABLE (IdEstado INT);
    INSERT INTO @EstadosOcupados (IdEstado)
    SELECT IdEstado
    FROM EstadosTB
    WHERE LOWER(LTRIM(RTRIM(Descripcion))) IN (
        'asignada','en curso','aprobada','finalizada','rezagado'
    );

   
    SELECT
        v.IdVacante,
        LTRIM(RTRIM(v.Nombre)) AS NombreVacante,
        emp.NombreEmpresa,

       
        ISNULL((
            SELECT TOP 1 esp.Nombre
            FROM EspecialidadesVacantesTB ev
            INNER JOIN EspecialidadesTB esp ON esp.IdEspecialidad = ev.IdEspecialidad
            WHERE ev.IdVacante = v.IdVacante
              AND ev.IdEspecialidad IN (SELECT IdEspecialidad FROM @EspecialidadesEst)
        ), '—') AS Especialidad,

        v.NumCupos,

      
        (
            SELECT COUNT(*)
            FROM PracticaEstudianteTB p
            WHERE p.IdVacante = v.IdVacante
              AND p.IdEstado IN (SELECT IdEstado FROM @EstadosOcupados)
        ) AS CuposOcupados,

        v.FechaCierre,
        v.Requerimientos,
        v.Tipo,

       
        CASE 
            WHEN v.Tipo IS NOT NULL AND LOWER(LTRIM(RTRIM(v.Tipo))) = 'autogestionada'
                 AND EXISTS (
                     SELECT 1 
                     FROM PracticaEstudianteTB p
                     WHERE p.IdUsuario = @IdUsuario
                       AND p.IdVacante = v.IdVacante
                       AND p.IdEstado IN (3,5,6,8,9,11)
                 )
            THEN 'Autogestionada'
            ELSE NULL
        END AS TipoMensaje,

       
        ISNULL((
            SELECT TOP 1 LTRIM(RTRIM(e2.Descripcion))
            FROM PracticaEstudianteTB p2
            INNER JOIN EstadosTB e2 ON e2.IdEstado = p2.IdEstado
            WHERE p2.IdUsuario = @IdUsuario
              AND p2.IdVacante = v.IdVacante
            ORDER BY p2.IdPractica DESC
        ), 'Sin proceso activo') AS EstadoPractica,

        
        ISNULL((
            SELECT TOP 1 p3.IdPractica
            FROM PracticaEstudianteTB p3
            WHERE p3.IdUsuario = @IdUsuario
              AND p3.IdVacante = v.IdVacante
            ORDER BY p3.IdPractica DESC
        ), 0) AS IdPracticaVacante,

       
        CASE 
            WHEN EXISTS (
                SELECT 1 
                FROM PracticaEstudianteTB p4
                INNER JOIN EstadosTB e4 ON e4.IdEstado = p4.IdEstado
                WHERE p4.IdUsuario = @IdUsuario
                  AND p4.IdVacante <> v.IdVacante
                  AND LOWER(LTRIM(RTRIM(e4.Descripcion))) IN (
                      'en curso','asignada','aprobada','finalizada','rezagado'
                  )
            ) THEN 0  
            ELSE 1    
        END AS PuedeAsignar,

     
        (SELECT CONCAT(u.Nombre, ' ', u.Apellido1, ' ', u.Apellido2)
         FROM UsuariosTB u WHERE u.IdUsuario = @IdUsuario) AS NombreCompleto,

      
        CASE WHEN (SELECT EstadoAcademico FROM UsuariosTB WHERE IdUsuario = @IdUsuario) = 1
            THEN 'Activo' ELSE 'Inactivo' END AS EstadoAcademicoDescripcion

    FROM VacantesPracticasTB v
    INNER JOIN EmpresasTB emp ON emp.IdEmpresa = v.IdEmpresa

    WHERE 
    (
        v.IdEstado IN (1, 5)
        AND EXISTS (
            SELECT 1
            FROM EspecialidadesVacantesTB ev
            WHERE ev.IdVacante = v.IdVacante
              AND ev.IdEspecialidad IN (SELECT IdEspecialidad FROM @EspecialidadesEst)
        )
    )
    OR 
    (
        EXISTS (
            SELECT 1
            FROM PracticaEstudianteTB p
            WHERE p.IdUsuario = @IdUsuario
              AND p.IdVacante = v.IdVacante
              AND p.IdEstado IN (3,5,6,8,9,11)
        )
    )

    ORDER BY v.Nombre;
END;
GO


USE SIGEP;
GO

CREATE OR ALTER PROCEDURE [dbo].[ObtenerEstudiantesAsignarSP]
    @IdVacante INT,
    @IdUsuarioSesion INT
AS
BEGIN
    SET NOCOUNT ON;

   
    DECLARE @IdRol INT;
    SELECT @IdRol = IdRol
    FROM UsuariosTB
    WHERE IdUsuario = @IdUsuarioSesion;

   
    DECLARE @EstadosActivos TABLE (Descripcion NVARCHAR(100));
    INSERT INTO @EstadosActivos VALUES
        ('asignada'), ('en curso'), ('aprobada'), ('finalizada'), ('rezagado');

   
    DECLARE @EspecialidadesVacante TABLE (IdEspecialidad INT);
    INSERT INTO @EspecialidadesVacante (IdEspecialidad)
    SELECT IdEspecialidad
    FROM EspecialidadesVacantesTB
    WHERE IdVacante = @IdVacante;

   
    DECLARE @EspecialidadesProfesor TABLE (IdEspecialidad INT);
    IF @IdRol = 3
    BEGIN
        INSERT INTO @EspecialidadesProfesor (IdEspecialidad)
        SELECT IdEspecialidad
        FROM UsuarioEspecialidadTB
        WHERE IdUsuario = @IdUsuarioSesion
          AND IdEstado = 1;
    END;

   
    ;WITH Candidatos AS (
        SELECT DISTINCT u.IdUsuario
        FROM UsuariosTB u
        INNER JOIN RolesTB r ON r.IdRol = u.IdRol
       
        WHERE LOWER(LTRIM(RTRIM(r.Descripcion))) = 'estudiante'
          AND u.EstadoAcademico = 1
          AND (
                
                @IdRol IN (1,2,4)
                OR
                
                (
                    @IdRol = 3
                    AND EXISTS (
                        SELECT 1
                        FROM UsuarioEspecialidadTB ueProf
                        WHERE ueProf.IdUsuario = u.IdUsuario
                          AND ueProf.IdEstado = 1
                          AND ueProf.IdEspecialidad IN (SELECT IdEspecialidad FROM @EspecialidadesProfesor)
                    )
                )
              )
         
          AND EXISTS (
              SELECT 1
              FROM UsuarioEspecialidadTB ueVac
              WHERE ueVac.IdUsuario = u.IdUsuario
                AND ueVac.IdEstado = 1
                AND ueVac.IdEspecialidad IN (SELECT IdEspecialidad FROM @EspecialidadesVacante)
          )
    )


    , EspecialidadesPorUsuario AS (
        SELECT 
            ue.IdUsuario,
            STRING_AGG(esp.Nombre, ', ') AS Especialidades
        FROM UsuarioEspecialidadTB ue
        INNER JOIN EspecialidadesTB esp ON esp.IdEspecialidad = ue.IdEspecialidad
        WHERE ue.IdEstado = 1
        GROUP BY ue.IdUsuario
    )

  
    SELECT
        u.IdUsuario,
        u.Cedula,
        CONCAT(u.Nombre, ' ', u.Apellido1, ' ', u.Apellido2) AS NombreCompleto,
        ISNULL(epu.Especialidades, '—') AS Especialidad,
        u.EstadoAcademico,

       
        ISNULL((
            SELECT TOP 1 LTRIM(RTRIM(e.Descripcion))
            FROM PracticaEstudianteTB p
            INNER JOIN EstadosTB e ON e.IdEstado = p.IdEstado
            WHERE p.IdUsuario = u.IdUsuario
            ORDER BY p.IdPractica DESC
        ), 'Sin proceso activo') AS EstadoPractica,

      
        ISNULL((
            SELECT TOP 1 LTRIM(RTRIM(e2.Descripcion))
            FROM PracticaEstudianteTB p2
            INNER JOIN EstadosTB e2 ON e2.IdEstado = p2.IdEstado
            WHERE p2.IdUsuario = u.IdUsuario
              AND p2.IdVacante = @IdVacante
            ORDER BY p2.IdPractica DESC
        ), 'Sin proceso activo') AS EstadoVacante,

    
        ISNULL((
            SELECT TOP 1 p3.IdPractica
            FROM PracticaEstudianteTB p3
            WHERE p3.IdUsuario = u.IdUsuario
              AND p3.IdVacante = @IdVacante
            ORDER BY p3.IdPractica DESC
        ), 0) AS IdPracticaVacante,

     
        CAST(
            CASE WHEN EXISTS (
                SELECT 1 FROM PracticaEstudianteTB p4
                WHERE p4.IdVacante = @IdVacante
                  AND p4.IdUsuario = u.IdUsuario
            ) THEN 1 ELSE 0 END AS BIT
        ) AS TieneRelacionEnVacante,

       
        CAST(
            CASE WHEN EXISTS (
                SELECT 1
                FROM PracticaEstudianteTB p5
                INNER JOIN EstadosTB e5 ON e5.IdEstado = p5.IdEstado
                WHERE p5.IdUsuario = u.IdUsuario
                  AND LOWER(LTRIM(RTRIM(e5.Descripcion))) IN (SELECT Descripcion FROM @EstadosActivos)
            ) THEN 1 ELSE 0 END AS BIT
        ) AS TienePracticaActiva

    FROM Candidatos c
    INNER JOIN UsuariosTB u ON u.IdUsuario = c.IdUsuario
    LEFT JOIN EspecialidadesPorUsuario epu ON epu.IdUsuario = u.IdUsuario

    ORDER BY NombreCompleto;

END;
GO

USE SIGEP;
GO

CREATE OR ALTER PROCEDURE [dbo].[ObtenerEstudiantesPracticasSP]
    @IdUsuarioSesion INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IdRol INT;
    SELECT @IdRol = IdRol
    FROM UsuariosTB
    WHERE IdUsuario = @IdUsuarioSesion;

    DECLARE @EspecialidadesProfesor TABLE (IdEspecialidad INT);
    IF @IdRol = 3
    BEGIN
        INSERT INTO @EspecialidadesProfesor (IdEspecialidad)
        SELECT IdEspecialidad
        FROM UsuarioEspecialidadTB
        WHERE IdUsuario = @IdUsuarioSesion
          AND IdEstado = 1;
    END;

   
    DECLARE @EstadosActivos TABLE (Descripcion NVARCHAR(100));
    INSERT INTO @EstadosActivos VALUES
        ('en curso'), ('asignada'), ('aprobada'), ('en proceso de aplicacion');

   
    SELECT
        u.IdUsuario,
        u.Cedula,
        CONCAT(u.Nombre, ' ', u.Apellido1, ' ', u.Apellido2) AS NombreCompleto,
        ISNULL(es.Especialidades, '—') AS Especialidad,
        u.EstadoAcademico,

        CASE 
            WHEN EXISTS (
                SELECT 1
                FROM PracticaEstudianteTB p
                INNER JOIN EstadosTB e ON e.IdEstado = p.IdEstado
                WHERE p.IdUsuario = u.IdUsuario
                  AND LOWER(LTRIM(RTRIM(e.Descripcion))) IN (SELECT Descripcion FROM @EstadosActivos)
            ) THEN 'Con Procesos Activos'
            ELSE 'Sin Procesos Activos'
        END AS EstadoPostulacion,

        ISNULL((
            SELECT TOP 1 LTRIM(RTRIM(e2.Descripcion))
            FROM PracticaEstudianteTB p2
            INNER JOIN EstadosTB e2 ON e2.IdEstado = p2.IdEstado
            WHERE p2.IdUsuario = u.IdUsuario
            ORDER BY p2.IdPractica DESC
        ), 'Sin proceso activo') AS EstadoPractica,

        ISNULL((
            SELECT TOP 1 v.Nombre
            FROM PracticaEstudianteTB p3
            INNER JOIN VacantesPracticasTB v ON v.IdVacante = p3.IdVacante
            WHERE p3.IdUsuario = u.IdUsuario
            ORDER BY p3.IdPractica DESC
        ), '—') AS Vacante,

        ISNULL((
            SELECT TOP 1 emp.NombreEmpresa
            FROM PracticaEstudianteTB p4
            INNER JOIN VacantesPracticasTB v2 ON v2.IdVacante = p4.IdVacante
            INNER JOIN EmpresasTB emp ON emp.IdEmpresa = v2.IdEmpresa
            WHERE p4.IdUsuario = u.IdUsuario
            ORDER BY p4.IdPractica DESC
        ), '—') AS Empresa,

        ISNULL((
            SELECT TOP 1 v3.Tipo
            FROM PracticaEstudianteTB p5
            INNER JOIN VacantesPracticasTB v3 ON v3.IdVacante = p5.IdVacante
            WHERE p5.IdUsuario = u.IdUsuario
            ORDER BY p5.IdPractica DESC
        ), '—') AS Tipo,

		CASE 
    WHEN (
        SELECT TOP 1 LOWER(LTRIM(RTRIM(v4.Tipo)))
        FROM PracticaEstudianteTB p8
        INNER JOIN VacantesPracticasTB v4 ON v4.IdVacante = p8.IdVacante
        WHERE p8.IdUsuario = u.IdUsuario
        ORDER BY p8.IdPractica DESC
    ) = 'autogestionada'
    THEN 'Autogestionada'
    ELSE NULL
END AS TipoMensaje,

        ISNULL((
            SELECT TOP 1 p6.IdVacante
            FROM PracticaEstudianteTB p6
            WHERE p6.IdUsuario = u.IdUsuario
            ORDER BY p6.IdPractica DESC
        ), 0) AS IdVacanteUltima,

        ISNULL((
            SELECT TOP 1 p7.IdPractica
            FROM PracticaEstudianteTB p7
            WHERE p7.IdUsuario = u.IdUsuario
            ORDER BY p7.IdPractica DESC
        ), 0) AS IdPracticaVacante

    FROM UsuariosTB u
    INNER JOIN RolesTB r ON r.IdRol = u.IdRol
    LEFT JOIN (
        SELECT 
            ue.IdUsuario,
            STRING_AGG(esp.Nombre, ', ') AS Especialidades
        FROM UsuarioEspecialidadTB ue
        INNER JOIN EspecialidadesTB esp ON esp.IdEspecialidad = ue.IdEspecialidad
        WHERE ue.IdEstado = 1
        GROUP BY ue.IdUsuario
    ) AS es ON es.IdUsuario = u.IdUsuario

    WHERE 
        LOWER(LTRIM(RTRIM(r.Descripcion))) = 'estudiante'
        AND u.EstadoAcademico = 1
        AND (
            @IdRol IN (1, 2, 4) -- Admin / Coordinador / Egresado
            OR (
                @IdRol = 3         -- Profesor: solo sus especialidades
                AND EXISTS (
                    SELECT 1
                    FROM UsuarioEspecialidadTB ueProf
                    WHERE ueProf.IdUsuario = u.IdUsuario
                      AND ueProf.IdEstado = 1
                      AND ueProf.IdEspecialidad IN (SELECT IdEspecialidad FROM @EspecialidadesProfesor)
                )
            )
        )
        AND EXISTS (
            SELECT 1 FROM PracticaEstudianteTB pe WHERE pe.IdUsuario = u.IdUsuario
        )
    ORDER BY NombreCompleto;
END;
GO


--11-11-25
USE SIGEP;
GO

CREATE OR ALTER PROCEDURE [dbo].[AsignarEstudianteSP]
    @IdVacante INT,
    @IdUsuario INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @NumCupos INT, @Ocupados INT, @IdEstadoEnProceso INT, @IdEstadoAsignada INT, @IdEstadoRetirada INT;
   
    SELECT @NumCupos = NumCupos
    FROM VacantesPracticasTB
    WHERE IdVacante = @IdVacante;

    IF @NumCupos IS NULL
    BEGIN
        SELECT 0 AS ok, 'No se encontró la vacante seleccionada.' AS message;
        RETURN;
    END;
  
    SELECT @Ocupados = COUNT(*)
    FROM PracticaEstudianteTB
    WHERE IdVacante = @IdVacante
      AND IdEstado IN (5, 6, 8, 9, 11); 

    IF @Ocupados >= @NumCupos
    BEGIN
        SELECT 0 AS ok, CONCAT('No es posible asignar más estudiantes. Todos los cupos (', @NumCupos, ') ya están ocupados.') AS message;
        RETURN;
    END;

   
    IF EXISTS (
        SELECT 1
        FROM PracticaEstudianteTB p
        INNER JOIN EstadosTB e ON e.IdEstado = p.IdEstado
        WHERE p.IdUsuario = @IdUsuario
          AND p.IdVacante <> @IdVacante
          AND LOWER(LTRIM(RTRIM(e.Descripcion))) IN ('asignada','aprobada','en curso')
          
    )
    BEGIN
        SELECT 0 AS ok, 'El estudiante ya tiene una práctica activa en otra vacante.' AS message;
        RETURN;
    END;
   
    SELECT @IdEstadoEnProceso = IdEstado
    FROM EstadosTB WHERE LOWER(LTRIM(RTRIM(Descripcion))) = 'en proceso de aplicacion';

    SELECT @IdEstadoAsignada = IdEstado
    FROM EstadosTB WHERE LOWER(LTRIM(RTRIM(Descripcion))) = 'asignada';

    SELECT @IdEstadoRetirada = IdEstado
    FROM EstadosTB WHERE LOWER(LTRIM(RTRIM(Descripcion))) = 'retirada';

    IF @IdEstadoEnProceso IS NULL OR @IdEstadoAsignada IS NULL OR @IdEstadoRetirada IS NULL
    BEGIN
        SELECT 0 AS ok, 'No se encontraron los estados requeridos en EstadosTB.' AS message;
        RETURN;
    END;
    
    DECLARE @IdPractica INT, @IdEstadoActual INT;
    SELECT TOP 1 
        @IdPractica = IdPractica,
        @IdEstadoActual = IdEstado
    FROM PracticaEstudianteTB
    WHERE IdUsuario = @IdUsuario
      AND IdVacante = @IdVacante
    ORDER BY IdPractica DESC;

    DECLARE @EstadoActual NVARCHAR(100);
    SELECT @EstadoActual = LOWER(LTRIM(RTRIM(Descripcion))) 
    FROM EstadosTB 
    WHERE IdEstado = @IdEstadoActual;

    -- CASO 1: No tiene práctica previa en esta vacante
    IF @IdPractica IS NULL
    BEGIN
        INSERT INTO PracticaEstudianteTB (IdVacante, IdUsuario, IdEstado, FechaAplicacion)
        VALUES (@IdVacante, @IdUsuario, @IdEstadoEnProceso, GETDATE());

        SELECT 1 AS ok, 'Estudiante agregado en estado "En proceso de Aplicación".' AS message;
        RETURN;
    END;
   
    -- CASO 2: Reactivar si está retirada
    IF @EstadoActual = 'retirada'
    BEGIN
        UPDATE PracticaEstudianteTB
        SET IdEstado = @IdEstadoEnProceso,
            FechaAplicacion = GETDATE()
        WHERE IdPractica = @IdPractica;

        SELECT 1 AS ok, 'Estudiante reactivado en estado "En proceso de Aplicación".' AS message;
        RETURN;
    END;
    
    -- CASO 3: Avanzar de "en proceso" a "asignada"
    IF @EstadoActual = 'en proceso de aplicacion'
    BEGIN
        UPDATE PracticaEstudianteTB
        SET IdEstado = @IdEstadoAsignada,
            FechaAplicacion = GETDATE()
        WHERE IdPractica = @IdPractica;

        SELECT 1 AS ok, 'Estado actualizado a "Asignada".' AS message;
        RETURN;
    END;
    
    -- CASO 4: Ya está asignada
    IF @EstadoActual = 'asignada'
    BEGIN
        SELECT 0 AS ok, 'El estudiante ya está asignado en esta vacante.' AS message;
        RETURN;
    END;
    
   
    IF @EstadoActual IN ('finalizada','rezagado')
    BEGIN
        
        INSERT INTO PracticaEstudianteTB (IdVacante, IdUsuario, IdEstado, FechaAplicacion)
        VALUES (@IdVacante, @IdUsuario, @IdEstadoEnProceso, GETDATE());

        SELECT 1 AS ok, 'Nueva práctica creada en estado "En proceso de Aplicación". La práctica anterior se mantiene como historial.' AS message;
        RETURN;
    END;

    -- CASO 5: Bloquear solo si está en curso o aprobada 
    IF @EstadoActual IN ('aprobada','en curso')
    BEGIN
        SELECT 0 AS ok, CONCAT('No se puede reasignar porque la práctica está en estado "', @EstadoActual, '".') AS message;
        RETURN;
    END;
  
    
    UPDATE PracticaEstudianteTB
    SET IdEstado = @IdEstadoEnProceso,
        FechaAplicacion = GETDATE()
    WHERE IdPractica = @IdPractica;

    SELECT 1 AS ok, 'Estudiante agregado en estado "En proceso de Aplicación".' AS message;
END;
GO

--25-11-25
USE SIGEP;
GO

CREATE OR ALTER PROCEDURE ObtenerEmpresasActivasSP
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @idEstadoActivo INT;

    -- Obtener el ID del estado "Activo"
    SELECT @idEstadoActivo = IdEstado 
    FROM EstadosTB 
    WHERE LOWER(LTRIM(RTRIM(Descripcion))) = 'activo';

    -- Si no existe, usar 1 como fallback
    IF @idEstadoActivo IS NULL
        SET @idEstadoActivo = 1;

    -- Retornar solo empresas activas
    SELECT 
        IdEmpresa,
        NombreEmpresa
    FROM EmpresasTB
    WHERE IdEstado = @idEstadoActivo
    ORDER BY NombreEmpresa;
END;
GO