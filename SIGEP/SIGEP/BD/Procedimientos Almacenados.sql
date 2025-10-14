USE SIGEP;

Go

CREATE OR ALTER PROCEDURE [dbo].[RegistroSP] 
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


CREATE OR ALTER PROCEDURE [dbo].[LoginSP]
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
        E.Nombre AS Especialidad
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

-----------------------------------------------------------
--23 DE SEPTIEMBRE -- SP PARA RECUPERAR CONTRASEÑA
USE [SIGEP]
GO

/****** Object:  StoredProcedure [dbo].[CambiarContrasennaSP]    Script Date: 9/23/2025 11:12:03 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[CambiarContrasennaSP]
    @CEDULA VARCHAR(100),
    @NUEVA_CONTRASENNA VARCHAR(100)
AS
BEGIN
    -- Verifica si el usuario existe
    IF NOT EXISTS (SELECT 1 FROM UsuariosTb WHERE Cedula = @CEDULA)
    BEGIN
        RAISERROR('El usuario no existe.', 16, 1);
        RETURN;
    END

    -- Actualiza la contraseña encriptada
    UPDATE UsuariosTB
    SET Contrasenna = CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', @NUEVA_CONTRASENNA), 2)
    WHERE Cedula = @CEDULA;
END;
GO

CREATE OR ALTER PROCEDURE ObtenerEncargadosUsuarioSP
@IdUsuario int
AS
BEGIN
SELECT 
    E.IdEncargado, E.Cedula, E.Nombre, E.Apellido1, E.Apellido2, 
    E.FechaRegistro, E.Ocupacion, E.LugarTrabajo, E.IdEstado,
    EE.Parentesco,
    (SELECT TOP 1 T.Telefono 
     FROM TelefonosTB T 
     WHERE T.IdEncargado = E.IdEncargado) AS Telefono, (SELECT TOP 1 C.Email 
     FROM EmailsTB C
     WHERE C.IdEncargado = E.IdEncargado) AS Correo
FROM EncargadosTB E
INNER JOIN EstudianteEncargadoTB EE 
    ON E.IdEncargado = EE.IdEncargado
WHERE EE.IdUsuario = @IdUsuario;

END;

--SP PARA ACCIONES DE ENCARGADO EN MI PERFIL

USE [SIGEP]
GO

/****** Object:  StoredProcedure [dbo].[AccionesEncargadoSP]    Script Date: 10/12/2025 11:29:55 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE OR ALTER PROCEDURE [dbo].[AccionesEncargadoSP]
    @Accion INT,
    @IdEncargado INT = NULL,
    @Nombre VARCHAR(255) = NULL,
    @Telefono VARCHAR(255) = NULL,
    @Parentesco VARCHAR(255) = NULL,
    @LugarTrabajo VARCHAR(255) = NULL,
    @Ocupacion VARCHAR(255) = NULL,
    @Correo VARCHAR(255) = NULL,
    @Cedula VARCHAR(255) = NULL,
    @Apellido1 VARCHAR(255) = NULL,
    @Apellido2 VARCHAR(255) = NULL,
    @IdUsuario INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TelefonoExiste INT;
    DECLARE @EmailExiste INT;
    DECLARE @IdNuevoEncargado INT;

    -- ========================================
    -- 1️ ACTUALIZAR ENCARGADO
    -- ========================================
    IF (@Accion = 1)
    BEGIN

    UPDATE EncargadosTB
            SET Cedula = @Cedula,
                Nombre = @Nombre,
                Apellido1 = @Apellido1,
                Apellido2 = @Apellido2,
                Ocupacion = @Ocupacion,
                LugarTrabajo = @LugarTrabajo
            WHERE IdEncargado = @IdEncargado;

        SELECT @TelefonoExiste = COUNT(*) FROM TelefonosTB WHERE IdEncargado = @IdEncargado;
        SELECT @EmailExiste = COUNT(*) FROM EmailsTB WHERE IdEncargado = @IdEncargado;

        IF (@TelefonoExiste > 0)
            UPDATE TelefonosTB
                SET Telefono = @Telefono
                WHERE IdEncargado = @IdEncargado;
        ELSE
            INSERT INTO TelefonosTB (Telefono, IdEncargado)
            VALUES (@Telefono, @IdEncargado);

        IF (@EmailExiste > 0)
            UPDATE EmailsTB
                SET Email = @Correo
                WHERE IdEncargado = @IdEncargado;
        ELSE
            INSERT INTO EmailsTB (Email, IdEncargado)
            VALUES (@Correo, @IdEncargado);
         SELECT 1 AS Result;
        
    END

    -- ========================================
    -- 2️ DESACTIVAR RELACIÓN
    -- ========================================
    ELSE IF (@Accion = 2)
    BEGIN
        UPDATE EstudianteEncargadoTB
            SET IdEstado = 2
            WHERE IdUsuario = @IdUsuario AND IdEncargado = @IdEncargado;
             SELECT 1 AS Result;
    END

    -- ========================================
    -- 3️ AGREGAR NUEVO ENCARGADO
    -- ========================================
    ELSE IF (@Accion = 3)
    BEGIN
        INSERT INTO EncargadosTB (Cedula, Nombre, Apellido1, Apellido2, FechaRegistro, Ocupacion, LugarTrabajo, IdEstado)
        VALUES (@Cedula, @Nombre, @Apellido1, @Apellido2, GETDATE(), @Ocupacion, @LugarTrabajo, 1);

        SET @IdNuevoEncargado = SCOPE_IDENTITY();

        INSERT INTO EmailsTB (Email, IdEncargado)
        VALUES (@Correo, @IdNuevoEncargado);

        INSERT INTO TelefonosTB (Telefono, IdEncargado)
        VALUES (@Telefono, @IdNuevoEncargado);

        INSERT INTO EstudianteEncargadoTB (IdEncargado, IdUsuario, Parentesco, IdEstado)
        VALUES (@IdNuevoEncargado, @IdUsuario, @Parentesco, 1);

        SELECT @IdNuevoEncargado AS Result; 
        
    END

    -- ========================================
    -- 4️ ACTIVAR ENCARGADO
    -- ========================================
    ELSE IF (@Accion = 4)
    BEGIN
        UPDATE EstudianteEncargadoTB
            SET IdEstado = 1
            WHERE IdUsuario = @IdUsuario AND IdEncargado = @IdEncargado;

             SELECT 1 AS Result;
    END
END;
GO


