$(document).ready(function () {
    $("#BtnActualizarEstado").on("click", function (event) {
        event.preventDefault();

        // Obtener comentario
        let comentarioInput = $("#comentarioEstado");
        let comentario = comentarioInput.val().trim();

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

        // Obtener IdPractica y IdEstado del modal
        let idPractica = $("input[name='idPractica']").val();
        let idEstado = $("#nuevoEstado").val(); // <- Aquí capturas el select

        if (!idEstado) {
            Swal.fire({
                icon: "warning",
                title: "Estado requerido",
                text: "Debes seleccionar un nuevo estado.",
                confirmButtonColor: "#2D594D"
            });
            return;
        }

        // Mostrar indicador de carga
        Swal.fire({
            title: 'Actualizando estado...',
            allowOutsideClick: false,
            didOpen: () => {
                Swal.showLoading();
            }
        });

        // Llamada AJAX al controller
        $.ajax({
            url: '/Practicas/ActualizarEstadoPractica',
            type: 'POST',
            data: {
                idPractica: idPractica,
                idEstado: idEstado,
                comentario: comentario
            },
            success: function (response) {
                if (response.success) {
                    Swal.fire({
                        icon: "success",
                        title: "Estado actualizado",
                        text: response.message,
                        confirmButtonColor: "#2D594D"
                    }).then(() => {
                        $('#modalActualizarEstado').modal('hide');
                        comentarioInput.val('');
                        $('#nuevoEstado').val('');
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
                console.error('Error AJAX:', error);
                Swal.fire({
                    icon: "error",
                    title: "Error de conexión",
                    text: "No se pudo actualizar el estado. Inténtalo de nuevo.",
                    confirmButtonColor: "#2D594D"
                });
            }
        });
    });

    // Limpiar formulario al cerrar modal
    $('#modalActualizarEstado').on('hidden.bs.modal', function () {
        $('#comentarioEstado').val('');
        $('#nuevoEstado').val('');
    });
});
