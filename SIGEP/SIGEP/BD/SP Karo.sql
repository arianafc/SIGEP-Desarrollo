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

    DELETE FROM DocumentosTB
    WHERE IdDocumento = @IdDocumento;
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