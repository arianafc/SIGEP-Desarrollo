$(function () {

    $("#btnGuardarComunicado").click(function () {
        let formData = new FormData();
        formData.append("__RequestVerificationToken", $('input[name="__RequestVerificationToken"]').val());
        formData.append("Titulo", $("#TituloComunicado").val());
        formData.append("Descripcion", $("#DescripcionComunicado").val());
        formData.append("FechaAplicacion", $("#FechaAplicacionComunicado").val());
        formData.append("DirigidoA", $("#DirigidoAComunicado").val());

        let files = $("#ArchivoDoc")[0].files;
        for (let i = 0; i < files.length; i++) {
            formData.append("archivos", files[i]);
        }

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
                alert("Error al guardar el comunicado.");
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

        $(document).on("click", ".btnActualizarComunicado", function () {
            const id = $("#IdComunicadoEditar").val(); // asegúrate de tener este input hidden en el modal
            const tituloEditado = $("#TituloComunicadoEditar").val().trim();
            const descripcionEditada = $("#DescripcionComunicadoEditar").val().trim();
            const fechaAplicacionEditada = $("#FechaAplicacionComunicadoEditar").val().trim();
            const dirigidoAEditado = $("#DirigidoAComunicadoEditar").val().trim();

            let formData = new FormData();
            formData.append("IdComunicado", id);
            formData.append("Titulo", tituloEditado);
            formData.append("Descripcion", descripcionEditada);
            formData.append("FechaAplicacion", fechaAplicacionEditada);
            formData.append("DirigidoA", dirigidoAEditado);


            let files = $("#ArchivoDocEditar")[0].files;
            for (let i = 0; i < files.length; i++) {
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
                            <a href="/Perfil/DescargarDocumento?ruta=${encodeURIComponent(doc.RutaArchivo)}&download=true"
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


        $(document).on('click', '.btnEliminarDocComunicado', function () {
            var idDocumento = $(this).data('id');

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
                                location.reload();
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

});




