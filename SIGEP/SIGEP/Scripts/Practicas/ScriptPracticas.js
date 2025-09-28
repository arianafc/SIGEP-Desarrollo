$(document).ready(function () {
    // Función para agregar comentario
    $("#BtnAgregarComentario").on("click", function (event) {
        event.preventDefault();

        let comentarioInput = $("#ComentarioProceso");
        let comentario = comentarioInput.val().trim();

        if (comentario === "") {
            Swal.fire({
                icon: "warning",
                title: "Comentario requerido",
                text: "Debes escribir un comentario antes de continuar.",
                confirmButtonColor: "#2D594D"
            });
            comentarioInput.focus();
            return;
        }

        let idVacante = $("input[name='idVacante']").val();
        let idUsuario = $("input[name='idUsuario']").val();

        if (!idVacante || !idUsuario) {
            Swal.fire({
                icon: "error",
                title: "Error",
                text: "No se pudieron obtener los datos necesarios. Recarga la página e inténtalo de nuevo.",
                confirmButtonColor: "#2D594D"
            });
            return;
        }

        Swal.fire({
            title: 'Guardando comentario...',
            allowOutsideClick: false,
            didOpen: () => {
                Swal.showLoading();
            }
        });

        $.ajax({
            url: '/Practicas/AgregarComentario',
            type: 'POST',
            data: {
                idVacante: idVacante,
                idUsuario: idUsuario,
                comentario: comentario
            },
            success: function (response) {
                if (response && response.success) {
                    Swal.fire({
                        icon: "success",
                        title: "Comentario agregado",
                        text: response.message || "Comentario agregado correctamente",
                        confirmButtonColor: "#2D594D"
                    }).then(() => {
                        $('#modalAgregarComentario').modal('hide');
                        comentarioInput.val('');
                        window.location.reload();
                    });
                } else {
                    Swal.fire({
                        icon: "error",
                        title: "Error",
                        text: response.message || "No se pudo agregar el comentario",
                        confirmButtonColor: "#2D594D"
                    });
                }
            },
            error: function (xhr, status, error) {
                let errorMessage = "No se pudo guardar el comentario. Inténtalo de nuevo.";
                if (xhr.status === 404) {
                    errorMessage = "La función no está disponible. Contacta al administrador.";
                } else if (xhr.status === 500) {
                    errorMessage = "Error en el servidor. Inténtalo más tarde.";
                }

                Swal.fire({
                    icon: "error",
                    title: "Error de conexión",
                    text: errorMessage,
                    confirmButtonColor: "#2D594D"
                });
            }
        });
    });

    // Función para actualizar estado
    $("#BtnActualizarEstado").on("click", function (event) {
        event.preventDefault();

        let comentarioInput = $("#comentarioEstado");
        let estadoSelect = $("#nuevoEstado");
        let comentario = comentarioInput.val().trim();
        let nuevoEstado = estadoSelect.val();

        if (nuevoEstado === "" || nuevoEstado === null) {
            Swal.fire({
                icon: "warning",
                title: "Estado requerido",
                text: "Debes seleccionar un nuevo estado.",
                confirmButtonColor: "#2D594D"
            });
            estadoSelect.focus();
            return;
        }

        if (comentario === "") {
            Swal.fire({
                icon: "warning",
                title: "Comentario requerido",
                text: "Debes escribir un comentario antes de actualizar el estado.",
                confirmButtonColor: "#2D594D"
            });
            comentarioInput.focus();
            return;
        }

        let idPractica = $("input[name='idPractica']").val();

        if (!idPractica) {
            Swal.fire({
                icon: "error",
                title: "Error",
                text: "No se pudo obtener la información de la práctica. Recarga la página e inténtalo de nuevo.",
                confirmButtonColor: "#2D594D"
            });
            return;
        }

        Swal.fire({
            title: 'Actualizando estado...',
            text: 'Se enviará un correo de notificación al estudiante',
            allowOutsideClick: false,
            didOpen: () => {
                Swal.showLoading();
            }
        });

        $.ajax({
            url: '/Practicas/ActualizarEstadoPractica',
            type: 'POST',
            data: {
                idPractica: idPractica,
                idEstado: nuevoEstado,
                comentario: comentario
            },
            success: function (response) {
                if (response && response.success) {
                    Swal.fire({
                        icon: "success",
                        title: "Estado actualizado",
                        html: `<p>${response.message}</p>
                               <small><strong>Nuevo estado:</strong> ${response.data?.estado || ''}</small>`,
                        confirmButtonColor: "#2D594D"
                    }).then(() => {
                        $('#modalActualizarEstado').modal('hide');
                        window.location.reload();
                    });
                } else {
                    Swal.fire({
                        icon: "error",
                        title: "Error",
                        text: response.message || "No se pudo actualizar el estado",
                        confirmButtonColor: "#2D594D"
                    });
                }
            },
            error: function (xhr, status, error) {
                let errorMessage = "No se pudo actualizar el estado. Inténtalo de nuevo.";
                if (xhr.status === 404) {
                    errorMessage = "La función no está disponible. Contacta al administrador.";
                } else if (xhr.status === 500) {
                    errorMessage = "Error en el servidor. Inténtalo más tarde.";
                }

                Swal.fire({
                    icon: "error",
                    title: "Error de conexión",
                    text: errorMessage,
                    confirmButtonColor: "#2D594D"
                });
            }
        });
    });

    // Limpiar formularios cuando se cierren los modales
    $('#modalAgregarComentario').on('hidden.bs.modal', function () {
        $('#ComentarioProceso').val('');
    });

    $('#modalActualizarEstado').on('hidden.bs.modal', function () {
        $('#comentarioEstado').val('');
        $('#nuevoEstado').val('');
    });
});