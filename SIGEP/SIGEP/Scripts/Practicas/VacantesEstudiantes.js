(function ($) {
    $(function () {
        const CFG = window.VacantesCfg || { urls: {}, rol: 0 };

        // ===============================
        // Helpers
        // ===============================
        function redirSiLogin(res, xhr) {
            try {
                var ct = (xhr && xhr.getResponseHeader && xhr.getResponseHeader('content-type')) || '';
                if ((typeof res === 'string' && res.indexOf('<!DOCTYPE html') >= 0) ||
                    (ct && ct.indexOf('text/html') >= 0)) {
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
            processing: true,
            ajax: {
                url: CFG.urls.getVacantes,
                type: 'GET',
                data: function (d) {
                    d.idEstado = $('#filtroPractica').val() || null;
                    d.idEspecialidad = $('#filtroEspecialidad').val() || 0;
                    d.idModalidad = $('#filtroModalidad').val() || 0;
                    return d;
                },
                dataSrc: function (json) {
                    if (!json || !json.data) return [];
                    return json.data;
                }
            },
            columns: [
                { data: 'EmpresaNombre', title: 'Empresa' },
                { data: 'EspecialidadNombre', title: 'Especialidad' },
                { data: 'Requerimientos', title: 'Requisitos' },
                { data: 'NumCupos', title: 'Cupos Disponibles' },
                {
                    data: 'NumPostulados',
                    title: 'Estudiantes Postulados',
                    render: (data) => `<strong>${data || 0}</strong>`
                },
                {
                    data: 'EstadoNombre',
                    title: 'Estado',
                    render: (data) => badgeEstado(data)
                },
                {
                    data: 'IdVacante',
                    orderable: false,
                    title: 'Acciones',
                    render: function (data, type, row) {
                        const estado = (row.EstadoNombre || '').toLowerCase();
                        const inactivo = estado === 'inactivo' || estado === 'archivado';
                        const esAuto = row.Nombre && row.Nombre.includes('Práctica Autogestionada');
                        const dis = inactivo ? 'disabled aria-disabled="true"' : '';
                        const muted = inactivo ? 'opacity:0.35; cursor:not-allowed;' : '';

                        let acc = `
                            <button class="btn bg-transparent btn-visualizar" data-id="${data}" title="Visualizar" style="color:#2d594d">
                                <i class="fas fa-eye"></i>
                            </button>
                        `;

                        if ((CFG.rol === 2 || CFG.rol === 3) && !esAuto) {
                            acc += `
                                <button class="btn bg-transparent btn-asignar" data-id="${data}" 
                                    title="${inactivo ? 'Acción deshabilitada' : 'Asignar'}" 
                                    style="color:#2d594d; ${muted}" ${dis}>
                                    <i class="fas fa-user-plus"></i>
                                </button>
                            `;
                        }

                        if (CFG.rol === 2) {
                            acc += `
                                <button class="btn bg-transparent btn-editar" data-id="${data}" 
                                    title="${inactivo ? 'Deshabilitado' : 'Editar'}"
                                    style="color:#2d594d; ${muted}" ${dis}>
                                    <i class="fas fa-sync-alt"></i>
                                </button>
                                <button class="btn bg-transparent btn-eliminar" data-id="${data}" 
                                    title="Archivar" style="color:#2d594d; ${muted}" ${dis}>
                                    <i class="fas fa-archive"></i>
                                </button>
                            `;
                        }
                        return acc;
                    }
                }
            ],
            language: {
                url: "//cdn.datatables.net/plug-ins/1.13.6/i18n/es-ES.json",
                emptyTable: "No hay vacantes registradas.",
                zeroRecords: "No se encontraron resultados."
            }
        });

        // ===============================
        // Filtros
        // ===============================
        $('#filtroPractica, #filtroEspecialidad, #filtroModalidad').on('change', () => tabla.ajax.reload());

        // ===============================
        // VISUALIZAR VACANTE + POSTULACIONES
        // ===============================
        $('#miTabla').on('click', '.btn-visualizar', function () {
            const id = $(this).data('id');
            $.get(CFG.urls.detalle, { id }, function (res, status, xhr) {
                if (redirSiLogin(res, xhr)) return;

                if (res && res.ok && res.data) {
                    const d = res.data;
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

                    // Cargar postulaciones
                    $.getJSON(CFG.urls.obtenerPostulaciones, { idVacante: id }, function (r2) {
                        const $lista = $('#listaPostulaciones').empty();
                        $('#mensajeSinPostulaciones').hide();

                        if (r2.ok && r2.data && r2.data.length > 0) {
                            r2.data.forEach(function (p) {
                                const badge = badgeEstado(p.EstadoDescripcion);
                                const btnDes = `
                                    <button class="btn bg-transparent BtnDesasignarPracticaEstudiante"
                                        data-idvacante="${p.IdVacante}"
                                        data-idusuario="${p.IdUsuario}"
                                        title="Desasignar práctica"
                                        style="color:#2D594D;">
                                        <i class="fas fa-trash-alt"></i>
                                    </button>`;
                                $lista.append(`
                                    <li class="d-flex justify-content-between align-items-center p-2 border rounded mb-2">
                                        <div><strong>${escapeHtml(p.NombreCompleto)}</strong></div>
                                        <div class="d-flex align-items-center gap-2">${badge}${btnDes}</div>
                                    </li>
                                `);
                            });
                        } else {
                            $('#mensajeSinPostulaciones').show();
                        }
                    });
                } else {
                    Swal.fire('Error', 'No se pudo cargar la vacante', 'error');
                }
            });
        });

        // ===============================
        // ASIGNAR ESTUDIANTE
        // ===============================
        $('#miTabla').on('click', '.btn-asignar', function () {
            const idVacante = $(this).data('id');
            $('#modalAsignar').data('idVacante', idVacante);
            $('#modalAsignar').modal('show');

            $.getJSON(CFG.urls.obtenerEstudiantesAsignar, { idVacante }, function (res, status, xhr) {
                if (redirSiLogin(res, xhr)) return;
                const tbody = $('#miTablaAsignar tbody').empty();

                if (!res || !res.ok || !Array.isArray(res.data) || res.data.length === 0) {
                    tbody.append('<tr><td colspan="5" class="text-center text-muted">No hay estudiantes disponibles</td></tr>');
                    return;
                }

                res.data.forEach(function (e) {
                    const estado = (e.EstadoVacante || '').toLowerCase();
                    const badge = badgeEstado(e.EstadoVacante);
                    let btn = '';

                    if (!e.TieneRelacionEnVacante || estado === 'rechazada' || estado === 'retirada') {
                        btn = `
                            <button class="btn btn-sm btn-outline-success btn-asignar-estudiante" data-idusuario="${e.IdUsuario}">
                                <i class="fas fa-user-plus"></i> Asignar
                            </button>`;
                    } else if (!['finalizada', 'aprobada'].includes(estado)) {
                        btn = `
                            <button class="btn btn-sm btn-outline-danger btn-retirar-estudiante" data-idusuario="${e.IdUsuario}">
                                <i class="fas fa-user-minus"></i> Retirar
                            </button>`;
                    }

                    tbody.append(`
                        <tr>
                            <td>${escapeHtml(e.NombreCompleto)}</td>
                            <td>${escapeHtml(e.Cedula || '')}</td>
                            <td>${escapeHtml(e.Especialidad || '')}</td>
                            <td>${badge}</td>
                            <td class="text-center">${btn}</td>
                        </tr>
                    `);
                });
            });
        });

        // ASIGNAR
        $(document).on('click', '.btn-asignar-estudiante', function () {
            const idUsuario = $(this).data('idusuario');
            const idVacante = $('#modalAsignar').data('idVacante');

            $.post(CFG.urls.asignarEstudiante, { idUsuario, idVacante })
                .done(res => {
                    if (res.ok) {
                        Swal.fire('Éxito', res.message, 'success');
                        $('#modalAsignar').modal('hide');
                        tabla.ajax.reload(null, false);
                    } else {
                        Swal.fire('Error', res.message, 'error');
                    }
                })
                .fail(() => Swal.fire('Error', 'Error al asignar estudiante.', 'error'));
        });

        // DESASIGNAR
        $(document).on('click', '.BtnDesasignarPracticaEstudiante, .btn-retirar-estudiante', function () {
            const idUsuario = $(this).data('idusuario');
            const idVacante = $(this).data('idvacante') || $('#modalAsignar').data('idVacante');

            if (!idUsuario || !idVacante) {
                Swal.fire('Error', 'Datos inválidos para desasignar.', 'error');
                return;
            }

            Swal.fire({
                title: '¿Deseas desasignar esta práctica?',
                text: 'El estado cambiará a "Retirada".',
                icon: 'warning',
                input: 'textarea',
                inputLabel: 'Comentario (opcional)',
                showCancelButton: true,
                confirmButtonText: 'Sí, desasignar',
                cancelButtonText: 'Cancelar',
                confirmButtonColor: '#2d594d'
            }).then((result) => {
                if (result.isConfirmed) {
                    $.post(CFG.urls.desasignarPractica, {
                        idUsuario, idVacante, comentario: result.value || ''
                    })
                        .done(res => {
                            if (res.ok) {
                                Swal.fire('Listo', res.message, 'success');
                                $('#modalAsignar, #modalVisualizarVacante').modal('hide');
                                tabla.ajax.reload(null, false);
                            } else {
                                Swal.fire('Error', res.message, 'error');
                            }
                        })
                        .fail(() => Swal.fire('Error', 'Ocurrió un problema al retirar el estudiante.', 'error'));
                }
            });
        });

    });
})(jQuery);
