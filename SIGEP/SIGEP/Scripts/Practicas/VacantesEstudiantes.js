// ~/Scripts/Practicas/VistaVacantes.js
(function ($) {
    $(function () {
        const CFG = window.VacantesCfg || { urls: {}, rol: 0 };

        // ===============================
        // Helpers
        // ===============================
        function redirSiLogin(res, xhr) {
            try {
                var ct = (xhr && xhr.getResponseHeader && xhr.getResponseHeader('content-type')) || '';
                if ((typeof res === 'string' && res.indexOf('<!DOCTYPE html') >= 0) || (ct && ct.indexOf('text/html') >= 0)) {
                    window.location.href = CFG.urls.login;
                    return true;
                }
            } catch (e) { }
            return false;
        }

        function escapeHtml(text) {
            if (!text && text !== 0) return '';
            return $('<div>').text(text).html();
        }

        function badgeEstado(estadoOriginal) {
            var est = (estadoOriginal || '')
                .toString()
                .normalize('NFD').replace(/[\u0300-\u036f]/g, '')
                .toLowerCase()
                .replace(/\s+/g, ' ')
                .trim();

            var mapa = {
                'en proceso de aplicacion': { cls: 'badge-en-progreso', txt: 'En proceso de Aplicación' },
                'rechazada': { cls: 'badge-rechazada', txt: 'Rechazada' },
                'asignada': { cls: 'badge-asignada', txt: 'Asignada' },
                'aprobada': { cls: 'badge-aprobada', txt: 'Aprobada' },
                'retirada': { cls: 'badge-retirada', txt: 'Retirada' },
                'finalizada': { cls: 'badge-finalizada', txt: 'Finalizada' },
                'rezagado': { cls: 'badge-rezagado', txt: 'Rezagado' },
                'archivado': { cls: 'badge-archivado', txt: 'Archivado' },
                'en curso': { cls: 'badge-en-curso', txt: 'En Curso' },
                'activo': { cls: 'badge-activo', txt: 'Activo' },
                'inactivo': { cls: 'badge-inactivo', txt: 'Inactivo' }
            };

            var info = mapa[est] || { cls: 'badge-no-asignada', txt: (estadoOriginal || '—') };
            return `<span class="badge ${info.cls}">${info.txt}</span>`;
        }

        // ===============================
        // DataTable PRINCIPAL
        // ===============================
        var tabla = $('#miTabla').DataTable({
            responsive: true,
            ajax: {
                url: CFG.urls.getVacantes,
                data: function (d) {
                    d.idEstado = $('#filtroPractica').val() || null;
                    d.idEspecialidad = $('#filtroEspecialidad').val() || 0;
                    d.idModalidad = $('#filtroModalidad').val() || 0;
                    return d;
                },
                dataSrc: 'data'
            },
            columns: [
                { data: 'EmpresaNombre' },
                { data: 'EspecialidadNombre' },
                { data: 'Requerimientos' },
                { data: 'NumCupos' },
                { data: 'EstudiantesPostulados' },
                {
                    data: 'EstadoNombre',
                    render: function (data) {
                        return badgeEstado(data);
                    }
                },
                {
                    data: 'IdVacante',
                    orderable: false,
                    render: function (data, type, row) {
                        var inactivaOArchivado = (row.EstadoNombre === "Inactivo" || row.EstadoNombre === "Archivado");
                        var esAutogestionada = (row.Nombre && row.Nombre.indexOf('Práctica Autogestionada') >= 0);
                        var dis = inactivaOArchivado ? 'disabled aria-disabled="true"' : '';
                        var muted = inactivaOArchivado ? 'opacity:0.35; cursor:not-allowed;' : '';

                        var acc = `<button class="btn bg-transparent btn-visualizar" data-id="${data}" title="Visualizar" style="color:#2d594d">
                         <i class="fas fa-eye"></i>
                       </button>`;

                        if ((CFG.rol === 2 || CFG.rol === 3) && !esAutogestionada) {
                            acc += `<button class="btn bg-transparent btn-asignar" data-id="${data}" 
                          title="${inactivaOArchivado ? 'Acción deshabilitada' : 'Asignar'}" 
                          style="color:#2d594d; ${muted}" ${dis}>
                        <i class="fas fa-user-plus"></i>
                      </button>`;
                        }

                        if (CFG.rol === 2) {
                            acc += `<button class="btn bg-transparent btn-editar" data-id="${data}" 
                            title="${inactivaOArchivado ? 'Deshabilitado' : 'Editar'}" 
                            style="color:#2d594d; ${muted}" ${dis}>
                        <i class="fas fa-sync-alt"></i></button>
                      <button class="btn bg-transparent btn-eliminar" data-id="${data}" 
                            title="Archivar" style="color:#2d594d; ${muted}" ${dis}>
                        <i class="fas fa-archive"></i></button>`;
                        }
                        return acc;
                    }
                }
            ]
        });

        // ===============================
        // Filtros
        // ===============================
        $('#filtroPractica, #filtroEspecialidad, #filtroModalidad').on('change', function () {
            tabla.ajax.reload();
        });

        // ===============================
        // CREAR
        // ===============================
        $('#btnGuardarVacante').on('click', function (e) {
            e.preventDefault();
            var $form = $('#formCrearVacante');
            if (!$form[0].checkValidity()) {
                $form[0].reportValidity();
                return;
            }

            $.ajax({
                url: CFG.urls.crear,
                type: 'POST',
                data: $form.serialize(),
                dataType: 'json',
                success: function (res, status, xhr) {
                    if (redirSiLogin(res, xhr)) return;
                    if (res.ok) {
                        Swal.fire({
                            icon: 'success',
                            title: 'Éxito',
                            text: res.message
                        });
                        $('#modalCrearVacante').modal('hide');
                        $form[0].reset();
                        $('#ubicacionEmpresa').val('');
                        tabla.ajax.reload();
                    } else {
                        Swal.fire({ icon: 'error', title: 'Error', text: res.message || 'Error en servidor' });
                    }
                },
                error: function () {
                    Swal.fire('Error', 'Error en la petición al servidor.', 'error');
                }
            });
        });

        // ===============================
        // ELIMINAR (archivar)
        // ===============================
        $('#miTabla').on('click', '.btn-eliminar', function () {
            var id = $(this).data('id');
            Swal.fire({
                title: '¿Deseas archivar esta práctica?',
                text: 'No podrás revertir esta acción.',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: 'Sí, archivar',
                cancelButtonText: 'Cancelar',
                confirmButtonColor: '#2d594d'
            }).then((result) => {
                if (result.isConfirmed) {
                    $.post(CFG.urls.eliminar, { id }, function (res, status, xhr) {
                        if (redirSiLogin(res, xhr)) return;
                        if (res.ok) {
                            Swal.fire('Listo', res.message, 'success');
                            tabla.ajax.reload();
                        } else {
                            Swal.fire('Error', res.message, 'error');
                        }
                    });
                }
            });
        });

        // ===============================
        // VISUALIZAR + postulaciones
        // ===============================
        $('#miTabla').on('click', '.btn-visualizar', function () {
            var id = $(this).data('id');
            $.get(CFG.urls.detalle, { id }, function (res, status, xhr) {
                if (redirSiLogin(res, xhr)) return;
                if (res.ok && res.data) {
                    var d = res.data;
                    $('#vis-Nombre').val(d.Nombre);
                    $('#vis-Empresa').val(d.IdEmpresa);
                    $('#vis-Ubicacion').val(d.Ubicacion);
                    $('#vis-Especialidad').val(d.IdEspecialidad);
                    $('#vis-NumCupos').val(d.NumCupos);
                    $('#vis-Modalidad').val(d.IdModalidad);
                    $('#vis-Requerimientos').val(d.Requerimientos);
                    $('#vis-Descripcion').val(d.Descripcion);
                    $('#vis-FechaAplicacion').val(d.FechaMaxAplicacion ? d.FechaMaxAplicacion.split('T')[0] : '');
                    $('#vis-FechaCierre').val(d.FechaCierre ? d.FechaCierre.split('T')[0] : '');
                    $('#modalVisualizarVacante').modal('show');
                } else {
                    Swal.fire('Error', 'No se pudo cargar la vacante', 'error');
                }
            });
        });

        // ===============================
        // ASIGNAR ESTUDIANTE
        // ===============================
        $('#miTabla').on('click', '.btn-asignar', function () {
            var idVacante = $(this).data('id');
            $('#modalAsignar').data('idVacante', idVacante);

            if ($.fn.DataTable.isDataTable('#miTablaAsignar')) {
                $('#miTablaAsignar').DataTable().clear().destroy();
            }

            var $tbody = $('#miTablaAsignar tbody').empty();
            $.getJSON(CFG.urls.obtenerEstudiantesAsignar, { idVacante: idVacante }, function (res, status, xhr) {
                if (redirSiLogin(res, xhr)) return;
                if (!res || !res.ok) {
                    Swal.fire('Error', res.message || 'No se pudo cargar estudiantes', 'error');
                    return;
                }

                var lista = Array.isArray(res.data) ? res.data : [];
                if (lista.length === 0) {
                    $tbody.append('<tr><td colspan="5" class="text-center text-muted">No hay estudiantes disponibles</td></tr>');
                } else {
                    lista.forEach(function (e) {
                        var estado = (e.EstadoVacante || e.EstadoMostrar || '').trim();
                        var est = (estado || '').toLowerCase();
                        var badge = badgeEstado(estado || (e.TieneRelacionEnVacante ? 'Con Procesos Activos' : 'Sin Procesos Activos'));
                        var btn = '';

                        if (!e.TieneRelacionEnVacante) {
                            btn = `<button class="btn btn-sm btn-outline-success btn-asignar-estudiante" data-idusuario="${e.IdUsuario}">
                        <i class="fas fa-user-plus"></i> Asignar
                     </button>`;
                        } else if (est === 'retirada') {
                            btn = `<button class="btn btn-sm btn-outline-success btn-reactivar-estudiante" data-idusuario="${e.IdUsuario}">
                        <i class="fas fa-redo"></i> Reactivar
                     </button>`;
                        } else if (!['rechazada', 'aprobada', 'finalizada'].includes(est)) {
                            btn = `<button class="btn btn-sm btn-outline-danger btn-retirar-estudiante" data-idusuario="${e.IdUsuario}">
                        <i class="fas fa-user-minus"></i> Retirar
                     </button>`;
                        }

                        $tbody.append(`<tr>
                <td>${escapeHtml(e.NombreCompleto)}</td>
                <td>${escapeHtml(e.Cedula || '')}</td>
                <td>${escapeHtml(e.Especialidad || '')}</td>
                <td>${badge}</td>
                <td class="text-center">${btn}</td>
            </tr>`);
                    });
                }

                $('#miTablaAsignar').DataTable({
                    responsive: true,
                    autoWidth: false,
                    scrollX: true,
                    pageLength: 5,
                    deferRender: true,
                    language: {
                        url: "//cdn.datatables.net/plug-ins/1.13.6/i18n/es-ES.json",
                        emptyTable: "No hay estudiantes disponibles"
                    }
                });
                $('#modalAsignar').modal('show');
            });
        });

        // ===============================
        // RETIRAR / DESASIGNAR ESTUDIANTE
        // ===============================
        $(document).on('click', '.btn-retirar-estudiante', function () {
            var idUsuario = $(this).data('idusuario');
            var idVacante = $('#modalAsignar').data('idVacante');

            if (!idUsuario || !idVacante) {
                Swal.fire('Error', 'Datos inválidos para retirar al estudiante.', 'error');
                return;
            }

            Swal.fire({
                title: '¿Deseas retirar a este estudiante?',
                text: 'Esta acción marcará la práctica como "Retirada".',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: 'Sí, retirar',
                cancelButtonText: 'Cancelar',
                confirmButtonColor: '#2d594d'
            }).then(function (result) {
                if (result.isConfirmed) {
                    $.ajax({
                        url: CFG.urls.retirarEstudiante, // debe existir en tu controlador
                        type: 'POST',
                        data: { idUsuario: idUsuario, idVacante: idVacante },
                        success: function (res, status, xhr) {
                            if (redirSiLogin(res, xhr)) return;

                            if (res.ok) {
                                Swal.fire('Listo', res.message || 'Estudiante retirado correctamente.', 'success');
                                // recargar la tabla dentro del modal
                                $('#modalAsignar').modal('hide');
                                $('#miTabla').DataTable().ajax.reload(null, false);
                            } else {
                                Swal.fire('Error', res.message || 'No se pudo retirar el estudiante.', 'error');
                            }
                        },
                        error: function () {
                            Swal.fire('Error', 'Ocurrió un problema al retirar el estudiante.', 'error');
                        }
                    });
                }
            });
        });


    });
})(jQuery);