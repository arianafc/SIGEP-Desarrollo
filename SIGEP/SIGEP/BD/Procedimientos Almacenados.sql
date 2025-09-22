CREATE PROCEDURE [dbo].[RegistroSP] 
    @Nombre VARCHAR(20), 
    @Apellido1 VARCHAR(50), 
    @Apellido2 VARCHAR(50), 
    @Correo VARCHAR(255), 
    @Especialidad VARCHAR(255),
    @FechaNacimiento DATETIME,
    @Seccion VARCHAR(20),
    @Contrasenna VARCHAR(255), 
    @Cedula VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IdSeccion INT;
    DECLARE @IdEspecialidad INT;
    DECLARE @IdUsuario INT;

    -- Obtenemos los IDs
    SELECT @IdSeccion = IdSeccion
    FROM SeccionesTB
    WHERE Seccion = @Seccion;

    SELECT @IdEspecialidad = IdEspecialidad
    FROM EspecialidadesTB
    WHERE Nombre = @Especialidad;

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
        INSERT INTO dbo.UsuariosTB (Nombre, Apellido1, Apellido2, Contrasenna, Cedula, IdEstado, IdRol, IdSeccion)
        VALUES (
            @Nombre, 
            @Apellido1, 
            @Apellido2, 
            CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', @Contrasenna), 2), 
            @Cedula, 
            1,
            1, -- Estudiante
            @IdSeccion
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
