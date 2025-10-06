USE SIGEP
go
-- Stored Procedure principal para obtener todos los datos
CREATE OR ALTER PROCEDURE [dbo].[ObtenerVisualizacionPracticaSP]
    @IdVacante INT,
    @IdUsuario INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        -- Datos de la Vacante
        v.IdVacante,
        v.Nombre,
        e.NombreEmpresa as EmpresaNombre,
        v.Requerimientos,
        v.FechaMaxAplicacion,
        ISNULL(m.Descripcion, v.IdModalidad) as ModalidadNombre,
        
        -- Datos del Estudiante
        u.IdUsuario,
        CONCAT(u.Nombre, ' ', u.Apellido1, ' ', u.Apellido2) as EstudianteNombre,
        u.Cedula as EstudianteCedula,
        DATEDIFF(YEAR, u.FechaNacimiento, GETDATE()) as EstudianteEdad,
        esp.Nombre as EstudianteEspecialidad,
        eu.Email as EstudianteCorreo,
        
        -- Datos de Contacto Empresa
        e.NombreContacto as ContactoEmpresaNombre,
        ee.Email as ContactoEmpresaEmail,
        te.Telefono as ContactoEmpresaTelefono,
        
        -- Datos de la Práctica (AGREGADO IdPractica)
        p.IdPractica,
        p.FechaAplicacion,
        est.Descripcion as EstadoPractica
        
    FROM VacantesPracticasTB v
    INNER JOIN EmpresasTB e ON v.IdEmpresa = e.IdEmpresa
    LEFT JOIN ModalidadesTB m ON v.IdModalidad = m.IdModalidad
    INNER JOIN PracticaEstudianteTB p ON v.IdVacante = p.IdVacante AND p.IdUsuario = @IdUsuario
    INNER JOIN UsuariosTB u ON p.IdUsuario = u.IdUsuario
    INNER JOIN EstadosTB est ON p.IdEstado = est.IdEstado
    LEFT JOIN UsuarioEspecialidadTB ue ON u.IdUsuario = ue.IdUsuario AND ue.IdEstado = 1
    LEFT JOIN EspecialidadesTB esp ON ue.IdEspecialidad = esp.IdEspecialidad
    LEFT JOIN EmailsTB eu ON u.IdUsuario = eu.IdUsuario
    LEFT JOIN EmailsTB ee ON e.IdEmpresa = ee.IdEmpresa  
    LEFT JOIN TelefonosTB te ON e.IdEmpresa = te.IdEmpresa
    WHERE v.IdVacante = @IdVacante
END

-- Stored Procedure para obtener comentarios
CREATE OR ALTER PROCEDURE [dbo].[ObtenerComentariosPracticaSP]
    @IdVacante INT,
    @IdUsuario INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        c.IdComentario as Id,
        c.Fecha,
        CONCAT(u.Nombre, ' ', u.Apellido1) as Usuario,
        c.Comentario
    FROM ComentariosPracticaTB c
    INNER JOIN UsuariosTB u ON c.IdUsuario = u.IdUsuario
    INNER JOIN PracticaEstudianteTB p ON c.IdPractica = p.IdPractica
    WHERE p.IdVacante = @IdVacante AND p.IdUsuario = @IdUsuario
    ORDER BY c.Fecha DESC
END


-- Stored Procedure para insertar comentarios
CREATE OR ALTER PROCEDURE [dbo].[InsertarComentarioPracticaSP]
    @IdVacante INT,
    @IdUsuario INT,
    @Comentario VARCHAR(1000),
    @IdUsuarioComentario INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @IdPractica INT;
    DECLARE @FilasAfectadas INT = 0;
    
    -- Obtener el IdPractica
    SELECT @IdPractica = IdPractica 
    FROM PracticaEstudianteTB 
    WHERE IdVacante = @IdVacante AND IdUsuario = @IdUsuario;
    
    IF @IdPractica IS NOT NULL
    BEGIN
        INSERT INTO ComentariosPracticaTB 
        (Comentario, Fecha, IdUsuario, IdPractica, Tipo)
        VALUES 
        (@Comentario, GETDATE(), @IdUsuarioComentario, @IdPractica, 'General');
        
        SET @FilasAfectadas = @@ROWCOUNT;
    END
    
    -- Devolver el número de filas afectadas
    SELECT @FilasAfectadas as FilasAfectadas;
END

-- Stored Procedure para actualizar estado de la practica
-- Stored Procedure para actualizar estado de la practica
CREATE OR ALTER PROCEDURE ActualizarEstadoPracticaSP
    @IdPractica INT,
    @IdEstado INT,
    @Comentario NVARCHAR(MAX),
    @IdUsuarioSesion INT 
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- 1. Actualizar estado de la práctica
        UPDATE PracticaEstudianteTB
        SET IdEstado = @IdEstado
        WHERE IdPractica = @IdPractica;
        
        -- 2. Insertar comentario si se proporcionó
        DECLARE @IdComentarioNuevo INT = NULL;
        
        IF @Comentario IS NOT NULL AND LTRIM(RTRIM(@Comentario)) <> ''
        BEGIN
            -- Usar @IdUsuarioSesion en lugar de p.IdUsuario
            INSERT INTO ComentariosPracticaTB (Comentario, Fecha, IdUsuario, IdPractica, Tipo)
            VALUES (@Comentario, GETDATE(), @IdUsuarioSesion, @IdPractica, 'Actualización Estado');
            
            -- Capturar el ID del comentario recién insertado
            SET @IdComentarioNuevo = SCOPE_IDENTITY();
        END
        
        -- 3. Devolver información relevante
        SELECT 
            p.IdPractica,
            p.IdVacante,
            p.IdUsuario,
            u.Nombre + ' ' + u.Apellido1 + ' ' + ISNULL(u.Apellido2, '') AS EstudianteNombre,
            e.Email AS EstudianteCorreo,
            es.Descripcion AS EstadoDescripcion,
            c.Comentario,
            c.Fecha AS FechaComentario
        FROM PracticaEstudianteTB p
        INNER JOIN UsuariosTB u ON p.IdUsuario = u.IdUsuario
        LEFT JOIN EmailsTB e ON u.IdUsuario = e.IdUsuario
        INNER JOIN EstadosTB es ON p.IdEstado = es.IdEstado
        -- Usar el comentario específico que acabamos de insertar
        LEFT JOIN ComentariosPracticaTB c ON c.IdComentario = @IdComentarioNuevo
        WHERE p.IdPractica = @IdPractica;
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END

-- SP para registrar autogestión de práctica
CREATE OR ALTER PROCEDURE [dbo].[RegistrarAutogestionPracticaSP]
    @IdUsuario INT,
    @NombreEmpresa VARCHAR(255),
    @Sector VARCHAR(255),
    @NombreEncargado VARCHAR(255),
    @Puesto VARCHAR(255),
    @Correo VARCHAR(255),
    @Telefono VARCHAR(50),
    @IdDistrito INT,
    @DireccionExacta VARCHAR(2000),
    @DescripcionTareas VARCHAR(1000),
    @Duracion VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        
        DECLARE @IdDireccion INT, @IdEmpresa INT, @IdVacante INT, @IdPractica INT;
        
        -- 1. Crear dirección
        INSERT INTO DireccionesTB (DireccionExacta, IdEstado, IdDistrito)
        VALUES (@DireccionExacta, 1, @IdDistrito);
        SET @IdDireccion = SCOPE_IDENTITY();
        
        -- 2. Crear empresa
        INSERT INTO EmpresasTB (NombreEmpresa, NombreContacto, IdDireccion, AreasAfines, IdEstado)
        VALUES (@NombreEmpresa, @NombreEncargado, @IdDireccion, @Sector, 1);
        SET @IdEmpresa = SCOPE_IDENTITY();
        
        -- 3. Insertar email y teléfono de la empresa
        INSERT INTO EmailsTB (IdEmpresa, Email) VALUES (@IdEmpresa, @Correo);
        INSERT INTO TelefonosTB (IdEmpresa, Telefono) VALUES (@IdEmpresa, @Telefono);
        
        -- 4. Crear vacante
        INSERT INTO VacantesPracticasTB (Nombre, IdEmpresa, Requerimientos, FechaMaxAplicacion, 
                                        NumCupos, IdModalidad, Descripcion, Tipo, IdEstado)
        VALUES (@Puesto, @IdEmpresa, 'Práctica autogestionada por estudiante', GETDATE(), 
                1, 'Híbrido', @DescripcionTareas, 'Autogestionada', 1);
        SET @IdVacante = SCOPE_IDENTITY();
        
        -- 5. Crear práctica con estado "Pendiente de Aprobación" (asumiendo IdEstado = 5)
        INSERT INTO PracticaEstudianteTB (IdVacante, IdUsuario, FechaAplicacion, IdEstado)
        VALUES (@IdVacante, @IdUsuario, GETDATE(), 5); -- Estado pendiente de aprobación
        SET @IdPractica = SCOPE_IDENTITY();
        
        -- 6. Agregar comentario inicial
        INSERT INTO ComentariosPracticaTB (Comentario, Fecha, IdUsuario, IdPractica, Tipo)
        VALUES ('Práctica autogestionada registrada por el estudiante. Pendiente de aprobación.', 
                GETDATE(), @IdUsuario, @IdPractica, 'Autogestion');
        
        COMMIT TRANSACTION;
        SELECT 1 as Resultado, @IdPractica as IdPractica;
        
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        SELECT 0 as Resultado, ERROR_MESSAGE() as Error;
    END CATCH
END

-- SP para obtener postulaciones del estudiante
CREATE OR ALTER PROCEDURE [dbo].[ObtenerPostulacionesEstudianteSP]
    @IdUsuario INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        p.IdPractica,
        v.IdVacante,
        p.IdUsuario,  -- Agregar IdUsuario
        v.Nombre as NombreVacante,
        e.NombreEmpresa,
        est.Descripcion as EstadoPractica,
        p.FechaAplicacion,
        CASE 
            WHEN v.Tipo = 'Autogestionada' THEN CAST(1 AS BIT)
            ELSE CAST(0 AS BIT)
        END as EsAutogestionada  -- Agregar EsAutogestionada
    FROM PracticaEstudianteTB p
    INNER JOIN VacantesPracticasTB v ON p.IdVacante = v.IdVacante
    INNER JOIN EmpresasTB e ON v.IdEmpresa = e.IdEmpresa
    INNER JOIN EstadosTB est ON p.IdEstado = est.IdEstado
    WHERE p.IdUsuario = @IdUsuario
    ORDER BY p.FechaAplicacion DESC
END


-- SP de Johnny
CREATE OR ALTER PROCEDURE[dbo].[ObtenerEstudiantesProfesorSP]
    @IdUsuario INT,
    @IdVacante INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.UsuariosTB WHERE IdUsuario = @IdUsuario)
    BEGIN
        RAISERROR('El IdUsuario especificado no existe.', 16, 1);
        RETURN;
    END;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.UsuarioEspecialidadTB 
        WHERE IdUsuario = @IdUsuario AND IdEstado = 1
    )
    BEGIN
        RAISERROR('El usuario no tiene especialidades activas.', 16, 1);
        RETURN;
    END;

    ;WITH EspecialidadesDelUsuario AS (
        SELECT ue.IdEspecialidad
        FROM dbo.UsuarioEspecialidadTB ue
        WHERE ue.IdUsuario = @IdUsuario
          AND ue.IdEstado = 1
    )
    SELECT
        u.IdUsuario,
        CONCAT(u.Nombre, ' ', u.Apellido1, ' ', u.Apellido2) AS Nombre,
        u.Cedula,
        CASE 
            WHEN MAX(CASE WHEN asg.TieneAsignada = 1 THEN 1 ELSE 0 END) = 1 THEN 'Asignada'
            WHEN COUNT(p.IdPractica) > 0 THEN 'Con Procesos Activos'
            ELSE 'Sin Procesos Activos'
        END AS EstadoPractica,                    -- situación global del estudiante
        estUsuario.Descripcion AS EstadoUsuario,
        esp.Nombre             AS Especialidad,
        -- Relación específica con ESTA vacante:
        CAST(CASE WHEN vac.IdPractica IS NULL THEN 0 ELSE 1 END AS bit) AS TieneRelacionEnVacante,
        vac.EstadoVacante,
        vac.IdPractica AS IdPracticaVacante
    FROM dbo.UsuariosTB u
    INNER JOIN dbo.UsuariosTB uRol
        ON uRol.IdUsuario = u.IdUsuario AND uRol.IdRol = 1
    INNER JOIN dbo.UsuarioEspecialidadTB ueMatch
        ON ueMatch.IdUsuario = u.IdUsuario AND ueMatch.IdEstado = 1
    INNER JOIN dbo.EspecialidadesTB esp
        ON esp.IdEspecialidad = ueMatch.IdEspecialidad
    INNER JOIN dbo.EstadosTB estUsuario
        ON estUsuario.IdEstado = u.IdEstado
    LEFT JOIN dbo.PracticaEstudianteTB p
        ON p.IdUsuario = u.IdUsuario
    OUTER APPLY (
        SELECT TOP (1) 1 AS TieneAsignada
        FROM dbo.PracticaEstudianteTB px
        INNER JOIN dbo.EstadosTB esx ON esx.IdEstado = px.IdEstado
        WHERE px.IdUsuario = u.IdUsuario
          AND esx.Descripcion = 'Asignada'
    ) asg
    OUTER APPLY (
        SELECT TOP (1) pv.IdPractica, ev.Descripcion AS EstadoVacante
        FROM dbo.PracticaEstudianteTB pv
        INNER JOIN dbo.EstadosTB ev ON ev.IdEstado = pv.IdEstado
        WHERE pv.IdUsuario = u.IdUsuario
          AND (@IdVacante IS NOT NULL AND pv.IdVacante = @IdVacante)
        ORDER BY pv.FechaAplicacion DESC, pv.IdPractica DESC
    ) vac
    WHERE EXISTS (
        SELECT 1 FROM EspecialidadesDelUsuario eu
        WHERE eu.IdEspecialidad = ueMatch.IdEspecialidad
    )
    GROUP BY
        u.IdUsuario, u.Nombre, u.Apellido1, u.Apellido2, u.Cedula,
        estUsuario.Descripcion, esp.Nombre,
        vac.IdPractica, vac.EstadoVacante
    ORDER BY Nombre;
END;
GO

-- Sprint #2

-- SP para obtener estudiantes asignados a un profesor
CREATE OR ALTER PROCEDURE ObtenerEstudiantesParaEvaluacionSP
    @IdProfesor INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        u.IdUsuario,
        u.Cedula,
        u.Nombre + ' ' + u.Apellido1 + ' ' + ISNULL(u.Apellido2, '') AS NombreCompleto,
        e.Nombre AS Especialidad,
        t.Telefono,
        v.Nombre AS PracticaAsignada,
        CASE 
            WHEN n.NotaFinal >= 70 THEN 'Aprobado'
            WHEN n.NotaFinal < 70 AND n.NotaFinal IS NOT NULL THEN 'Rezagado'
            ELSE 'Aprobado'
        END AS EstadoAcademico,
        CAST(n.NotaFinal AS DECIMAL(5,2)) AS NotaFinal
    FROM UsuariosTB u
    INNER JOIN UsuarioEspecialidadTB ue ON u.IdUsuario = ue.IdUsuario AND ue.IdEstado = 1
    INNER JOIN EspecialidadesTB e ON ue.IdEspecialidad = e.IdEspecialidad
    LEFT JOIN TelefonosTB t ON u.IdUsuario = t.IdUsuario
    INNER JOIN PracticaEstudianteTB p ON u.IdUsuario = p.IdUsuario
    LEFT JOIN VacantesPracticasTB v ON p.IdVacante = v.IdVacante
    LEFT JOIN NotasEstudiantesTB n ON u.IdUsuario = n.IdUsuario
    WHERE u.IdRol = 1 -- Estudiantes
        AND u.IdEstado = 1 -- Activos
        AND p.IdEstado IN (
            SELECT IdEstado FROM EstadosTB 
            WHERE Descripcion IN ('Asignada', 'En Proceso', 'Aprobado')
        )
    ORDER BY u.Nombre, u.Apellido1;
END
GO

-- SP para obtener perfil completo del estudiante
CREATE OR ALTER PROCEDURE ObtenerPerfilEstudianteSP
    @IdUsuario INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        u.Nombre + ' ' + u.Apellido1 + ' ' + ISNULL(u.Apellido2, '') AS NombreCompleto,
        em.Email AS Correo,
        t.Telefono,
        d.DireccionExacta + ', ' + dis.Nombre + ', ' + c.Nombre + ', ' + p.Nombre AS Direccion,
        CASE WHEN u.Nombre LIKE '%a' THEN 'Femenino' ELSE 'Masculino' END AS Sexo, -- Ajustar según tu campo
        e.Nombre AS Especialidad,
        DATEDIFF(YEAR, u.FechaNacimiento, GETDATE()) AS Edad,
        s.Seccion,
        emp.NombreEmpresa,
        temp.Telefono AS TelefonoEmpresa
    FROM UsuariosTB u
    LEFT JOIN EmailsTB em ON u.IdUsuario = em.IdUsuario
    LEFT JOIN TelefonosTB t ON u.IdUsuario = t.IdUsuario
    LEFT JOIN DireccionesTB d ON u.IdDireccion = d.IdDireccion
    LEFT JOIN DistritosTB dis ON d.IdDistrito = dis.IdDistrito
    LEFT JOIN CantonesTB c ON dis.IdCanton = c.IdCanton
    LEFT JOIN ProvinciasTB p ON c.IdProvincia = p.IdProvincia
    LEFT JOIN UsuarioEspecialidadTB ue ON u.IdUsuario = ue.IdUsuario
    LEFT JOIN EspecialidadesTB e ON ue.IdEspecialidad = e.IdEspecialidad
    LEFT JOIN SeccionesTB s ON u.IdSeccion = s.IdSeccion
    LEFT JOIN PracticaEstudianteTB pr ON u.IdUsuario = pr.IdUsuario
    LEFT JOIN VacantesPracticasTB v ON pr.IdVacante = v.IdVacante
    LEFT JOIN EmpresasTB emp ON v.IdEmpresa = emp.IdEmpresa
    LEFT JOIN TelefonosTB temp ON emp.IdEmpresa = temp.IdEmpresa
    WHERE u.IdUsuario = @IdUsuario;
END
GO

-- SP para obtener comentarios del estudiante
CREATE OR ALTER PROCEDURE ObtenerComentariosEstudianteSP
    @IdUsuario INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        u.Nombre + ' ' + u.Apellido1 AS Autor,
        c.Fecha,
        c.Comentario
    FROM ComentariosPracticaTB c
    INNER JOIN PracticaEstudianteTB p ON c.IdPractica = p.IdPractica
    INNER JOIN UsuariosTB u ON c.IdUsuario = u.IdUsuario
    WHERE p.IdUsuario = @IdUsuario
        AND (
            LTRIM(RTRIM(c.Tipo)) = 'Evaluación Tutor' 
            OR LTRIM(RTRIM(c.Tipo)) = 'Actualización Estado'
        )
    ORDER BY c.Fecha DESC, c.IdComentario DESC; 
END
GO

-- SP para obtener notas del estudiante
CREATE OR ALTER PROCEDURE ObtenerNotasEstudianteSP
    @IdUsuario INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        Nota1,
        Nota2,
        NotaFinal
    FROM NotasEstudiantesTB
    WHERE IdUsuario = @IdUsuario;
END
GO

-- SP para guardar/actualizar notas del estudiante
CREATE OR ALTER PROCEDURE GuardarNotaEstudianteSP
    @IdUsuario INT,
    @Nota1 DECIMAL(5,2),
    @Nota2 DECIMAL(5,2),
    @NotaFinal DECIMAL(5,2),
    @IdProfesor INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Verificar si ya existe un registro de notas
        IF EXISTS (SELECT 1 FROM NotasEstudiantesTB WHERE IdUsuario = @IdUsuario)
        BEGIN
            -- Actualizar notas existentes
            UPDATE NotasEstudiantesTB
            SET Nota1 = @Nota1,
                Nota2 = @Nota2,
                NotaFinal = @NotaFinal,
                FechaActualizacion = GETDATE(),
                IdProfesor = @IdProfesor
            WHERE IdUsuario = @IdUsuario;
        END
        ELSE
        BEGIN
            -- Insertar nuevas notas
            INSERT INTO NotasEstudiantesTB (IdUsuario, Nota1, Nota2, NotaFinal, FechaRegistro, IdProfesor)
            VALUES (@IdUsuario, @Nota1, @Nota2, @NotaFinal, GETDATE(), @IdProfesor);
        END
        
        COMMIT TRANSACTION;
        
        SELECT 1 AS Exito, 'Nota registrada correctamente' AS Mensaje;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        
        SELECT 0 AS Exito, ERROR_MESSAGE() AS Mensaje;
    END CATCH
END
GO


-- Crear tabla para almacenar las notas de los estudiantes
CREATE TABLE [dbo].[NotasEstudiantesTB](
    [IdNota] [int] IDENTITY(1,1) NOT NULL,
    [IdUsuario] [int] NOT NULL,
    [Nota1] [decimal](5, 2) NULL,
    [Nota2] [decimal](5, 2) NULL,
    [NotaFinal] [decimal](5, 2) NULL,
    [FechaRegistro] [datetime] NOT NULL DEFAULT GETDATE(),
    [FechaActualizacion] [datetime] NULL,
    [IdProfesor] [int] NOT NULL,
    CONSTRAINT [PK_NotasEstudiantesTB] PRIMARY KEY CLUSTERED ([IdNota] ASC),
    CONSTRAINT [FK_NotasEstudiantes_Usuario] FOREIGN KEY([IdUsuario]) REFERENCES [dbo].[UsuariosTB] ([IdUsuario]),
    CONSTRAINT [FK_NotasEstudiantes_Profesor] FOREIGN KEY([IdProfesor]) REFERENCES [dbo].[UsuariosTB] ([IdUsuario])
)
GO