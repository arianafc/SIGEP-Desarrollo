CREATE PROCEDURE [dbo].[RegistroSP] 
    @Nombre VARCHAR(20), 
    @Apellido1 VARCHAR(50), 
    @Apellido2 VARCHAR(50), 
    @Correo VARCHAR(255), 
    @IdEspecialidad INT,
    @FechaNacimiento DATETIME,
    @IdSeccion INT,
    @Contrasenna VARCHAR(255), 
    @Cedula VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IdUsuario INT;

    -- Validaciones
    IF @IdSeccion IS NULL
    BEGIN
        RAISERROR('La sección especificada no existe.', 16, 1);
        RETURN;
    END

    IF @IdEspecialidad IS NULL
    BEGIN
        RAISERROR('La especialidad especificada no existe.', 16, 1);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM UsuariosTB WHERE Cedula = @Cedula)
    BEGIN
        RAISERROR('Imposible completar el registro. Ya existe una cuenta asociada a esa cédula.', 16, 1);
        RETURN;
    END

    BEGIN TRY
        BEGIN TRAN;

        -- Insertar usuario
        INSERT INTO dbo.UsuariosTB (Nombre, Apellido1, Apellido2, Contrasenna, FechaNacimiento, Cedula, IdEstado, IdRol, IdSeccion, FechaRegistro)
        VALUES (
            @Nombre, 
            @Apellido1, 
            @Apellido2, 
            CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', @Contrasenna), 2), 
            @FechaNacimiento,
            @Cedula, 
            1,
            1, -- Estudiante
            @IdSeccion,
            GETDATE()
        );

        SET @IdUsuario = SCOPE_IDENTITY();

        -- Insertar correo
        INSERT INTO dbo.EmailsTB (IdUsuario, Email) 
        VALUES (@IdUsuario, @Correo);

        -- Relación usuario-especialidad
        INSERT INTO dbo.UsuarioEspecialidadTB (IdEspecialidad, IdUsuario, IdEstado) 
        VALUES (@IdEspecialidad, @IdUsuario, 1);

        COMMIT;

        -- Devolver el ID
        SELECT @IdUsuario AS IdUsuario;

    END TRY
    BEGIN CATCH
        ROLLBACK;
        THROW;
    END CATCH
END;
GO

USE [SIGEP]
GO

/****** Object:  StoredProcedure [dbo].[LoginSP]    Script Date: 9/22/2025 12:51:44 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[LoginSP]
    @CEDULA VARCHAR(100),
    @CONTRASENNA VARCHAR(100)
AS
BEGIN

    SELECT 
        U.IdUsuario,
        U.Nombre,
        U.Apellido1, 
        U.Apellido2,
        U.Cedula,
        U.IdRol,
        U.IdEstado,
        S.Seccion,
        E.Nombre
    FROM dbo.UsuariosTB U 
    INNER JOIN dbo.SeccionesTB S 
    on U.IdSeccion = S.IdSeccion
    INNER JOIN dbo.UsuarioEspecialidadTB UE
    ON UE.IdUsuario = U.IdUsuario
    INNER JOIN dbo.EspecialidadesTB E
    ON UE.IdEspecialidad = E.IdEspecialidad
    WHERE U.Cedula = @CEDULA
    AND Contrasenna = CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', @CONTRASENNA), 2);
END;
GO
