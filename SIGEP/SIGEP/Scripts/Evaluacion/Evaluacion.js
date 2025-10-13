$(document).ready(function () {
    // Inicializar DataTable
    var table = $('#miTabla').DataTable({
        responsive: true,
        ajax: {
            url: '/Evaluacion/ObtenerEstudiantes',
            type: 'GET',
            dataSrc: ''
        },
        columns: [
            { data: 'Cedula' },
            { data: 'NombreCompleto' },
            { data: 'Especialidad' },
            { data: 'Telefono' },
            { data: 'PracticaAsignada' },
            {
                data: 'EstadoAcademico',
                render: function (data) {
                    return `<span class="badge badge-asignada">${data}</span>`;
                }
            },
            {
                data: 'NotaFinal',
                render: function (data) {
                    return data !== null ? data : '-';
                }
            },
            {
                data: null,
                render: function (data, type, row) {
                    var rol = $('#rolUsuario').val();
                    var botones = `
                        <button class="btn text-decoration-none bg-transparent VerPerfil" 
                                data-cedula="${row.Cedula}" 
                                data-idusuario="${row.IdUsuario}"
                                style="color: #2d594d">
                            <i class="fas fa-eye"></i>
                        </button>
                        <button class="btn Comentarios bg-transparent text-decoration-none" 
                                data-cedula="${row.Cedula}"
                                data-idusuario="${row.IdUsuario}"
                                data-nombre="${row.NombreCompleto}"
                                data-practica="${row.PracticaAsignada}"
                                style="color: #2d594d">
                            <i class="fas fa-comment"></i>
                        </button>
                    `;

                    if (rol == '3') { // Solo profesores
                        botones += `
                            <button class="btn btnEditarNota bg-transparent text-decoration-none" 
                                    data-cedula="${row.Cedula}"
                                    data-idusuario="${row.IdUsuario}"
                                    data-nombre="${row.NombreCompleto}"
                                    style="color: #2d594d;">
                                <i class="bi bi-pencil-square"></i>
                            </button>
                        `;
                    }
                    return botones;
                }
            }
        ],
        dom: 'Bfrtip',
        buttons: [
            {
                extend: 'excelHtml5',
                text: '<i class="fas fa-file-excel"></i> Exportar a Excel',
                className: 'btn btn-verde-personalizado btn-sm'
            },
            {
                extend: 'pdfHtml5',
                text: '<i class="fas fa-file-pdf"></i> Exportar a PDF',
                className: 'btn btn-verde-personalizado btn-sm',
                orientation: 'landscape',
                pageSize: 'A4'
            }
        ],
        language: {
            url: "//cdn.datatables.net/plug-ins/1.13.6/i18n/es-ES.json"
        }
    });

    // Filtros
    $('#filtroEstadoAcademico').on('change', function () {
        table.column(5).search(this.value).draw();
    });

    $('#filtroEspecialidad').on('keyup', function () {
        table.column(2).search(this.value).draw();
    });

    // Ver perfil del estudiante
    $('#miTabla').on('click', '.VerPerfil', function () {
        var idUsuario = $(this).data('idusuario');
        cargarPerfilEstudiante(idUsuario);
    });

    // Abrir modal de comentarios
    $('#miTabla').on('click', '.Comentarios', function () {
        var idUsuario = $(this).data('idusuario');
        var nombre = $(this).data('nombre');
        var cedula = $(this).data('cedula');
        var practica = $(this).data('practica');

        abrirModalComentarios(idUsuario, nombre, cedula, practica);
    });

    // Editar nota
    $('#miTabla').on('click', '.btnEditarNota', function () {
        var idUsuario = $(this).data('idusuario');
        var nombre = $(this).data('nombre');

        abrirModalNota(idUsuario, nombre);
    });

    // Guardar nota
    $('#btnGuardarNota').on('click', function () {
        guardarNota();
    });

    // Guardar comentario
    $('#btnGuardarComentario').on('click', function () {
        guardarComentario();
    });

    // Subir documento
    $('#btnSubirArchivo').on('click', function () {
        subirDocumento();
    });
});

// Función para cargar perfil del estudiante
function cargarPerfilEstudiante(idUsuario) {
    $.ajax({
        url: '/Evaluacion/ObtenerPerfilEstudiante',
        type: 'GET',
        data: { idUsuario: idUsuario },
        success: function (data) {
            if (data.success) {
                llenarModalPerfil(data.perfil);

                $('#btnSubirArchivo').data('idusuario', idUsuario);

                $('#modalPerfil').modal('show');
            } else {
                Swal.fire('Error', data.message, 'error');
            }
        },
        error: function () {
            Swal.fire('Error', 'No se pudo cargar el perfil del estudiante', 'error');
        }
    });
}

// Función para llenar el modal de perfil
// Función para llenar el modal de perfil
function llenarModalPerfil(perfil) {
    console.log('Datos recibidos:', perfil);

    // Información Personal
    var inputs = $('#modalPerfil .modal-body input[readonly]');

    // Primera fila
    $(inputs[0]).val(perfil.NombreCompleto || '');
    $(inputs[1]).val(perfil.Correo || '');

    // Segunda fila
    $(inputs[2]).val(perfil.Telefono || '');
    $(inputs[3]).val(perfil.Direccion || '');

    // Tercera fila
    $(inputs[4]).val(perfil.Sexo || '');
    $(inputs[5]).val(perfil.Especialidad || '');

    // Cuarta fila
    $(inputs[6]).val(perfil.Edad ? perfil.Edad + ' años' : '');
    $(inputs[7]).val(perfil.Seccion || '');

    // Información de la práctica
    $(inputs[8]).val(perfil.NombreEmpresa || '');
    $(inputs[9]).val(perfil.TelefonoEmpresa || '');

    // Retroalimentaciones
    var contenedor = $('#retroalimentacionComentarios').empty();
    if (perfil.Comentarios && perfil.Comentarios.length > 0) {
        perfil.Comentarios.forEach(function (comentario) {
            contenedor.append(`
                <div style="margin-bottom: 10px; padding: 10px; border-left: 3px solid #2D594D; background-color: #f8f9fa; border-radius: 4px;">
                    <div style="margin-bottom: 5px;">
                        <strong style="color: #2D594D;">${comentario.Autor}</strong> 
                        <small class="text-muted">
                            <i class="bi bi-clock"></i> ${comentario.Fecha}
                        </small>
                    </div>
                    <div>${comentario.Comentario}</div>
                </div>
            `);
        });
    } else {
        contenedor.html(`<p class="text-muted"><i class="bi bi-info-circle"></i> Sin comentarios registrados.</p>`);
    }
}

// Función para cargar perfil del estudiante
function cargarPerfilEstudiante(idUsuario) {
    $.ajax({
        url: '/Evaluacion/ObtenerPerfilEstudiante',
        type: 'GET',
        data: { idUsuario: idUsuario },
        success: function (data) {
            if (data.success) {
                llenarModalPerfil(data.perfil);
                $('#btnSubirArchivo').data('idusuario', idUsuario);

                // Cargar documentos de evaluación
                cargarDocumentosEvaluacion(idUsuario);

                $('#modalPerfil').modal('show');
            } else {
                Swal.fire('Error', data.message, 'error');
            }
        },
        error: function () {
            Swal.fire('Error', 'No se pudo cargar el perfil del estudiante', 'error');
        }
    });
}

// Función para cargar documentos de evaluación
function cargarDocumentosEvaluacion(idUsuario) {
    $.ajax({
        url: '/Evaluacion/ObtenerDocumentosEvaluacion',
        type: 'GET',
        data: { idUsuario: idUsuario },
        success: function (response) {
            var container = $('#evaluacionesContainer').empty();

            if (response.success && response.documentos && response.documentos.length > 0) {
                response.documentos.forEach(function (doc) {
                    var icono = obtenerIconoDocumento(doc.Extension);
                    var fechaFormateada = formatearFecha(doc.FechaSubida);

                    var docHtml = `
                        <div class="col-12 mb-2">
                            <div class="documento-item d-flex align-items-center justify-content-between p-3" 
                                 style="background-color: #f8f9fa; border-radius: 8px; border-left: 3px solid #2D594D;">
                                <div class="d-flex align-items-center flex-grow-1">
                                    <i class="${icono} me-3" style="font-size: 1.5rem; color: #2D594D;"></i>
                                    <div>
                                        <div class="fw-semibold">${doc.Nombre}</div>
                                        <small class="text-muted">
                                            <i class="bi bi-calendar3"></i> ${fechaFormateada}
                                        </small>
                                    </div>
                                </div>
                                <div class="d-flex gap-2">
                                    ${doc.Extension.toLowerCase() === '.pdf' ?
                            `<button class="btn btn-sm" 
                                                style="background-color: transparent; color: #2D594D;" 
                                                onclick="visualizarDocumento(${doc.IdDocumento})"
                                                title="Vista previa">
                                            <i class="fas fa-eye"></i>
                                        </button>` : ''}
                                    <button class="btn btn-sm" 
                                            style="background-color: transparent; color: #2D594D;" 
                                            onclick="descargarDocumento(${doc.IdDocumento})"
                                            title="Descargar">
                                        <i class="fas fa-download"></i>
                                    </button>
                                </div>
                            </div>
                        </div>
                    `;
                    container.append(docHtml);
                });
            } else {
                container.html(`
                    <div class="col-12">
                        <p class="text-muted text-center">
                            <i class="bi bi-info-circle"></i> No hay documentos de evaluación cargados.
                        </p>
                    </div>
                `);
            }
        },
        error: function () {
            $('#evaluacionesContainer').html(`
                <div class="col-12">
                    <p class="text-danger text-center">
                        <i class="bi bi-exclamation-triangle"></i> Error al cargar los documentos.
                    </p>
                </div>
            `);
        }
    });
}

// Función para obtener el ícono según la extensión
function obtenerIconoDocumento(extension) {
    switch (extension.toLowerCase()) {
        case '.pdf':
            return 'fas fa-file-pdf';
        case '.xlsx':
        case '.xls':
            return 'fas fa-file-excel';
        default:
            return 'fas fa-file';
    }
}

// Función para formatear fecha
function formatearFecha(fecha) {
    var date = new Date(fecha);
    var dia = String(date.getDate()).padStart(2, '0');
    var mes = String(date.getMonth() + 1).padStart(2, '0');
    var anio = date.getFullYear();
    var horas = String(date.getHours()).padStart(2, '0');
    var minutos = String(date.getMinutes()).padStart(2, '0');

    return `${dia}/${mes}/${anio} ${horas}:${minutos}`;
}

// Función para visualizar documento (solo PDFs)
function visualizarDocumento(idDocumento) {
    var url = '/Evaluacion/VisualizarDocumento?idDocumento=' + idDocumento;
    window.open(url, '_blank');
}

// Función para descargar documento
function descargarDocumento(idDocumento) {
    window.location.href = '/Evaluacion/DescargarDocumento?idDocumento=' + idDocumento;
}

// Función para abrir modal de comentarios
function abrirModalComentarios(idUsuario, nombre, cedula, practica) {
    $('#nombreEstudiante').text(nombre);
    $('#cedulaEstudiante').text(cedula);
    $('#practicaAsignada').text(practica);
    $('#btnGuardarComentario').data('idusuario', idUsuario);
    $('#nuevoComentario').val('');

    // Cargar comentarios anteriores
    $.ajax({
        url: '/Evaluacion/ObtenerComentarios',
        type: 'GET',
        data: { idUsuario: idUsuario },
        success: function (data) {
            var ul = $('#comentariosAnteriores').empty();
            if (data && data.length > 0) {
                data.forEach(function (comentario) {
                    ul.append(`
                        <li class="list-group-item" style="word-break: break-word; white-space: pre-wrap; background-color: #f8f9fa; border-radius: 8px; margin-bottom: 6px; padding: 10px;">
                            <div><strong>Comentario:</strong> ${comentario.Comentario}</div>
                            <small class="text-muted">
                                <i class="bi bi-person"></i> ${comentario.Autor} |
                                <i class="bi bi-clock"></i> ${comentario.Fecha}
                            </small>
                        </li>
                    `);
                });
            }
            $('#modalComentarios').modal('show');
        },
        error: function () {
            Swal.fire('Error', 'No se pudieron cargar los comentarios', 'error');
        }
    });
}

// Función para abrir modal de nota
function abrirModalNota(idUsuario, nombre) {
    $('#nombreEstudianteNota').text(nombre);
    $('#btnGuardarNota').data('idusuario', idUsuario);

    // Cargar notas actuales
    $.ajax({
        url: '/Evaluacion/ObtenerNotas',
        type: 'GET',
        data: { idUsuario: idUsuario },
        success: function (data) {
            $('#inputNota1').val(data.Nota1 || '');
            $('#inputNota2').val(data.Nota2 || '');
            calcularNotaFinal();
            $('#modalNota').modal('show');
        },
        error: function () {
            $('#inputNota1').val('');
            $('#inputNota2').val('');
            $('#inputNotaFinal').val('');
            $('#modalNota').modal('show');
        }
    });
}

// Función para calcular nota final automáticamente
function calcularNotaFinal() {
    var nota1 = parseFloat($('#inputNota1').val()) || 0;
    var nota2 = parseFloat($('#inputNota2').val()) || 0;
    var notaFinal = (nota1 + nota2) / 2;
    $('#inputNotaFinal').val(notaFinal.toFixed(2));
}

// Event listeners para calcular nota automáticamente
$(document).on('input', '#inputNota1, #inputNota2', function () {
    calcularNotaFinal();
});

// Función para guardar nota
function guardarNota() {
    var idUsuario = $('#btnGuardarNota').data('idusuario');
    var nota1 = parseFloat($('#inputNota1').val());
    var nota2 = parseFloat($('#inputNota2').val());

    if (isNaN(nota1) || nota1 < 0 || nota1 > 100) {
        Swal.fire('Advertencia', 'La Nota 1 debe estar entre 0 y 100', 'warning');
        return;
    }

    if (isNaN(nota2) || nota2 < 0 || nota2 > 100) {
        Swal.fire('Advertencia', 'La Nota 2 debe estar entre 0 y 100', 'warning');
        return;
    }

    var notaFinal = (nota1 + nota2) / 2;

    $.ajax({
        url: '/Evaluacion/GuardarNota',
        type: 'POST',
        data: {
            idUsuario: idUsuario,
            nota1: nota1,
            nota2: nota2,
            notaFinal: notaFinal
        },
        success: function (response) {
            if (response.success) {
                Swal.fire({
                    icon: 'success',
                    title: 'Nota registrada correctamente',
                    text: response.message,
                    timer: 2000,
                    showConfirmButton: false
                });

                $('#modalNota').modal('hide');
                $('#miTabla').DataTable().ajax.reload();
            } else {
                Swal.fire('Error', response.message, 'error');
            }
        },
        error: function () {
            Swal.fire('Error', 'No se pudo guardar la nota', 'error');
        }
    });
}

// Función para guardar comentario
function guardarComentario() {
    var idUsuario = $('#btnGuardarComentario').data('idusuario');
    var comentario = $('#nuevoComentario').val().trim();

    if (comentario === '') {
        Swal.fire('Advertencia', 'Debe escribir un comentario antes de guardar', 'warning');
        return;
    }

    $.ajax({
        url: '/Evaluacion/GuardarComentario',
        type: 'POST',
        data: {
            idUsuario: idUsuario,
            comentario: comentario
        },
        success: function (response) {
            if (response.success) {
                Swal.fire({
                    icon: 'success',
                    title: 'Comentario agregado exitosamente',
                    timer: 2000,
                    showConfirmButton: false
                });

                // Agregar el comentario a la lista
                $('#comentariosAnteriores').prepend(`
                    <li class="list-group-item" style="word-break: break-word; white-space: pre-wrap; background-color: #f8f9fa; border-radius: 8px; margin-bottom: 6px; padding: 10px;">
                        <div><strong>Comentario:</strong> ${comentario}</div>
                        <small class="text-muted">
                            <i class="bi bi-person"></i> ${response.autor} |
                            <i class="bi bi-clock"></i> ${response.fecha}
                        </small>
                    </li>
                `);

                $('#nuevoComentario').val('');
            } else {
                Swal.fire('Error', response.message, 'error');
            }
        },
        error: function () {
            Swal.fire('Error', 'No se pudo guardar el comentario', 'error');
        }
    });
}

// Función para subir documento
function subirDocumento() {
    var input = document.getElementById('inputArchivo');
    var archivo = input.files[0];
    var idUsuario = $('#btnSubirArchivo').data('idusuario');

    if (!archivo) {
        Swal.fire('Advertencia', 'Por favor seleccione un archivo', 'warning');
        return;
    }

    var extensionesValidas = ['.xls', '.xlsx', '.pdf'];
    var nombre = archivo.name.toLowerCase();
    var esValido = extensionesValidas.some(ext => nombre.endsWith(ext));

    if (!esValido) {
        Swal.fire('Error', 'Solo se permiten archivos .xls, .xlsx o .pdf', 'error');
        input.value = '';
        return;
    }

    var formData = new FormData();
    formData.append('archivo', archivo);
    formData.append('idUsuario', idUsuario);

    $.ajax({
        url: '/Evaluacion/SubirDocumento',
        type: 'POST',
        data: formData,
        processData: false,
        contentType: false,
        success: function (response) {
            if (response.success) {
                Swal.fire({
                    icon: 'success',
                    title: 'Documento subido correctamente',
                    timer: 2000,
                    showConfirmButton: false
                });
                input.value = '';
                $('#modalSubirDocumento').modal('hide');

                // Recargar lista de documentos
                cargarDocumentosEvaluacion(idUsuario);
            } else {
                Swal.fire('Error', response.message, 'error');
            }
        },
        error: function () {
            Swal.fire('Error', 'No se pudo subir el documento', 'error');
        }
    });
}