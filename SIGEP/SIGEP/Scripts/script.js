$(document).ready(function () {
    function capitalizeWords(str) {
        return str
            ? str.toLowerCase().split(' ').map(word =>
                word.charAt(0).toUpperCase() + word.slice(1)
            ).join(' ')
            : "";
    }

    function ConsultarPersonaApi() {
        $("#nombre").val("");
        $("#apellido1").val("");
        $("#apellido2").val("");

        let identificacion = $("#cedula").val();

        if (identificacion.length >= 9) {
            $.ajax({
                type: 'GET',
                url: 'https://apis.gometa.org/cedulas/' + identificacion,
                dataType: 'json',
                success: function (data) {
                    if (data.resultcount > 0) {
                        let persona = data.results[0];
                        $("#nombre").val(capitalizeWords(persona.firstname1));
                        $("#apellido1").val(capitalizeWords(persona.lastname1));
                        $("#apellido2").val(capitalizeWords(persona.lastname2));
                    } else {
                        alert("No se encontró información para esa cédula");
                    }
                },
                error: function () {
                    alert("Error al consultar la API");
                }
            });
        }
    }
    $("#cedula").on("keyup", ConsultarPersonaApi);

           document.querySelectorAll('.btn-desasignar').forEach(btn => {
                btn.addEventListener('click', function (e) {
                    e.preventDefault();

                    const nombre = this.getAttribute('data-nombre');
                    const contenedor = this.closest('a');

                    Swal.fire({
                        title: '¿Está seguro?',
                        text: `¿Desea desasignar al estudiante ${nombre}?`,
                        icon: 'warning',
                        showCancelButton: true,
                        confirmButtonColor: '#2D594D', 
                        cancelButtonColor: '#d33',
                        confirmButtonText: 'Sí, desasignar',
                        cancelButtonText: 'Cancelar'
                    }).then((result) => {
                        if (result.isConfirmed) {
                          
                            contenedor.remove();

                            Swal.fire({
                                title: 'Desasignado',
                                text: `El estudiante ${nombre} ha sido desasignado.`,
                                icon: 'success',
                                confirmButtonColor: '#2D594D'
                            });
                        }
                    });
                });
           });

    $(".btn-desasignar-practica").on("click", function (e) {
        e.preventDefault();

        const btn = $(this);
        const fila = btn.closest("tr");
        const nombre = btn.data("nombre");
        const badgeEstado = fila.find("td:nth-child(5) .badge");

        // Verifica si el estado es "Rechazado"
        const estadoTexto = badgeEstado.text().trim().toLowerCase();
        if (estadoTexto === "rechazado") {
            Swal.fire({
                icon: 'info',
                title: 'No permitido',
                text: 'No se puede desasignar una práctica que ya fue rechazada.',
                confirmButtonColor: '#2D594D'
            });
            return;
        }

        Swal.fire({
            title: '¿Está seguro?',
            text: `¿Desea desasignar la práctica de ${nombre}?`,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#2D594D',
            cancelButtonColor: '#d33',
            confirmButtonText: 'Sí, desasignar',
            cancelButtonText: 'Cancelar'
        }).then((result) => {
            if (result.isConfirmed) {
               
                badgeEstado.text("Desasignada");
                badgeEstado.removeClass().addClass("badge-desasignada");

         
                btn.hide();

                Swal.fire({
                    icon: 'success',
                    title: 'Práctica desasignada',
                    text: `La práctica de ${nombre} ha sido marcada como desasignada.`,
                    confirmButtonColor: '#2D594D'
                });
            }
        });
    });

    $(".BtnDesasignarPracticaEstudiante").on("click", function () {
        const $badge = $(this).siblings(".badge");

        const estadoActual = $badge.text().trim();

        if (estadoActual === "Rechazado") {
            // No hacer nada si es rechazado
            return;
        }

        Swal.fire({
            title: "¿Deseas desasignar esta práctica?",
            text: "Esta acción cambiará el estado a 'Desasignada'.",
            icon: "warning",
            showCancelButton: true,
            confirmButtonText: "Sí, desasignar",
            cancelButtonText: "Cancelar",
            customClass: {
                confirmButton: 'btn-verde-personalizado'
            }
        }).then((result) => {
            if (result.isConfirmed) {
                // Cambiar texto y clases
                $badge
                    .text("Desasignada")
                    .removeClass()
                    .addClass("badge badge-no-asignada")
                    .css({ backgroundColor: "#9E9E9E", color: "white" });
                $(this).hide();
                Swal.fire({
                    title: "Desasignada",
                    text: "La práctica fue desasignada correctamente.",
                    icon: "success",
                    confirmButtonText: "OK",
                    customClass: {
                        confirmButton: 'btn-verde-personalizado'
                    }
                });
            }
        });
    });

});