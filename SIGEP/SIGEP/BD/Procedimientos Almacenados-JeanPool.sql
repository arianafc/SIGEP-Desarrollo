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
        
        -- Datos de la Práctica
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
        
        SELECT 1 as Resultado; -- Éxito
    END
    ELSE
    BEGIN
        SELECT 0 as Resultado; -- Error: no se encontró la práctica
    END
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
        IF @Comentario IS NOT NULL AND LTRIM(RTRIM(@Comentario)) <> ''
        BEGIN
            INSERT INTO ComentariosPracticaTB (Comentario, Fecha, IdUsuario, IdPractica, Tipo)
            SELECT @Comentario, GETDATE(), p.IdUsuario, p.IdPractica, 'Actualización Estado'
            FROM PracticaEstudianteTB p
            WHERE p.IdPractica = @IdPractica;
        END

        -- 3. Devolver información relevante
        SELECT 
            p.IdPractica,
            p.IdVacante,
            p.IdUsuario,
            u.Nombre AS EstudianteNombre,
            e.Email AS EstudianteCorreo,
            es.Descripcion AS EstadoDescripcion,
            c.Comentario,
            c.Fecha AS FechaComentario
        FROM PracticaEstudianteTB p
        INNER JOIN UsuariosTB u ON p.IdUsuario = u.IdUsuario
        LEFT JOIN EmailsTB e ON u.IdUsuario = e.IdUsuario
        INNER JOIN EstadosTB es ON p.IdEstado = es.IdEstado
        LEFT JOIN ComentariosPracticaTB c 
            ON c.IdPractica = p.IdPractica
           AND c.Fecha = (SELECT MAX(Fecha) FROM ComentariosPracticaTB WHERE IdPractica = p.IdPractica)
        WHERE p.IdPractica = @IdPractica;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END




