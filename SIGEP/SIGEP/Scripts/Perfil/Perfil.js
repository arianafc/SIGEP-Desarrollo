$(function () {

    var Contrasenna = $('#ContrasennaNueva');
    var ConfirmarContrasenna = $('#ConfirmarContrasenna');
    var Form = $('#CambiarContrasennaForm');

    $(".btnEditarEncargado").click(function () {
        var idEncargado = $(this).data('id'); 
        editarEncargado(idEncargado);
    });
  


    // ===================== AGREGAR ENCARGADO =====================
    $('#btnGuardarEncargado').on('click', function () {
        // Captura de datos del formulario
        var data = {
            cedula: $("#CedulaNuevoEncargado").val().trim(),
            nombre: $('#NombreNuevoEncargado').val().trim(),
            apellido1: $('#Apellido1NuevoEncargado').val().trim(),
            apellido2: $('#Apellido2NuevoEncargado').val().trim(),
            telefono: $('#TelefonoNuevoEncargado').val().trim(),
            correo: $('#CorreoNuevoEncargado').val().trim(),
            parentesco: $('#ParentescoNuevoEncargado').val().trim(),
            ocupacion: $('#OcupacionNuevoEncargado').val().trim(),
            lugarTrabajo: $('#ResidenciaNuevoEncargado').val().trim()
        };

        // Validaciones
        if (!data.cedula || !data.nombre || !data.apellido1 || !data.telefono || !data.correo || !data.parentesco) {
            Swal.fire('Error', 'Por favor complete todos los campos obligatorios.', 'error');
            return;
        }

        // Validar correo electrónico
        var emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!emailPattern.test(data.correo)) {
            Swal.fire('Error', 'Ingrese un correo electrónico válido.', 'error');
            return;
        }

        // Validar teléfono (solo números y mínimo 8 dígitos)
        var telefonoPattern = /^\d{8,}$/;
        if (!telefonoPattern.test(data.telefono)) {
            Swal.fire('Error', 'Ingrese un número de teléfono válido (mínimo 8 dígitos).', 'error');
            return;
        }

        // Enviar datos por AJAX
        $.ajax({
            url: '/Perfil/AgregarEncargado',
            type: 'POST',
            data: data,
            success: function (response) {
                if (response.success) {
                    Swal.fire('Éxito', response.mensaje, 'success')
                        .then(() => window.location.href = '/Perfil/MiPerfil');
                } else {
                    Swal.fire('Error', response.mensaje, 'error');
                }
            },
            error: function (error) {
                Swal.fire('Error', 'Error al agregar encargado: ' + error.responseText, 'error');
            }
        });
    });


    // ===================== ACTUALIZAR ENCARGADO =====================
    $('#btnActualizarEncargado').click(function () {
        var data = {
            IdEncargado: $('#IdEncargado').val(),
            Nombre: $('#NombreEditar').val(),
            Apellido1: $('#Apellido1Editar').val(),
            Apellido2: $('#Apellido2Editar').val(),
            Telefono: $('#TelefonoEditar').val(),
            Parentesco: $('#ParentescoEditar').val(),
            LugarTrabajo: $('#LugarTrabajoEditar').val(),
            Ocupacion: $('#OcupacionEditar').val(),
            Correo: $('#CorreoEditar').val(),
            Cedula: $('#CedulaEditar').val()
        };

        $.ajax({
            url: '/Perfil/ActualizarEncargado',
            type: 'POST',
            data: data,
            success: function (response) {
                if (response.success) {
                    $('#modalEditarEncargado').modal('hide');
                    Swal.fire('Éxito', response.mensaje, 'success')
                        .then(() => window.location.href = '/Perfil/MiPerfil');
                } else {
                    Swal.fire('Error', response.mensaje, 'error');
                }
            },
            error: function () {
                Swal.fire('Error', 'No se pudo actualizar el encargado.', 'error');
            }
        });
    });


    $('#modalEditarEncargado').on('hidden.bs.modal', function () {
        $('#modalVerEncargados').modal('hide');
    });
    // ===================== VALIDAR CONTRASEÑA =====================
    function validarContrasenna() {
        let pass = Contrasenna.val().trim();
        let confirm = ConfirmarContrasenna.val().trim();

        if (pass.length < 8) {
            Swal.fire({
                icon: 'warning',
                title: 'Contraseña muy corta',
                text: 'La contraseña debe tener al menos 8 caracteres.',
                confirmButtonColor: '#8CA653'
            });
            return false;
        }

        if (pass !== confirm) {
            Swal.fire({
                icon: 'warning',
                title: 'Las contraseñas no coinciden',
                text: 'Por favor, verifique que ambas contraseñas sean iguales.',
                confirmButtonColor: '#8CA653'
            });
            return false;
        }

        return true;
    }

    Form.on("submit", function (e) {
        if (!validarContrasenna()) {
            e.preventDefault();
            return false;
        }
    });

    // ===================== VALIDACIÓN DE INFO PERSONAL =====================
    document.getElementById("btnActualizarInfo")?.addEventListener("click", function () {
        const campos = document.querySelectorAll('#info-personal input');
        let incompletos = false;

        campos.forEach(input => {
            if (input.value.trim() === "") {
                incompletos = true;
            }
        });

        if (incompletos) {
            Swal.fire({
                icon: 'warning',
                title: 'Campos incompletos',
                text: 'Por favor complete todos los campos obligatorios',
                confirmButtonColor: '#8CA653'
            });
        } else {
            Swal.fire({
                icon: 'success',
                title: 'Información actualizada correctamente',
                showConfirmButton: false,
                timer: 2000
            });
        }
    });

    // ===================== MENSAJES REUTILIZABLES =====================
    function mostrarSuccessMensaje() {
        Swal.fire({
            icon: 'success',
            title: 'Información guardada correctamente',
            showConfirmButton: false,
            timer: 2000
        });
    }



    // ===================== EDITAR ENCARGADO =====================
   

    // ===================== ELIMINAR ENCARGADO =====================
  

    // ===================== LIMPIAR MODAL =====================
   
});
function editarEncargado(idEncargado) {
    $.ajax({
        url: '/Perfil/ObtenerEncargadoPorId',
        type: 'GET',
        data: { idEncargado: idEncargado },
        success: function (data) {
            if (data && !data.error) {
                $('#IdEncargado').val(data.IdEncargado);
                $('#CedulaEditar').val(data.Cedula);
                $('#NombreEditar').val(data.Nombre);
                $('#Apellido1Editar').val(data.Apellido1);
                $('#Apellido2Editar').val(data.Apellido2);
                $('#TelefonoEditar').val(data.Telefono);
                $('#ParentescoEditar').val(data.Parentesco);
                $('#LugarTrabajoEditar').val(data.LugarTrabajo);
                $('#OcupacionEditar').val(data.Ocupacion);
                $('#CorreoEditar').val(data.Correo);
                $('#modalVerEncargados').modal('hide');
                $('#modalEditarEncargado').modal('show');


            } else {
                Swal.fire('Error', data.mensaje || 'No se pudo obtener la información del encargado.', 'error');
            }
        },
        error: function () {
            Swal.fire('Error', 'No se pudo conectar con el servidor.', 'error');
        }
    });
}

function eliminarEncargado(idEncargado) {
    Swal.fire({
        title: '¿Está seguro?',
        text: "¿Seguro que desea desactivar este encargado?",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33',
        confirmButtonText: 'Sí, desactivar',
        cancelButtonText: 'Cancelar'
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: '/Perfil/EliminarEncargado',
                type: 'POST',
                data: { IdEncargado: idEncargado },
                success: function (response) {
                    if (response.success) {
                        Swal.fire('Éxito', response.mensaje, 'success')
                            .then(() => window.location.href = '/Perfil/MiPerfil');
                    } else {
                        Swal.fire('Error', response.mensaje, 'error');
                    }
                },
                error: function () {
                    Swal.fire('Error', 'No se pudo eliminar el encargado.', 'error');
                }
            });
        }
    });
}


function activarEncargado(idEncargado) {
    Swal.fire({
        title: '¿Está seguro?',
        text: "¿Seguro que desea activar este encargado?",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33',
        confirmButtonText: 'Sí, activar',
        cancelButtonText: 'Cancelar'
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: '/Perfil/ActivarEncargado',
                type: 'POST',
                data: { IdEncargado: idEncargado },
                success: function (response) {
                    if (response.success) {
                        Swal.fire('Éxito', response.mensaje, 'success')
                            .then(() => window.location.href = '/Perfil/MiPerfil');
                    } else {
                        Swal.fire('Error', response.mensaje, 'error');
                    }
                },
                error: function () {
                    Swal.fire('Error', 'No se pudo activar el encargado.', 'error');
                }
            });
        }
    });
}



$('#CedulaNuevoEncargado').on('blur', function () {
    let cedula = $(this).val().trim();

    if (cedula === '') return; // Evita llamadas vacías

    $.ajax({
        url: '/Perfil/ObtenerEncargadoPorCedula',
        type: 'GET',
        data: { Cedula: cedula },
        success: function (response) {
            if (response.success && response.data) {
                let e = response.data;

                // Autorellena los campos
                $('#NombreNuevoEncargado').val(e.Nombre);
                $('#Apellido1NuevoEncargado').val(e.Apellido1);
                $('#Apellido2NuevoEncargado').val(e.Apellido2);
                $('#TelefonoNuevoEncargado').val(e.Telefono);
                $('#CorreoNuevoEncargado').val(e.Correo);
                $('#ParentescoNuevoEncargado').val(''); // se deja para que el estudiante lo indique
                $('#OcupacionNuevoEncargado').val(e.Ocupacion);
                $('#ResidenciaNuevoEncargado').val(e.LugarTrabajo);

                // Notificación visual (opcional)
                Swal.fire({
                    icon: 'info',
                    title: 'Encargado encontrado',
                    text: 'Se han autocompletado los datos del encargado.',
                    timer: 2000,
                    showConfirmButton: false
                });
            } else {
                // Si no existe, limpia el formulario
                $('#NombreNuevoEncargado, #Apellido1NuevoEncargado, #Apellido2NuevoEncargado, #TelefonoNuevoEncargado, #CorreoNuevoEncargado, #ParentescoNuevoEncargado, #OcupacionNuevoEncargado, #ResidenciaNuevoEncargado').val('');

                Swal.fire({
                    icon: 'warning',
                    title: 'No encontrado',
                    text: 'No existe un encargado con esa cédula. Puede registrarlo nuevo.',
                    timer: 2500,
                    showConfirmButton: false
                });
            }
        },
        error: function () {
            Swal.fire({
                icon: 'error',
                title: 'Error de conexión',
                text: 'No se pudo consultar la cédula. Intente nuevamente.'
            });
        }
    });
});

