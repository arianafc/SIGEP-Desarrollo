$(function () {

    $("#btnActualizarInfoAcademica").on("click", function (e) {
        e.preventDefault(); 

     
        let carrera = ($("#Carrera").val() || "").trim();
        let anno = ($("#AnnoGraduacion").val() || "").trim();
        let titulo = ($("#TituloObtenido").val() || "").trim();

       
        if (carrera === "" || anno === "" || titulo === "") {
            Swal.fire({
                icon: "warning",
                title: "Campos incompletos",
                text: "Por favor, completa todos los campos antes de actualizar."
            });
            return; 
        }

        
        Swal.fire({
            title: "¿Deseas actualizar tu información académica?",
            text: "Se guardarán los datos ingresados.",
            icon: "question",
            showCancelButton: true,
            confirmButtonText: "Sí, actualizar",
            cancelButtonText: "Cancelar"
        }).then((result) => {
            if (result.isConfirmed) {
                $("#ActualizarInfoAcademicaForm").submit();
            }
        });
    });

    $(document).on('click', '.btnEditarEspecialidad', function () {
        const idUsuarioEspecialidad = $(this).data('idusuarioespecialidad');
        const idEspecialidadActual = $(this).data('idespecialidad');

        if (!idUsuarioEspecialidad) {
            Swal.fire('Error', 'No se pudo identificar la especialidad a editar.', 'error');
            return;
        }

        $('#IdUsuarioEspecialidadEditar').val(idUsuarioEspecialidad);
        $('#IdEspecialidadEditar').val(idEspecialidadActual);

        // Abrir modal
        const modalEditar = new bootstrap.Modal(document.getElementById('modalEditarEspecialidad'));
        modalEditar.show();
    });

    $('#btnActualizarEspecialidad').on('click', function () {

        const idUsuarioEspecialidad = $('#IdUsuarioEspecialidadEditar').val();
        const idEspecialidad = $('#IdEspecialidadEditar').val();

        if (!idUsuarioEspecialidad) {
            Swal.fire('Error', 'No se encontró la especialidad seleccionada.', 'error');
            return;
        }

        if (!idEspecialidad) {
            Swal.fire('Campo requerido', 'Por favor seleccione una especialidad.', 'warning');
            return;
        }

        $.ajax({
            url: '/Perfil/ActualizarEspecialidad',
            type: 'POST',
            data: {
                IdEspecialidadUsuario: idUsuarioEspecialidad,
                IdEspecialidad: idEspecialidad
            },
            beforeSend: function () {
                Swal.fire({
                    title: 'Guardando cambios...',
                    text: 'Por favor espere.',
                    allowOutsideClick: false,
                    didOpen: () => Swal.showLoading()
                });
            },
            success: function (res) {
                if (res.success) {
                    Swal.fire({
                        icon: 'success',
                        title: 'Especialidad actualizada',
                        text: res.msg || 'La especialidad se actualizó correctamente.'
                    }).then(() => {
                        const modal = document.getElementById('modalEditarEspecialidad');
                        if (modal) {
                            const modalInstance = bootstrap.Modal.getInstance(modal) ||
                                new bootstrap.Modal(modal);
                            modalInstance.hide();
                        }
                        location.reload();
                    });
                } else {
                    Swal.fire('Atención', res.msg || 'No se pudo actualizar la especialidad.', 'warning');
                }
            },
            error: function (xhr, status, error) {
                console.error(error);
                Swal.fire('Error', 'Ocurrió un error al comunicarse con el servidor.', 'error');
            }
        });
    });


    $('#btnGuardarEspecialidad').on('click', function () {

        const ddl = $('#IdEspecialidad'); 
        const idEspecialidad = ddl.val();

        if (!idEspecialidad) {
            Swal.fire('Campo requerido', 'Por favor seleccione una especialidad.', 'warning');
            return;
        }

        $.ajax({
            url: '/Perfil/AgregarEspeciaidad',
            type: 'POST',
            data: { IdEspecialidad: idEspecialidad },
            beforeSend: function () {
                Swal.fire({
                    title: 'Guardando...',
                    text: 'Por favor espere.',
                    allowOutsideClick: false,
                    didOpen: () => Swal.showLoading()
                });
            },
            success: function (res) {
                if (res.success) {
                    Swal.fire({
                        icon: 'success',
                        title: 'Listo',
                        text: res.msg || 'Especialidad agregada con éxito.'
                    }).then(() => {
                        
                        const modal = document.getElementById('modalAgregarEspecialidad');
                        if (modal) {
                            const modalInstance = bootstrap.Modal.getInstance(modal) ||
                                new bootstrap.Modal(modal);
                            modalInstance.hide();
                        }
                        location.reload();
                    });
                } else {
                    Swal.fire('Atención', res.msg || 'No se pudo agregar la especialidad.', 'warning');
                }
            },
            error: function (xhr, status, error) {
                console.error(error);
                Swal.fire('Error', 'Ocurrió un error al comunicarse con el servidor.', 'error');
            }
        });
    });

    window.cambiarEstadoEspecialidad = function (idUsuarioEspecialidad) {

        if (!idUsuarioEspecialidad) {
            Swal.fire('Error', 'No se pudo identificar la especialidad.', 'error');
            return;
        }

        Swal.fire({
            title: '¿Está seguro?',
            text: 'Se cambiará el estado de esta especialidad.',
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: 'Sí, continuar',
            cancelButtonText: 'Cancelar'
        }).then((result) => {

            if (!result.isConfirmed) return;

            $.ajax({
                url: '/Perfil/CambioEstadoEspecialidad',
                type: 'POST',
                data: { IdUsuarioEspecialidad: idUsuarioEspecialidad },
                beforeSend: function () {
                    Swal.fire({
                        title: 'Procesando...',
                        text: 'Por favor espere.',
                        allowOutsideClick: false,
                        didOpen: () => Swal.showLoading()
                    });
                },
                success: function (res) {
                    if (res.success) {
                        Swal.fire({
                            icon: 'success',
                            title: 'Listo',
                            text: res.msg || 'Estado de la especialidad actualizado.'
                        }).then(() => {
                            location.reload();
                        });
                    } else {
                        Swal.fire('Atención', res.msg || 'No se pudo cambiar el estado.', 'warning');
                    }
                },
                error: function (xhr, status, error) {
                    console.error(error);
                    Swal.fire('Error', 'Ocurrió un error al comunicarse con el servidor.', 'error');
                }
            });
        });
    };

    $('#btnSubirDoc').on('click', function () {
        var archivo = $('#ArchivoDoc')[0].files[0];
        var idUsuario = $('#IdUsuarioDoc').val();

        if (!archivo) {
            Swal.fire({
                icon: 'warning',
                title: 'No hay archivo',
                text: 'Por favor seleccione un archivo antes de continuar.',
                confirmButtonColor: '#3085d6'
            });
            return;
        }

        // Validar extensión
        var extensionesPermitidas = ['.xls', '.xlsx', '.pdf', '.png', '.jpeg'];
        var extension = '.' + archivo.name.split('.').pop().toLowerCase();
        if (!extensionesPermitidas.includes(extension)) {
            Swal.fire({
                icon: 'error',
                title: 'Extensión inválida',
                text: 'Solo se permiten archivos .xls, .xlsx, .pdf, .jpeg o .png',
                confirmButtonColor: '#d33'
            });
            return;
        }

        var formData = new FormData();
        formData.append('archivo', archivo);
        formData.append('idUsuario', idUsuario);

        $.ajax({
            url: '/Perfil/SubirDocumento',
            type: 'POST',
            data: formData,
            contentType: false,
            processData: false,
            success: function (response) {
                if (response.success) {
                    Swal.fire({
                        title: 'Éxito',
                        text: response.message,
                        icon: 'success',
                        confirmButtonColor: '#2D594D'
                    }).then(() => {
                        $('#modalSubirDoc').modal('hide');
                        location.reload(); // recarga la página para mostrar el documento
                    });
                } else {
                    Swal.fire({
                        title: 'Error',
                        text: response.message,
                        icon: 'error',
                        confirmButtonColor: '#d33'
                    });
                }
            },
            error: function (xhr, status, error) {
                Swal.fire({
                    title: 'Error',
                    text: 'Ocurrió un error al subir el documento: ' + error,
                    icon: 'error',
                    confirmButtonColor: '#d33'
                });
            }
        });
    });

    function cargarDocumentos() {

        let idUsuario = $('#IdUsuarioDocumento').val();

        $.ajax({
            url: '/Perfil/ObtenerDocumentos',
            type: 'GET',
            data: { idUsuario: idUsuario },
            success: function (response) {
                var contenedor = $('#listaDocumentos');
                contenedor.empty();

                if (!response.success) {
                    Swal.fire('Error', response.message || 'No se pudieron cargar los documentos.', 'error');
                    return;
                }

                var documentos = response.documentos;

                if (!documentos || documentos.length === 0) {
                    contenedor.append('<div class="text-center text-muted">No hay documentos subidos.</div>');
                    return;
                }

                documentos.forEach(function (doc) {
                    var item = $(`
            <div class="list-group-item d-flex justify-content-between align-items-center" 
                 style="background-color: white; border: 1px solid #8CA653; border-radius: 8px; margin-bottom: 10px;">
                <div>
                    <strong>${doc.Nombre}</strong><br />
                    <small>Cargado: ${doc.FechaSubida}</small>
                </div>
                <div class="d-flex gap-3">

                 <a href="/Perfil/DescargarDocumento?idDocumento=${doc.IdDocumento}" 
                    title="Descargar" class="btn btn-link p-0 text-secondary">
                    <i class="fas fa-download"></i>
            </a>
                                <button class="btn btn-link p-0 text-secondary btnEliminarDoc" data-id="${doc.IdDocumento}" title="Eliminar">
                       <i class="fas fa-trash-alt"></i>
                    </button>
                </div>
            </div>
        `);
                    contenedor.append(item);
                });
            }
,
            error: function () {
                Swal.fire('Error', 'No se pudieron cargar los documentos.', 'error');
            }
        });
    }

    // Evento para eliminar
    $(document).on('click', '.btnEliminarDoc', function () {
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
                    url: '/Perfil/EliminarDocumento',
                    type: 'POST',
                    data: { idDocumento: idDocumento },
                    success: function (response) {
                        if (response.success) {
                            Swal.fire('Eliminado', response.message, 'success');
                            cargarDocumentos(); // recargar lista
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

    // Cargar documentos al abrir el modal
    $('#modalVerDocs').on('shown.bs.modal', function () {
        cargarDocumentos();
    });

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
        var telefonoPattern = /^\d{8}$/;
        if (!telefonoPattern.test(data.telefono)) {
            Swal.fire('Error', 'Ingrese un número de teléfono válido (8 dígitos).', 'error');
            return;
        }

        // Enviar datos por AJAX
        $.ajax({
            url: '/Perfil/AgregarEncargado',
            type: 'POST',
            data: data,
            success: function (response) {
                if (response.success) {
                    Swal.fire({
                        title: 'Éxito',
                        text: response.mensaje,
                        icon: 'success',
                        confirmButtonColor: '#2D594D'
                    }).then(() => {
                        window.location.href = '/Perfil/MiPerfil';
                    });
                } else {
                    Swal.fire({
                        title: 'Error',
                        text: response.mensaje,
                        icon: 'error',
                        confirmButtonColor: '#d33'
                    });
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

        if (!data.Cedula || !data.Nombre || !data.Apellido1 || !data.Apellido2 || !data.Telefono || !data.Correo || !data.Ocupacion || !data.LugarTrabajo || !data.Parentesco) {
            Swal.fire('Error', 'Por favor complete todos los campos obligatorios.', 'error');
            return;
        }

        // Validar correo electrónico
        var emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!emailPattern.test(data.Correo)) {
            Swal.fire('Error', 'Ingrese un correo electrónico válido.', 'error');
            return;
        }

        // Validar teléfono (solo números y mínimo 8 dígitos)
        var telefonoPattern = /^\d{8}$/;

        if (!telefonoPattern.test(data.Telefono)) {
            Swal.fire('Error', 'Ingrese un número de teléfono válido (8 dígitos).', 'error');
            return;
        }


        $.ajax({
            url: '/Perfil/ActualizarEncargado',
            type: 'POST',
            data: data,
            success: function (response) {
                if (response.success) {
                    $('#modalEditarEncargado').modal('hide');
                    Swal.fire({
                        title: 'Éxito',
                        text: response.mensaje,
                        icon: 'success',
                        confirmButtonColor: '#2D594D'
                    }).then(() => {
                        window.location.href = '/Perfil/MiPerfil';
                    });
                } else {
                    Swal.fire({
                        title: 'Error',
                        text: response.mensaje,
                        icon: 'error',
                        confirmButtonColor: '#d33'
                    });
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
                        Swal.fire({
                            title: 'Éxito',
                            text: response.mensaje,
                            icon: 'success',
                            confirmButtonColor: '#2D594D'
                        }).then(() => {
                            window.location.href = '/Perfil/MiPerfil';
                        });
                    } else {
                        Swal.fire({
                            title: 'Error',
                            text: response.mensaje,
                            icon: 'error',
                            confirmButtonColor: '#d33'
                        });
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
                        Swal.fire({
                            title: 'Éxito',
                            text: response.mensaje,
                            icon: 'success',
                            confirmButtonColor: '#2D594D'
                        }).then(() => {
                            window.location.href = '/Perfil/MiPerfil';
                        });
                    } else {
                        Swal.fire({
                            title: 'Error',
                            text: response.mensaje,
                            icon: 'error',
                            confirmButtonColor: '#d33'
                        });
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



$('#ActualizarPerfil').on('submit', function (e) {
    e.preventDefault(); // Evita envío automático

    let nombre = $('#NombrePerfil').val().trim();
    let apellido1 = $('#Apellido1Perfil').val().trim();
    let apellido2 = $('#Apellido2Perfil').val().trim();
    let cedula = $('#CedulaPerfil').val().trim();
    let telefono = $('#TelefonoPerfil').val().trim();
    let correo = $('#CorreoPersonalPerfil').val().trim();
    let direccion = $('#DireccionPerfil').val().trim();

    

    // Validar campos vacíos
    if (!nombre || !apellido1 || !apellido2 || !cedula || !telefono ||
        !correo || !direccion) {
        Swal.fire({
            icon: 'warning',
            title: 'Campos incompletos',
            text: 'Por favor complete todos los campos obligatorios antes de continuar.',
            confirmButtonColor: '#3085d6'
        });
        return;
    }

    // Validar correo electrónico
    let emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(correo)) {
        Swal.fire({
            icon: 'error',
            title: 'Correo inválido',
            text: 'Por favor ingrese un correo electrónico válido.',
            confirmButtonColor: '#3085d6'
        });
        return;
    }

    // Validar teléfono (solo números y mínimo 8 dígitos)
    var telefonoPattern = /^\d{8}$/;
    if (!telefonoPattern.test(telefono)) {
        Swal.fire('Error', 'Ingrese un número de teléfono válido (8 dígitos).', 'error');
        return;
    }


    const regexCedula = /^[0-9]+$/;

    if (!regexCedula.test(cedula)) {
        Swal.fire({
            icon: "error",
            title: "Cédula inválida",
            text: "La cédula solo puede contener números."
        });
        return;
    }

    const regexNombre = /^[A-Za-zÁÉÍÓÚáéíóúÑñ\s]+$/;

    if (!regexNombre.test(nombre)) {
        Swal.fire({
            icon: "error",
            title: "Nombre inválido",
            text: "El nombre solo puede contener letras."
        });
        return;
    }

    if (!regexNombre.test(apellido1)) {
        Swal.fire({
            icon: "error",
            title: "Apellido inválido",
            text: "El primer apellido solo puede contener letras."
        });
        return;
    }

    if (!regexNombre.test(apellido2)) {
        Swal.fire({
            icon: "error",
            title: "Apellido inválido",
            text: "El segundo apellido solo puede contener letras."
        });
        return;
    }



    // Confirmación antes de enviar
    Swal.fire({
        title: '¿Desea actualizar su información?',
        icon: 'question',
        showCancelButton: true,
        confirmButtonText: 'Sí, actualizar',
        cancelButtonText: 'Cancelar',
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33'
    }).then((result) => {
        if (result.isConfirmed) {
            e.currentTarget.submit();
        }
    });
});

