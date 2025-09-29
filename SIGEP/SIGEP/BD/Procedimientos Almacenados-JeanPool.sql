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
CREATE OR ALTER PROCEDURE ActualizarEstadoPracticaSP
    @IdPractica INT,
    @IdEstado INT,
    @Comentario NVARCHAR(MAX)
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
            INSERT INTO ComentariosPracticaTB (Comentario, Fecha, IdUsuario, IdPractica, Tipo)
            SELECT @Comentario, GETDATE(), p.IdUsuario, p.IdPractica, 'Actualización Estado'
            FROM PracticaEstudianteTB p
            WHERE p.IdPractica = @IdPractica;
            
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