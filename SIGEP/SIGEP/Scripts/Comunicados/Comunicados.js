$(function () {

    $("#btnEnviar").on("click", function () {
        const poblacion = $("#correoPoblacion").val().trim();
        const asunto = $("#correoAsunto").val().trim();
        const mensaje = $("#correoMensaje").val().trim();
        const archivos = $("#correoArchivo")[0].files;

        if (!poblacion || !asunto || !mensaje) {
            Swal.fire("Campos incompletos", "Por favor complete todos los campos obligatorios.", "warning");
            return;
        }

        Swal.fire({
            title: "¿Deseas enviar el correo?",
            text: "El mensaje será enviado a la población seleccionada.",
            icon: "question",
            showCancelButton: true,
            confirmButtonColor: "#2D594D",
            cancelButtonColor: "#d33",
            confirmButtonText: "Sí, enviar",
            cancelButtonText: "Cancelar"
        }).then((result) => {
            if (result.isConfirmed) {
                let formData = new FormData();
                formData.append("Poblacion", poblacion);
                formData.append("Asunto", asunto);
                formData.append("Mensaje", mensaje);

                // ⬇️ Agregar los archivos con el nombre exacto "Archivos"
                if (archivos && archivos.length > 0) {
                    for (let i = 0; i < archivos.length; i++) {
                        formData.append("Archivos", archivos[i]);
                    }
                }

                // ⬇️ Agregar token antifalsificación
                formData.append("__RequestVerificationToken", $('input[name="__RequestVerificationToken"]').val());

                Swal.fire({
                    title: "Enviando correos...",
                    text: "Por favor espere mientras se envían los mensajes.",
                    allowOutsideClick: false,
                    didOpen: () => {
                        Swal.showLoading();
                    }
                });

                $.ajax({
                    url: '/Comunicados/EnviarCorreo',
                    type: 'POST',
                    data: formData,
                    processData: false,
                    contentType: false,
                    success: function (response) {
                        Swal.close();

                        if (response.ok) {
                            Swal.fire({
                                title: "¡Enviado!",
                                text: response.msg,
                                icon: "success",
                                timer: 2500,
                                showConfirmButton: false
                            });

                            $("#formEnviarCorreo")[0].reset();
                            $("#modalEnviarCorreo").modal("hide");
                        } else {
                            Swal.fire("Error", response.msg || "No se pudo enviar el correo.", "error");
                        }
                    },
                    error: function (xhr) {
                        Swal.close();
                        console.log(xhr.responseText);
                        Swal.fire("Error", "Ocurrió un error al intentar enviar el correo.", "error");
                    }
                });
            }
        });
    });

    $("#btnGuardarComunicado").click(function () {

        let formData = new FormData();
        formData.append("__RequestVerificationToken", $('input[name="__RequestVerificationToken"]').val());

        const Titulo = $("#TituloComunicado").val().trim();
        const Descripcion = $("#DescripcionComunicado").val().trim();
        const dirigidoA = $("#DirigidoAComunicado").val();

        let fechaInput = $("#FechaAplicacionComunicado").val();

        let files = $("#ArchivoDoc")[0].files;
        let extensionesPermitidas = ["pdf", "xls", "xlsx"];

        // Validación de título y descripción
        if (!Titulo || !Descripcion) {
            Swal.fire({
                title: 'Error',
                text: "Debes completar el título y la descripción.",
                icon: 'info',
                confirmButtonColor: '#2D594D'
            });
            return;
        }

        // Si no selecciona fecha se asigna la de hoy
        if (!fechaInput) {

            const hoy = new Date();
            const hoyFormato = hoy.toISOString().split('T')[0];

            fechaInput = hoyFormato;

            $("#FechaAplicacionComunicado").val(hoyFormato);

            Swal.fire({
                title: 'Fecha límite no seleccionada',
                text: "Se asignará automáticamente la fecha del día de hoy.",
                icon: 'info',
                confirmButtonColor: '#2D594D'
            });
        }

        // Validar que la fecha no sea menor a hoy
        const hoy = new Date();
        const hoyFormato =
            hoy.getFullYear() + "-" +
            String(hoy.getMonth() + 1).padStart(2, "0") + "-" +
            String(hoy.getDate()).padStart(2, "0");

        if (fechaInput < hoyFormato) {
            Swal.fire({
                title: 'Error',
                text: "La fecha límite no puede ser anterior al día de hoy.",
                icon: 'info',
                confirmButtonColor: '#2D594D'
            });
            return;
        }

        // Agregar datos al FormData
        formData.append("Titulo", Titulo);
        formData.append("Descripcion", Descripcion);
        formData.append("FechaAplicacion", fechaInput);
        formData.append("DirigidoA", dirigidoA);

        // Validación de archivos
        for (let i = 0; i < files.length; i++) {

            let nombreArchivo = files[i].name;
            let extension = nombreArchivo.split('.').pop().toLowerCase();

            if (!extensionesPermitidas.includes(extension)) {
                Swal.fire({
                    title: 'Archivo no permitido',
                    text: "Solo se permiten archivos PDF, XLS o XLSX.",
                    icon: 'error',
                    confirmButtonColor: '#2D594D'
                });
                return;
            }

            formData.append("archivos", files[i]);
        }

        // Envío AJAX
        $.ajax({
            url: '/Comunicados/CrearComunicado',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,

            success: function (res) {

                if (res.ok) {

                    Swal.fire({
                        title: 'Éxito',
                        text: res.msg,
                        icon: 'success',
                        confirmButtonColor: '#2D594D'
                    }).then(() => {

                        $("#modalAgregarComunicado").modal('hide');
                        location.reload();

                    });

                } else {

                    Swal.fire({
                        title: 'Error',
                        text: res.msg,
                        icon: 'error',
                        confirmButtonColor: '#d33'
                    });

                }

            },

            error: function () {

                Swal.fire({
                    title: 'Error',
                    text: "Ocurrió un error al guardar el comunicado.",
                    icon: 'error'
                });

            }

        });

    });
    

    $(document).on("click", ".btn-abrir-comunicado", function () {
        const id = $(this).data("id");
        const titulo = $(this).data("titulo");
        const descripcion = $(this).data("descripcion");
        const fecha = $(this).data("fecha");
        const aplicacion = $(this).data("aplicacion");
        const publicadoPor = $(this).data("publicado");
        const dirigido = $(this).data("dirigido");



        // Llenar los datos del modal principal
        $("#modalComunicadoUnicoLabel").text(titulo);
        $("#comunicadoDescripcion").text(descripcion);
        $("#comunicadoFecha").text(fecha);
        $("#comunicadoAplicacion").text(aplicacion);
        $("#comunicadoPublicadoPor").text(publicadoPor);
        $("#comunicadoDirigido").text(dirigido);

        // Llenar el modal de edición (si se abre luego)
        $("#IdComunicadoEditar").val(id);
        $("#TituloComunicadoEditar").val(titulo);
        $("#DescripcionComunicadoEditar").val(descripcion);
        $("#FechaPublicacionComunicadoEditar").val(fecha);
        $("#FechaAplicacionComunicadoEditar").val(aplicacion !== "N/A" ? aplicacion : "");
        $("#DirigidoAComunicadoEditar").val(dirigido);

        // Mostrar mensajes de carga
        const contenedorDocs = $("#comunicadoDocumentos");

        // Obtener documentos por AJAX
        $.ajax({
            url: '/Comunicados/ObtenerDocumentos',
            type: 'GET',
            data: { IdComunicado: id },
            success: function (response) {
                contenedorDocs.empty();

                if (!response.success) {
                    Swal.fire('Error', response.message || 'No se pudieron cargar los documentos.', 'error');
                    return;
                }

                const documentos = response.documentos;

                if (!documentos || documentos.length === 0) {
                    contenedorDocs.html('<div class="text-center text-muted">No hay documentos subidos.</div>');

                    return;
                }

                documentos.forEach(doc => {
                    // Plantilla del documento
                    const itemHtml = `
                    <div class="list-group-item d-flex justify-content-between align-items-center mb-2"
                         style="background-color: white; border: 1px solid #8CA653; border-radius: 8px;">
                        <div>
                            <strong>${doc.Nombre}</strong><br />
                            <small>Cargado: ${doc.FechaSubida}</small>
                        </div>
                        <div id="AccionesDocumentosComunicados" class="d-flex gap-3">
                           <a href="/Comunicados/DescargarDocumento?idDocumento=${doc.IdDocumento}" 
   title="Descargar" class="btn btn-link p-0 text-secondary">
   <i class="fas fa-download"></i>
</a>
                            ${typeof rolUsuario !== 'undefined' && rolUsuario === 2 ? `
                                <button class="btn btn-link p-0 text-secondary btnEliminarDocComunicado"
                                        data-id="${doc.IdDocumento}" title="Eliminar">
                                    <i class="fas fa-trash-alt"></i>
                                </button>` : ""}
                        </div>
                    </div>`;

                    // Agregar documento en ambos modales
                    contenedorDocs.append(itemHtml);
                });
            },
            error: function () {
                Swal.fire('Error', 'No se pudieron cargar los documentos.', 'error');
            }
        });

        $(document).on("click", ".btnEliminarComunicado", function () {

            Swal.fire({
                title: "¿Estás seguro?",
                text: "El comunicado será desactivado y no estará disponible para los usuarios.",
                icon: "warning",
                showCancelButton: true,
                confirmButtonColor: "#d33",
                cancelButtonColor: "#3085d6",
                confirmButtonText: "Sí, desactivar",
                cancelButtonText: "Cancelar"
            }).then((result) => {
                if (result.isConfirmed) {
                    $.ajax({
                        url: '/Comunicados/EliminarComunicado',
                        type: 'POST',
                        data: { IdComunicado: id },
                        success: function (response) {
                            if (response.ok) {
                                Swal.fire({
                                    title: "Desactivado",
                                    text: response.msg,
                                    icon: "success",
                                    timer: 2000,
                                    showConfirmButton: false
                                });


                                $("#modalComunicadoUnico").modal("hide");
                                location.reload();
                            } else {
                                Swal.fire('Error', response.msg || 'No se pudo desactivar el comunicado.', 'error');
                            }
                        },
                        error: function () {
                            Swal.fire('Error', 'Ocurrió un error al intentar desactivar el comunicado.', 'error');
                        }
                    });
                }
            });

        });

    });

  
        

    $(document).on("click", ".btnActualizarComunicado", function () {
            const id = $("#IdComunicadoEditar").val(); 
            const tituloEditado = $("#TituloComunicadoEditar").val().trim();
            const descripcionEditada = $("#DescripcionComunicadoEditar").val().trim();
            const fechaAplicacionEditada = $("#FechaAplicacionComunicadoEditar").val().trim();
        const dirigidoAEditado = $("#DirigidoAComunicadoEditar").val().trim();

        if (!tituloEditado || !descripcionEditada) {
            Swal.fire({
                title: "Campos incompletos",
                text: "Debe ingresar el título y la descripción del comunicado.",
                icon: "warning",
                confirmButtonColor: "#2D594D"
            });
            return;
        }

            let formData = new FormData();
            formData.append("IdComunicado", id);
            formData.append("Titulo", tituloEditado);
            formData.append("Descripcion", descripcionEditada);
            formData.append("FechaAplicacion", fechaAplicacionEditada);
            formData.append("DirigidoA", dirigidoAEditado);


            let files = $("#ArchivoDocEditar")[0].files;
            let extensionesPermitidas = ["pdf", "xls", "xlsx"];

            for (let i = 0; i < files.length; i++) {

                let nombreArchivo = files[i].name;
                let extension = nombreArchivo.split('.').pop().toLowerCase();

                if (!extensionesPermitidas.includes(extension)) {
                    Swal.fire({
                        title: "Archivo no permitido",
                        text: "Solo se permiten archivos PDF, XLS o XLSX.",
                        icon: "error",
                        confirmButtonColor: "#2D594D"
                    });
                    return;
                }

                formData.append("archivos", files[i]);
            } 


            Swal.fire({
                title: "¿Estás seguro?",
                text: "Los datos del comunicado serán actualizados.",
                icon: "warning",
                showCancelButton: true,
                confirmButtonColor: "#2D594D",
                cancelButtonColor: "#d33",
                confirmButtonText: "Sí, actualizar",
                cancelButtonText: "Cancelar"
            }).then((result) => {
                if (result.isConfirmed) {
                    $.ajax({
                        url: '/Comunicados/EditarComunicado',
                        type: 'POST',
                        data: formData,
                        processData: false, 
                        contentType: false, 
                        success: function (response) {
                            if (response.ok) {
                                Swal.fire({
                                    title: "Actualizado",
                                    text: response.msg,
                                    icon: "success",
                                    timer: 2000,
                                    showConfirmButton: false
                                });

                                $("#modalEditarComunicado").modal("hide");
                                setTimeout(() => location.reload(), 2000);
                            } else {
                                Swal.fire('Error', response.msg || 'No se pudo actualizar el comunicado.', 'error');
                            }
                        },
                        error: function () {
                            Swal.fire('Error', 'Ocurrió un error al intentar actualizar el comunicado.', 'error');
                        }
                    });
                }
            });
        });


     

        $(document).on('click', '.btnEliminarDocComunicado', function () {
            const idDocumento = $(this).data('id');

            Swal.fire({
                title: '¿Desea eliminar este documento?',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: 'Sí, eliminar',
                cancelButtonText: 'Cancelar',
                confirmButtonColor: '#d33',
                cancelButtonColor: '#3085d6'
            }).then((result) => {
                if (result.isConfirmed) {
                    $.ajax({
                        url: '/Comunicados/EliminarDocumento',
                        type: 'POST',
                        data: { idDocumento: idDocumento },
                        success: function (response) {
                            if (response.success) {
                                Swal.fire('Eliminado', response.message, 'success');
                                setTimeout(function () {
                                    location.reload();
                                }, 2000);
                            } else {
                                Swal.fire('Error', response.message, 'error');
                            }
                        },
                        error: function () {
                            Swal.fire('Error', 'Ocurrió un error al eliminar.', 'error');
                        }
                    });
                }
            });
        });

    });

