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
                    var badgeClass = data === 'Aprobado' ? 'badge-aprobada' :
                        data === 'Rezagado' ? 'badge-rezagado' :
                            'badge-secondary';
                    return `<span class="badge ${badgeClass}">${data}</span>`;
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
                extend: 'print',
                text: '<i class="fas fa-print"></i> Imprimir',
                className: 'btn btn-verde-personalizado btn-sm'
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

    // Información de la práctica - Solo enlace
    var practicaContainer = $('#infoPracticaContainer').empty();

    if (perfil.NombreEmpresa && perfil.IdVacante && perfil.IdUsuario) {
        var urlVisualizacion = '/Practicas/VisualizacionPostulacion?idVacante=' + perfil.IdVacante + '&idUsuario=' + perfil.IdUsuario;

        practicaContainer.html(`
            <div class="col-md-12">
                <a href="${urlVisualizacion}" 
                   class="d-flex justify-content-between align-items-center p-3 text-decoration-none"
                   style="background-color: #f8f9fa; border-radius: 8px; border-left: 4px solid #2D594D; color: #2D594D;">
                    <span style="font-weight: 600;">${perfil.NombreEmpresa}</span>
                    <span class="badge badge-en-curso">En Curso</span>
                </a>
            </div>
        `);
    } else {
        practicaContainer.html(`
            <div class="col-md-12">
                <div class="p-3 text-center" style="background-color: #f8f9fa; border-radius: 8px; border: 2px dashed #dee2e6;">
                    <p class="text-muted mb-0">
                        <i class="bi bi-info-circle"></i> No tiene práctica asignada
                    </p>
                </div>
            </div>
        `);
    }

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
                    var fechaFormateada = doc.FechaSubida;

                    var docHtml = `
                        <div class="mb-3">
                            <div class="documento-item d-flex align-items-center justify-content-between p-3" 
                                 style="background-color: #f8f9fa; border-radius: 8px; border-left: 4px solid #2D594D;">
                                <div class="d-flex align-items-center flex-grow-1" style="min-width: 0;">
                                    <i class="${icono} me-3" style="font-size: 1.8rem; color: #2D594D; flex-shrink: 0;"></i>
                                    <div style="min-width: 0; flex: 1;">
                                        <div class="fw-semibold text-truncate" style="color: #2D594D;">${doc.Nombre}</div>
                                        <small class="text-muted">
                                            <i class="bi bi-calendar3"></i> ${fechaFormateada}
                                        </small>
                                    </div>
                                </div>
                                <div class="d-flex gap-2 ms-3" style="flex-shrink: 0;">
                                    ${doc.Extension.toLowerCase() === '.pdf' ?
                            `<button class="btn btn-sm" 
                                                style="background-color: transparent; color: #2D594D; border: 1px solid #2D594D;" 
                                                onclick="visualizarDocumento(${doc.IdDocumento})"
                                                title="Vista previa">
                                            <i class="fas fa-eye"></i>
                                        </button>` : ''}
                                    <button class="btn btn-sm" 
                                            style="background-color: transparent; color: #2D594D; border: 1px solid #2D594D;" 
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
                    <div class="text-center py-4" style="background-color: #f8f9fa; border-radius: 8px; border: 2px dashed #dee2e6;">
                        <i class="bi bi-folder-x" style="font-size: 3rem; color: #6c757d;"></i>
                        <p class="text-muted mt-2 mb-0">No hay documentos de evaluación cargados.</p>
                    </div>
                `);
            }
        },
        error: function () {
            $('#evaluacionesContainer').html(`
                <div class="alert alert-danger" role="alert">
                    <i class="bi bi-exclamation-triangle me-2"></i>
                    Error al cargar los documentos.
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
    $('#contadorCaracteres').text('0'); // Resetear contador

    // Cargar comentarios anteriores
    $.ajax({
        url: '/Evaluacion/ObtenerComentarios',
        type: 'GET',
        data: { idUsuario: idUsuario },
        success: function (data) {
            var container = $('#comentariosAnteriores').empty();
            if (data && data.length > 0) {
                data.forEach(function (comentario) {
                    container.append(`
                        <div class="mb-2" style="background-color: #f8f9fa; border-left: 3px solid #2D594D; border-radius: 4px; padding: 6px 10px;">
                            <div class="d-flex justify-content-between align-items-center" style="margin-bottom: 4px;">
                                <strong style="color: #2D594D; font-size: 0.8rem;">
                                    <i class="bi bi-person-circle"></i> ${comentario.Autor}
                                </strong>
                                <small class="text-muted" style="font-size: 0.7rem;">
                                    <i class="bi bi-clock"></i> ${comentario.Fecha}
                                </small>
                            </div>
                            <div style="font-size: 0.8rem; line-height: 1.3; color: #495057;">${comentario.Comentario}</div>
                        </div>
                    `);
                });
            } else {
                container.html(`
                    <p class="text-muted text-center py-2" style="font-size: 0.85rem;">
                        <i class="bi bi-info-circle"></i> No hay comentarios anteriores
                    </p>
                `);
            }
            $('#modalComentarios').modal('show');
        },
        error: function () {
            Swal.fire('Error', 'No se pudieron cargar los comentarios', 'error');
        }
    });
}

// Función para calcular nota final automáticamente
function calcularNotaFinal() {
    var nota1 = $('#inputNota1').val();
    var nota2 = $('#inputNota2').val();

    // Solo calcular si ambas notas tienen valor (incluyendo cero)
    if (nota1 !== '' && nota2 !== '') {
        var n1 = parseFloat(nota1);
        var n2 = parseFloat(nota2);
        var notaFinal = (n1 + n2) / 2;
        $('#inputNotaFinal').val(notaFinal.toFixed(2));
    } else {
        $('#inputNotaFinal').val('');
    }
}

// Event listeners para calcular nota automáticamente
$(document).on('input', '#inputNota1, #inputNota2', function () {
    calcularNotaFinal();
});

// Función para guardar nota
function guardarNota() {
    var idUsuario = $('#btnGuardarNota').data('idusuario');
    var nota1Input = $('#inputNota1').val();
    var nota2Input = $('#inputNota2').val();

    // Validar que al menos una nota esté ingresada
    if (nota1Input === '' && nota2Input === '') {
        Swal.fire('Advertencia', 'Debe ingresar al menos una nota', 'warning');
        return;
    }

    // Convertir a valores numéricos o null
    var nota1 = nota1Input !== '' ? parseFloat(nota1Input) : null;
    var nota2 = nota2Input !== '' ? parseFloat(nota2Input) : null;

    // Validar rangos solo si la nota fue ingresada
    if (nota1 !== null && (nota1 < 0 || nota1 > 100)) {
        Swal.fire('Advertencia', 'La Nota 1 debe estar entre 0 y 100', 'warning');
        return;
    }

    if (nota2 !== null && (nota2 < 0 || nota2 > 100)) {
        Swal.fire('Advertencia', 'La Nota 2 debe estar entre 0 y 100', 'warning');
        return;
    }

    // Calcular nota final solo si ambas notas existen
    var notaFinal = null;
    if (nota1 !== null && nota2 !== null) {
        notaFinal = (nota1 + nota2) / 2;
    }

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
            // Mostrar nota1 (incluso si es 0)
            if (data.Nota1 !== null && data.Nota1 !== undefined) {
                $('#inputNota1').val(data.Nota1);
            } else {
                $('#inputNota1').val('');
            }

            // Mostrar nota2 (incluso si es 0)
            if (data.Nota2 !== null && data.Nota2 !== undefined) {
                $('#inputNota2').val(data.Nota2);
            } else {
                $('#inputNota2').val('');
            }

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

// Función para guardar comentario
function guardarComentario() {
    var idUsuario = $('#btnGuardarComentario').data('idusuario');
    var comentario = $('#nuevoComentario').val().trim();

    if (comentario === '') {
        Swal.fire('Advertencia', 'Debe escribir un comentario antes de guardar', 'warning');
        return;
    }

    // Validación de longitud
    if (comentario.length > 255) {
        Swal.fire('Advertencia', 'El comentario no puede exceder los 255 caracteres', 'warning');
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
                // Agregar el comentario con padding muy pequeño
                $('#comentariosAnteriores').prepend(`
                    <div class="mb-2" style="background-color: #f8f9fa; border-left: 3px solid #2D594D; border-radius: 4px; padding: 6px 10px;">
                        <div class="d-flex justify-content-between align-items-center" style="margin-bottom: 4px;">
                            <strong style="color: #2D594D; font-size: 0.8rem;">
                                <i class="bi bi-person-circle"></i> ${response.autor}
                            </strong>
                            <small class="text-muted" style="font-size: 0.7rem;">
                                <i class="bi bi-clock"></i> ${response.fecha}
                            </small>
                        </div>
                        <div style="font-size: 0.8rem; line-height: 1.3; color: #495057;">${comentario}</div>
                    </div>
                `);
                $('#nuevoComentario').val('');
                $('#contadorCaracteres').text('0'); // Resetear contador
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

// Contador de caracteres para el comentario
$(document).on('input', '#nuevoComentario', function () {
    var longitud = $(this).val().length;
    $('#contadorCaracteres').text(longitud);

    // Cambiar color según la cantidad de caracteres
    if (longitud >= 255) {
        $('#contadorCaracteres').css('color', '#dc3545'); // Rojo cuando llega al límite
        $('#contadorCaracteres').css('font-weight', 'bold');
    } else if (longitud >= 230) {
        $('#contadorCaracteres').css('color', '#ffc107'); // Amarillo cuando está cerca
        $('#contadorCaracteres').css('font-weight', 'normal');
    } else {
        $('#contadorCaracteres').css('color', '#6c757d'); // Gris normal
        $('#contadorCaracteres').css('font-weight', 'normal');
    }
});

// Validar al pegar texto
$(document).on('paste', '#nuevoComentario', function (e) {
    var pastedText = (e.originalEvent || e).clipboardData.getData('text/plain');
    var currentText = $(this).val();
    var maxLength = 255;

    // Si el texto pegado más el actual excede el límite, truncar
    if ((currentText + pastedText).length > maxLength) {
        e.preventDefault();
        var remainingLength = maxLength - currentText.length;
        var truncatedText = pastedText.substring(0, remainingLength);

        // Insertar el texto truncado
        var textarea = this;
        var startPos = textarea.selectionStart;
        var endPos = textarea.selectionEnd;
        textarea.value = currentText.substring(0, startPos) + truncatedText + currentText.substring(endPos);

        // Actualizar contador
        $('#contadorCaracteres').text(textarea.value.length);

        Swal.fire({
            icon: 'warning',
            title: 'Texto truncado',
            text: 'El texto pegado excedía el límite de 255 caracteres y fue recortado.',
            timer: 3000,
            showConfirmButton: false
        });
    }
});