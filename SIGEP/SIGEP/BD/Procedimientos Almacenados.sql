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
    E.FechaRegistro, E.Ocupacion, E.LugarTrabajo, EE.IdEstado,
    EE.Parentesco,
    (SELECT TOP 1 T.Telefono 
     FROM TelefonosTB T 
     WHERE T.IdEncargado = E.IdEncargado) AS Telefono, (SELECT TOP 1 C.Email 
     FROM EmailsTB C
     WHERE C.IdEncargado = E.IdEncargado) AS Correo
FROM EncargadosTB E
INNER JOIN EstudianteEncargadoTB EE 
    ON E.IdEncargado = EE.IdEncargado
WHERE EE.IdUsuario = @IdUsuario AND E.IdEstado = 1;

END;

--SP PARA ACCIONES DE ENCARGADO EN MI PERFIL

USE [SIGEP]
GO

/****** Object:  StoredProcedure [dbo].[AccionesEncargadoSP]    Script Date: 10/19/2025 11:56:35 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


ALTER PROCEDURE [dbo].[AccionesEncargadoSP]
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

    DECLARE @EsEstudiante INT;
    DECLARE @IdDuplicado INT;
    DECLARE @IdEncargadoExistente INT;


    -- ========================================
    -- 1️ ACTUALIZAR ENCARGADO
    -- ========================================
    IF (@Accion = 1)
    BEGIN
      

        -- Actualizar datos del encargado
        UPDATE EncargadosTB
        SET Cedula = @Cedula,
            Nombre = @Nombre,
            Apellido1 = @Apellido1,
            Apellido2 = @Apellido2,
            Ocupacion = @Ocupacion,
            LugarTrabajo = @LugarTrabajo
        WHERE IdEncargado = @IdEncargado;

        -- Actualizar o insertar teléfono
        IF EXISTS (SELECT 1 FROM TelefonosTB WHERE IdEncargado = @IdEncargado)
            UPDATE TelefonosTB
            SET Telefono = @Telefono
            WHERE IdEncargado = @IdEncargado;
        ELSE
            INSERT INTO TelefonosTB (Telefono, IdEncargado)
            VALUES (@Telefono, @IdEncargado);

        -- Actualizar o insertar correo
        IF EXISTS (SELECT 1 FROM EmailsTB WHERE IdEncargado = @IdEncargado)
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
    -- 3️ AGREGAR NUEVO ENCARGADO / ACTUALIZAR SI EXISTE
    -- ========================================
    ELSE IF (@Accion = 3)
    BEGIN
        -- Verificar si ya existe la cédula en EncargadosTB
        SELECT @IdEncargadoExistente = IdEncargado
        FROM EncargadosTB
        WHERE Cedula = @Cedula;

        IF @IdEncargadoExistente IS NULL
        BEGIN
            -- Insertar nuevo encargado
            INSERT INTO EncargadosTB (Cedula, Nombre, Apellido1, Apellido2, FechaRegistro, Ocupacion, LugarTrabajo, IdEstado)
            VALUES (@Cedula, @Nombre, @Apellido1, @Apellido2, GETDATE(), @Ocupacion, @LugarTrabajo, 1);

            SET @IdEncargadoExistente = SCOPE_IDENTITY();
        END
        ELSE
        BEGIN
            -- Si ya existe, actualizar sus datos
            UPDATE EncargadosTB
            SET Nombre = @Nombre,
                Apellido1 = @Apellido1,
                Apellido2 = @Apellido2,
                Ocupacion = @Ocupacion,
                LugarTrabajo = @LugarTrabajo
            WHERE IdEncargado = @IdEncargadoExistente;
        END

        -- Actualizar o insertar correo
        IF EXISTS (SELECT 1 FROM EmailsTB WHERE IdEncargado = @IdEncargadoExistente)
            UPDATE EmailsTB
            SET Email = @Correo
            WHERE IdEncargado = @IdEncargadoExistente;
        ELSE
            INSERT INTO EmailsTB (Email, IdEncargado)
            VALUES (@Correo, @IdEncargadoExistente);

        -- Actualizar o insertar teléfono
        IF EXISTS (SELECT 1 FROM TelefonosTB WHERE IdEncargado = @IdEncargadoExistente)
            UPDATE TelefonosTB
            SET Telefono = @Telefono
            WHERE IdEncargado = @IdEncargadoExistente;
        ELSE
            INSERT INTO TelefonosTB (Telefono, IdEncargado)
            VALUES (@Telefono, @IdEncargadoExistente);

        -- Relacionar encargado con el estudiante (si no existe)
        IF NOT EXISTS (
            SELECT 1 FROM EstudianteEncargadoTB
            WHERE IdEncargado = @IdEncargadoExistente AND IdUsuario = @IdUsuario
        )
        BEGIN
            INSERT INTO EstudianteEncargadoTB (IdEncargado, IdUsuario, Parentesco, IdEstado)
            VALUES (@IdEncargadoExistente, @IdUsuario, @Parentesco, 1);
        END

        SELECT @IdEncargadoExistente AS Result;
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
END
GO



USE [SIGEP]
GO

/****** Object:  StoredProcedure [dbo].[ObtenerDocumentosPerfilSP]    Script Date: 10/27/2025 4:32:28 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE   PROCEDURE [dbo].[ObtenerDocumentosPerfilSP]
    @IdUsuario INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        IdDocumento,
        Documento,
        Tipo,
        RutaArchivo,
        FechaSubida,
        LOWER(RIGHT(Documento, CHARINDEX('.', REVERSE(Documento)))) AS Extension
    FROM DocumentosTB
    WHERE IdUsuario = @IdUsuario 
        AND Tipo = 'Perfil'
    ORDER BY FechaSubida DESC;
END
GO


CREATE OR ALTER PROCEDURE ObtenerBolsaEmpleoSP
AS
BEGIN
    
    SELECT B.IdEmpleo, B.Empresa, B.IdEstado, B.Descripcion, B.Requisitos, B.FechaPublicacion, B.FechaLimite, B.IdDireccion, B.AreaAfin, D.DireccionExacta,
    D.IdDistrito, Di.Nombre as Distrito, M.Descripcion as Modalidad, p.IdProvincia, P.Nombre as Provincia, C.IdCanton, C.Nombre as Canton, M.IdModalidad
    FROM BolsaEmpleoTB B
    LEFT JOIN DireccionesTB D
    ON B.IdDireccion = D.IdDireccion
    LEFT JOIN DistritosTB Di
    ON D.IdDistrito = Di.IdDistrito
    LEFT JOIN CantonesTB C
    ON di.IdCanton = c.IdCanton
    LEFT JOIN ProvinciasTB P
    ON C.IdProvincia = P.IdProvincia
    LEFT JOIN ModalidadesTB M
    ON M.IdModalidad = B.IdModalidad


END

------SP Historico
USE [SIGEP]
GO

/****** Object:  StoredProcedure [dbo].[HistoricoPracticasSP]    Script Date: 24/11/2025 18:36:50 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER   PROCEDURE [dbo].[HistoricoPracticasSP]
AS
BEGIN

SELECT 
    P.IdPractica, 
    P.IdVacante, 
    P.IdUsuario, 
    U.Nombre,
    U.Apellido1,
    U.Apellido2,
    P.FechaAplicacion, 
    P.IdEstado, 
    V.Nombre AS NombreVacante,
    V.Requerimientos, 
    V.FechaMaxAplicacion, 
    V.NumCupos, 
    V.FechaCierre, 
    V.Descripcion,
    V.Tipo, 
    M.Descripcion AS Modalidad, 
    EV.IdEspecialidad, 
    E.Nombre AS Especialidad
FROM PracticaEstudianteTB P
INNER JOIN VacantesPracticasTB V ON P.IdVacante = V.IdVacante
INNER JOIN ModalidadesTB M ON V.IdModalidad = M.IdModalidad
LEFT JOIN EspecialidadesVacantesTB EV ON EV.IdVacante = P.IdVacante
INNER JOIN EspecialidadesTB E ON E.IdEspecialidad = EV.IdEspecialidad
INNER JOIN UsuariosTB U ON P.IdUsuario = U.IdUsuario
WHERE P.FechaAplicacion IS NOT NULL
  AND YEAR(P.FechaAplicacion) < YEAR(GETDATE());
END;
GO


