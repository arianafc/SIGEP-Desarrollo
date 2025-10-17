$(function () {
    var Contrasenna = $('#ContrasennaNueva');
    var ConfirmarContrasenna = $('#ConfirmarContrasenna');
    var BtnActualizar = $('#BtnActualizarContrasenna');
    var Form = $('#CambiarContrasennaForm');
 

    $('#modalEditarEncargado').on('hidden.bs.modal', function () {
        $('.modal-backdrop').remove();
        $('body').removeClass('modal-open');
     
    });

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
                        .then(() => {
                          
                            window.location.href = '/Perfil/MiPerfil';
                        });
                } else {
                 
                    Swal.fire('Error', response.mensaje, 'error');
                }
            },
            error: function () {
                Swal.fire('Error', 'No se pudo actualizar el encargado.', 'error');
            }
        });
    });

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
            e.preventDefault(); // evita que se mande si no pasa validación
            return false;
        }
    });
});


// Validación de info personal
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

// Reutilizable éxito
function mostrarSuccessMensaje() {
    Swal.fire({
        icon: 'success',
        title: 'Información guardada correctamente',
        showConfirmButton: false,
        timer: 2000
    });
}

["btnActualizarAcademica", "btnActualizarEncargado", "btnActualizarLaboral", "btnActualizarMedica", "btnSubirDoc"].forEach(id => {
    document.getElementById(id)?.addEventListener("click", mostrarSuccessMensaje);
});


function mostrarWarningMensaje() {
    Swal.fire({
        icon: 'warning',
        title: 'Ha ocurrido un error',
        showConfirmButton: false,
        timer: 2000
    });
}

["btnEliminarEncargado", "btnEliminarFile", "btnDescargarFile", "btnVerFile", "btnGuardarEncargado"].forEach(id => {
    document.getElementById(id)?.addEventListener("click", mostrarWarningMensaje);
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
                $('#modalEditarEncargado').modal('show');
                $('#modalVerEncargados').hide();
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
    if (confirm("¿Seguro que desea eliminar este encargado?")) {
        var data = {

            IdEncargado: $('#IdEncargado').val(),
          
        };

        $.ajax({
            url: '/Perfil/EliminarEncargado',
            type: 'POST',
            data: data,
            success: function (response) {
                if (response.success) {
                    
                    Swal.fire('Éxito', response.mensaje, 'success')
                        .then(() => {

                            window.location.href = '/Perfil/MiPerfil';
                        });
                } else {

                    Swal.fire('Error', response.mensaje, 'error');
                }
            },
            error: function () {
                Swal.fire('Error', 'No se pudo actualizar el encargado.', 'error');
            }
        });
    }
}