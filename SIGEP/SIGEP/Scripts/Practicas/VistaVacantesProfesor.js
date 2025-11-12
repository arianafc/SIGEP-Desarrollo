(function ($) {
    $(function () {
        const CFG = window.VacantesProfesorCfg || { urls: {}, rol: 0 };

        // =====================================================
        // 🔹 Helpers
        // =====================================================
        function redirSiLogin(res, xhr) {
            try {
                const ct = (xhr && xhr.getResponseHeader && xhr.getResponseHeader('content-type')) || '';
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

        function normalizarEstado(str) {
            return (str || '')
                .toString()
                .normalize('NFD').replace(/[\u0300-\u036f]/g, '')
                .toLowerCase()
                .replace(/\s+/g, ' ')
                .trim();
        }

        function badgeEstado(estadoOriginal) {
            const est = normalizarEstado(estadoOriginal);

            const mapa = {
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
                'inactivo': { cls: 'badge-inactivo', txt: 'Inactivo' },
                'sin proceso activo': { cls: 'badge-no-asignada', txt: 'Sin proceso activo' }
            };

            const info = mapa[est] || { cls: 'badge-no-asignada', txt: estadoOriginal || '—' };
            return `<span class="badge ${info.cls}">${info.txt}</span>`;
        }

        function formatFecha(val) {
            if (!val) return '';
            if (typeof val === 'string' && /^\/Date\(/.test(val)) {
                const ticks = parseInt(val.slice(6), 10);
                const d = new Date(ticks);
                return isNaN(d.getTime()) ? '' : d.toLocaleDateString('es-CR');
            }
            const d = new Date(val);
            if (!isNaN(d.getTime())) return d.toLocaleDateString('es-CR');
            const s = (val + '').split('T')[0].split('-');
            return (s.length === 3) ? (s[2] + '/' + s[1] + '/' + s[0]) : (val + '');
        }

        // =====================================================
        // 🔹 Filtros + Listado de vacantes (cards)
        // =====================================================
        filtrarVacantes();
        $("#filtroPractica,#filtroModalidad").on('change', filtrarVacantes);

        function filtrarVacantes() {
            const estado = ($("#filtroPractica").val() || '').toString().trim().toLowerCase();
            const modalidad = parseInt($("#filtroModalidad").val() || '0', 10) || 0;

            $.getJSON(CFG.urls.getVacantesProfesor, { estado, idModalidad: modalidad })
                .done(resp => {
                    if (resp && resp.ok) renderVacantes(resp.data);
                    else $(".vacantes-lista").html(
                        '<div class="vacante-alerta"><strong>Información:</strong> No se encontraron vacantes.</div>'
                    );
                })
                .fail(xhr => {
                    console.error('Error AJAX GetVacantesProfesor:', xhr.responseText || xhr.statusText);
                    $(".vacantes-lista").html(
                        '<div class="vacante-alerta"><strong>Error:</strong> No se pudo cargar la información.</div>'
                    );
                });
        }

        function renderVacantes(vacantes) {
            const $c = $(".vacantes-lista").empty();
            if (!vacantes || vacantes.length === 0) {
                $c.append('<div class="vacante-alerta"><strong>Información:</strong> No se encontraron vacantes.</div>');
                return;
            }

            vacantes.forEach(v => {
                if ((v.EstadoNombre || '').toLowerCase() === 'autogestionada') return;

                const aplicaciones = v.EstudiantesPostulados || 0;

                const card = `
                <article class="vacante-card" data-area="${escapeHtml(v.EspecialidadNombre || '')}">
                    <header class="vacante-header">
                        <h3 class="vacante-titulo">${escapeHtml(v.Nombre)}</h3>
                        <span class="vacante-empresa">${escapeHtml(v.EmpresaNombre)}</span>
                    </header>
                    <ul class="vacante-detalles">
                        <li><strong>Requisitos:</strong> ${escapeHtml(v.Requerimientos || '')}</li>
                        <li><strong>Modalidad:</strong> ${escapeHtml(v.ModalidadNombre || '')}</li>
                        <li><strong>Fecha límite de aplicación:</strong> ${formatFecha(v.FechaMaxAplicacion)}</li>
                        <li><strong>Número de cupos:</strong> ${v.NumCupos ?? 0}</li>
                        <li><strong>Número de aplicaciones:</strong> ${aplicaciones}</li>
                    </ul>
                    <div class="row g-2">
                        <div class="col-12 col-md-6">
                            <button class="w-100 btn btn-cta btn-detalle" data-id="${v.IdVacante}">
                                Ver más
                            </button>
                        </div>
                        <div class="col-12 col-md-6">
                            <button class="w-100 btn btn-cta btn-asignar" data-id="${v.IdVacante}" ${(v.NumCupos ?? 0) <= 0 ? 'disabled' : ''}>
                                Asignar
                            </button>
                        </div>
                    </div>
                </article>`;
                $c.append(card);
            });
        }

        // =====================================================
        // 🔹 Ver más (detalle vacante + postulaciones)
        // =====================================================
        $(document).on('click', '.btn-detalle', function () {
            const idVacante = $(this).data('id');
            if (!idVacante) return;

            $.getJSON(CFG.urls.detalle, { id: idVacante })
                .done(res => {
                    if (!res || !res.ok || !res.data) {
                        Swal.fire('Error', 'No se pudo cargar la vacante', 'error');
                        return;
                    }

                    const d = res.data;

                    $('#vis-Nombre').text(d.Nombre || '');
                    $('#vis-Empresa').text(d.EmpresaNombre || '');
                    $('#vis-Descripcion').text(d.Descripcion || '');
                    $('#vis-Requisitos').text(d.Requerimientos || '');
                    $('#vis-Modalidad').text(d.ModalidadNombre || '');
                    $('#vis-Ubicacion').text(d.Ubicacion || '');
                    $('#vis-FechaAplicacion').text(formatFecha(d.FechaMaxAplicacion));
                    $('#vis-NombreContacto').text(d.NombreContacto || '-');
                    $('#vis-Telefonos').text(
                        (d.Telefonos && d.Telefonos.length) ? d.Telefonos.join(', ') : 'No disponible'
                    );
                    $('#vis-Emails').text(
                        (d.Emails && d.Emails.length) ? d.Emails.join(', ') : 'No disponible'
                    );

                    $('#modalVisualizar').data('idVacante', idVacante);
                    $('#modalVisualizar').modal('show');

                    cargarPostulaciones(idVacante);
                })
                .fail(xhr => {
                    console.error('Error detalle vacante:', xhr.responseText || xhr.statusText);
                    Swal.fire('Error', 'No se pudo cargar la vacante', 'error');
                });
        });

        function cargarPostulaciones(idVacante) {
            $.getJSON(CFG.urls.obtenerPostulaciones, { idVacante })
                .done(res => {
                    const $lista = $('#listaPostulaciones').empty();

                    if (!res || !res.ok || !res.data || res.data.length === 0) {
                        $('#mensajeSinAsignados').show();
                        return;
                    }
                    $('#mensajeSinAsignados').hide();

                    res.data.forEach(item => {
                        const estado = (item.EstadoDescripcion || item.EstadoVacante || '').trim();

                        const li = `
                <li class="d-flex justify-content-between align-items-center p-2 border rounded mb-2">
                    <div>
                        <a href="${CFG.urls.visualizacionPostulacion}?idVacante=${encodeURIComponent(idVacante)}&idUsuario=${encodeURIComponent(item.IdUsuario)}"
                           class="text-decoration-none"
                           style="color:#2D594D; font-weight:600;">
                           ${escapeHtml(item.NombreCompleto)}
                        </a>
                    </div>
                    <div class="d-flex align-items-center gap-2">
                        ${badgeEstado(estado)}
                    </div>
                </li>`;
                        $lista.append(li);
                    });
                })
                .fail(xhr => {
                    console.error('Error obtenerPostulaciones:', xhr.responseText || xhr.statusText);
                    $('#listaPostulaciones').html(
                        '<li class="text-danger text-center p-2">Error al cargar postulaciones.</li>'
                    );
                });
        }


        

        // =====================================================
        // 🔹 Desasignar desde modal "Ver más" con comentario
        // =====================================================
        $(document).on("click", ".btn-desasignar-estudiante", function (e) {
            e.preventDefault();

            const idVacante = $(this).data("idvacante");
            const idUsuario = $(this).data("idusuario");
            const modalVisualizarEl = document.getElementById("modalVisualizar");
            const modalVisualizar = modalVisualizarEl
                ? bootstrap.Modal.getInstance(modalVisualizarEl)
                : null;

            if (modalVisualizar && modalVisualizar._focustrap) {
                modalVisualizar._focustrap.deactivate();
            }

            Swal.fire({
                title: '¿Desea desasignar este estudiante?',
                text: 'El estado se cambiará a "Retirada".',
                icon: 'warning',
                input: 'textarea',
                inputLabel: 'Comentario (opcional)',
                inputPlaceholder: 'Escribe un comentario...',
                showCancelButton: true,
                confirmButtonText: 'Sí, desasignar',
                cancelButtonText: 'Cancelar',
                confirmButtonColor: "#2D594D",
                cancelButtonColor: "#6c757d",
                allowOutsideClick: false
            }).then(result => {
                if (!result.isConfirmed) {
                    if (modalVisualizar && modalVisualizar._focustrap) {
                        modalVisualizar._focustrap.activate();
                    }
                    return;
                }

                const comentario = (result.value || '').trim();

                const ejecutarRetiro = () => {
                    $.post(CFG.urls.retirarEstudiante, {
                        idVacante,
                        idUsuario
                    })
                        .done(resp => {
                            if (resp && resp.ok) {
                                Swal.fire({
                                    title: "Desasignado",
                                    text: resp.message || "El estudiante fue desasignado correctamente.",
                                    icon: "success",
                                    timer: 1500,
                                    showConfirmButton: false
                                }).then(() => {
                                    cargarPostulaciones(idVacante);
                                    filtrarVacantes();
                                });
                            } else {
                                Swal.fire("Error", (resp && resp.message) || "No se pudo desasignar.", "error");
                            }
                        })
                        .fail(xhr => {
                            console.error("Error RetirarEstudiante:", xhr.responseText || xhr.statusText);
                            Swal.fire("Error", "Ocurrió un error al procesar la solicitud.", "error");
                        })
                        .always(() => {
                            if (modalVisualizar && modalVisualizar._focustrap) {
                                modalVisualizar._focustrap.activate();
                            }
                        });
                };

                if (comentario) {
                    $.post(CFG.urls.agregarComentario, {
                        idVacante,
                        idUsuario,
                        comentario
                    })
                        .done(res => {
                            if (res && res.success) {
                                ejecutarRetiro();
                            } else {
                                Swal.fire("Error", (res && res.message) || "No se pudo guardar el comentario.", "error")
                                    .then(() => {
                                        if (modalVisualizar && modalVisualizar._focustrap) {
                                            modalVisualizar._focustrap.activate();
                                        }
                                    });
                            }
                        })
                        .fail(xhr => {
                            console.error("Error agregarComentario:", xhr.responseText || xhr.statusText);
                            Swal.fire("Error", "No se pudo guardar el comentario.", "error")
                                .then(() => {
                                    if (modalVisualizar && modalVisualizar._focustrap) {
                                        modalVisualizar._focustrap.activate();
                                    }
                                });
                        });
                } else {
                    ejecutarRetiro();
                }
            });
        });

        // =====================================================
        // 🔹 Modal Asignar (DataTable) — estilo unificado
        // =====================================================
        const tablaAsignar = $('#tablaAsignar').DataTable({
            language: { url: '//cdn.datatables.net/plug-ins/1.13.6/i18n/es-ES.json' },
            responsive: true,
            pageLength: 5,
            autoWidth: false,
            destroy: true
        });

        $(document).on('click', '.btn-asignar', function () {
            const idVacante = $(this).data('id');
            $('#modalAsignarVacante').data('idvacante', idVacante);
            $('#modalAsignarVacante').modal('show');
            cargarEstudiantesAsignar(idVacante);
        });

        //function cargarEstudiantesAsignar(idVacante) {
        //    $.getJSON(CFG.urls.obtenerEstudiantesAsignar, { idVacante })
        //        .done(res => {
        //            tablaAsignar.clear();

        //            if (!res || !res.ok || !res.data || res.data.length === 0) {
        //                tablaAsignar.row.add([
        //                    '—', '—', '—',
        //                    '—',
        //                    '<span class="text-danger">No hay estudiantes disponibles</span>'
        //                ]);
        //                tablaAsignar.draw();
        //                return;
        //            }

        //            res.data.forEach(e => {
        //                const estadoRaw = e.EstadoVacante || e.EstadoPractica || 'Sin proceso activo';
        //                const estado = normalizarEstado(estadoRaw);
        //                const badge = badgeEstado(estadoRaw);
        //                const tieneRelacion = !!e.TieneRelacionEnVacante;
        //                const tieneActiva = !!e.TienePracticaActiva;

        //                let btnHtml = '';

        //                // 1️⃣ Tiene práctica activa en otra vacante → bloquear
        //                if (tieneActiva && !tieneRelacion) {
        //                    btnHtml = `
        //                        <button class="btn btn-sm btn-outline-secondary" disabled
        //                                title="Ya tiene práctica activa en otra vacante">
        //                            <i class="fas fa-ban"></i> No disponible
        //                        </button>`;
        //                }
        //                // 2️⃣ Relación con esta vacante
        //                else if (tieneRelacion) {
        //                    if (estado === 'asignada') {
        //                        // Puede retirar de ESTA vacante
        //                        btnHtml = `
        //                            <button class="btn btn-sm btn-outline-danger btn-retirar-estudiante"
        //                                    data-idusuario="${e.IdUsuario}">
        //                                <i class="fas fa-user-minus"></i> Retirar
        //                            </button>`;
        //                    }
        //                    else if (estado === 'en proceso de aplicacion' ||
        //                        estado === 'retirada' ||
        //                        estado === 'sin proceso activo') {
        //                        // Usamos mismo botón Asignar (el backend decide: nuevo, pasar a asignada, reactivar, etc.)
        //                        btnHtml = `
        //                            <button class="btn btn-sm btn-outline-success btn-asignar-estudiante"
        //                                    data-idusuario="${e.IdUsuario}">
        //                                <i class="fas fa-user-plus"></i> Asignar
        //                            </button>`;
        //                    }
        //                    else {
        //                        // Estados finales o bloqueantes en esta vacante
        //                        btnHtml = `
        //                            <button class="btn btn-sm btn-outline-secondary" disabled>
        //                                <i class="fas fa-ban"></i> No disponible
        //                            </button>`;
        //                    }
        //                }
        //                // 3️⃣ No relación aún + sin práctica activa bloqueante → puede asignarse
        //                else {
        //                    if (['rechazada', 'aprobada', 'finalizada', 'en curso', 'archivado', 'rezagado'].includes(estado)) {
        //                        btnHtml = `
        //                            <button class="btn btn-sm btn-outline-secondary" disabled>
        //                                <i class="fas fa-ban"></i> No disponible
        //                            </button>`;
        //                    } else {
        //                        btnHtml = `
        //                            <button class="btn btn-sm btn-outline-success btn-asignar-estudiante"
        //                                    data-idusuario="${e.IdUsuario}">
        //                                <i class="fas fa-user-plus"></i> Asignar
        //                            </button>`;
        //                    }
        //                }

        //                tablaAsignar.row.add([
        //                    escapeHtml(e.NombreCompleto || ''),
        //                    escapeHtml(e.Cedula || ''),
        //                    escapeHtml(e.Especialidad || ''),
        //                    badge,
        //                    btnHtml
        //                ]);
        //            });

        //            tablaAsignar.draw();
        //        })
        //        .fail(xhr => {
        //            console.error('Error obtenerEstudiantesAsignar:', xhr.responseText || xhr.statusText);
        //            tablaAsignar.clear().row.add([
        //                '—', '—', '—', '—',
        //                '<span class="text-danger">Error al cargar estudiantes</span>'
        //            ]).draw();
        //        });
        //}
        function cargarEstudiantesAsignar(idVacante) {
            $.getJSON(CFG.urls.obtenerEstudiantesAsignar, { idVacante })
                .done(res => {
                    const $tbody = $('#tablaAsignar tbody');
                    $tbody.empty();

                    if (!res || !res.ok || !res.data || res.data.length === 0) {
                        $tbody.html('<tr><td colspan="5" class="text-center text-muted">No hay estudiantes disponibles</td></tr>');
                        return;
                    }

                    res.data.forEach(e => {
                        const estadoRaw = e.EstadoVacante || e.EstadoPractica || 'Sin proceso activo';
                        const estado = normalizarEstado(estadoRaw);
                        const badge = badgeEstado(estadoRaw);
                        const tieneRelacion = !!e.TieneRelacionEnVacante;
                        const tieneActiva = !!e.TienePracticaActiva;

                        let btnHtml = '';

                        // 🔹 1️⃣ Tiene práctica activa en otra vacante → bloquear
                        if (tieneActiva && !tieneRelacion) {
                            btnHtml = `
                        <button class="btn bg-transparent btn-accion"
                                title="Ya tiene práctica activa en otra vacante"
                                style="color:#6c757d;" disabled>
                            <i class="fas fa-ban fa-lg"></i>
                        </button>`;
                        }
                        // 🔹 2️⃣ Relación con esta vacante
                        else if (tieneRelacion) {
                            if (estado === 'asignada') {
                                // Puede retirar de ESTA vacante
                                btnHtml = `
    <button class="btn bg-transparent btn-accion btn-retirar-estudiante"
            data-idusuario="${e.IdUsuario}"
            data-idpractica="${e.IdPracticaVacante || 0}"
            data-nombre="${(e.NombreCompleto || '—').replace(/"/g, '&quot;')}"
            data-estadoacademico="${e.EstadoAcademicoDescripcion || 'Activo'}"
            title="Retirar estudiante"
            style="color:#b02a37;">
        <i class="fas fa-trash-alt fa-lg"></i>
    </button>`;
                            } else if (['en proceso de aplicacion', 'retirada', 'sin proceso activo'].includes(estado)) {
                                // Usamos mismo botón Asignar (el backend decide: nuevo, pasar a asignada, reactivar, etc.)
                                btnHtml = `
                            <button class="btn bg-transparent btn-accion btn-asignar-estudiante"
                                    data-idusuario="${e.IdUsuario}"
                                    title="Asignar estudiante"
                                    style="color:#198754;">
                                <i class="fas fa-user-plus fa-lg"></i>
                            </button>`;
                            } else {
                                // Estados finales o bloqueantes en esta vacante
                                btnHtml = `
                            <button class="btn bg-transparent btn-accion"
                                    title="No disponible"
                                    style="color:#6c757d;" disabled>
                                <i class="fas fa-ban fa-lg"></i>
                            </button>`;
                            }
                        }
                        // 🔹 3️⃣ No relación aún + sin práctica activa bloqueante → puede asignarse
                        else {
                            if (['rechazada', 'aprobada', 'finalizada', 'en curso', 'archivado', 'rezagado'].includes(estado)) {
                                btnHtml = `
                            <button class="btn bg-transparent btn-accion"
                                    title="No disponible"
                                    style="color:#6c757d;" disabled>
                                <i class="fas fa-ban fa-lg"></i>
                            </button>`;
                            } else {
                                btnHtml = `
                            <button class="btn bg-transparent btn-accion btn-asignar-estudiante"
                                    data-idusuario="${e.IdUsuario}"
                                    title="Asignar estudiante"
                                    style="color:#198754;">
                                <i class="fas fa-user-plus fa-lg"></i>
                            </button>`;
                            }
                        }

                        $tbody.append(`
                    <tr class="align-middle text-center">
                        <td>${escapeHtml(e.NombreCompleto || '')}</td>
                        <td>${escapeHtml(e.Cedula || '')}</td>
                        <td>${escapeHtml(e.Especialidad || '')}</td>
                        <td>${badge}</td>
                        <td>${btnHtml}</td>
                    </tr>`);
                    });
                })
                .fail(xhr => {
                    console.error('Error obtenerEstudiantesAsignar:', xhr.responseText || xhr.statusText);
                    $('#tablaAsignar tbody').html('<tr><td colspan="5" class="text-center text-danger">Error al cargar estudiantes</td></tr>');
                });
        }
    



        // =====================================================
        // 🔹 Asignar estudiante (usa tu AsignarEstudiante C#)
        // =====================================================
        $(document).on('click', '.btn-asignar-estudiante', function () {
            const idUsuario = $(this).data('idusuario');
            const idVacante = $('#modalAsignarVacante').data('idvacante');

            Swal.fire({
                title: 'Confirmar asignación',
                html: 'Este botón usará la lógica de estados del sistema (En proceso / Asignada / Reactivar).',
                icon: 'question',
                showCancelButton: true,
                confirmButtonText: 'Sí, continuar',
                cancelButtonText: 'Cancelar',
                confirmButtonColor: '#2D594D'
            }).then(r => {
                if (!r.isConfirmed) return;

                $.ajax({
                    url: CFG.urls.asignarEstudiante,
                    type: 'POST',
                    data: { idVacante, idUsuario },
                    success: function (res, status, xhr) {
                        if (redirSiLogin(res, xhr)) return;

                        if (res && res.ok) {
                            Swal.fire({
                                icon: 'success',
                                title: 'Éxito',
                                text: res.message || 'Acción realizada correctamente.',
                                timer: 1800,
                                showConfirmButton: false
                            });
                            cargarEstudiantesAsignar(idVacante);
                            filtrarVacantes();
                        } else {
                            Swal.fire('Aviso', (res && res.message) || 'No se pudo completar la acción.', 'warning');
                        }
                    },
                    error: function (xhr) {
                        console.error('Error asignarEstudiante:', xhr.responseText || xhr.statusText);
                        Swal.fire('Error', 'Ocurrió un error al procesar la solicitud.', 'error');
                    }
                });
            });
        });

        // =====================================================
        // 🔹 Retirar estudiante (usa RetirarEstudiante C#)
        // =====================================================
        // =====================================================
        // 🔹 Retirar estudiante (usa /Practicas/DesasignarPractica con comentario obligatorio)
        // =====================================================
        $(document).on('click', '.btn-retirar-estudiante', function () {
            const idUsuario = $(this).data('idusuario') || 0;
            const idVacante = $('#modalAsignarVacante').data('idvacante');
            const idPractica = $(this).data('idpractica') || 0;
            const nombre = $(this).data('nombre') || '—';
            const estadoAcademico = $(this).data('estadoacademico') ?? 'Activo';

            if (!idPractica) {
                Swal.fire('Error', 'No se encontró una práctica activa para este estudiante.', 'error');
                return;
            }

            // 🔹 Evitar bloqueo de foco del modal Bootstrap mientras aparece SweetAlert
            const modal = document.getElementById('modalAsignarVacante');
            const modalInstance = modal ? bootstrap.Modal.getInstance(modal) : null;
            if (modalInstance?._focustrap) modalInstance._focustrap.deactivate();

            Swal.fire({
                title: '¿Deseas retirar esta práctica?',
                html: `
        <div style="font-size:15px;line-height:1.5;">
            El estudiante <b style="color:#2d594d;">${nombre}</b><br>
            <small>Estado académico actual: <b>${estadoAcademico}</b></small><br><br>
            Pasará al estado de práctica <b>"Retirada"</b>.
        </div>`,
                icon: 'warning',
                input: 'textarea',
                inputLabel: 'Comentario (obligatorio)',
                inputPlaceholder: 'Escribe el motivo de la desasignación...',
                showCancelButton: true,
                confirmButtonText: 'Sí, desasignar',
                cancelButtonText: 'Cancelar',
                allowOutsideClick: false,
                confirmButtonColor: '#2d594d',
                preConfirm: (value) => {
                    if (!value || !value.trim()) {
                        Swal.showValidationMessage('⚠️ Debes ingresar el motivo de la desasignación.');
                    }
                }
            }).then(result => {
                if (!result.isConfirmed) {
                    if (modalInstance?._focustrap) modalInstance._focustrap.activate();
                    return;
                }

                const comentario = result.value.trim();

                $.post('/Practicas/DesasignarPractica', { idPractica, comentario })
                    .done(res => {
                        if (res.ok) {
                            Swal.fire({
                                icon: 'success',
                                title: 'Desasignado correctamente',
                                text: res.msg || 'La práctica fue desasignada exitosamente.',
                                timer: 1800,
                                showConfirmButton: false
                            });

                            // 🔁 Actualizar tabla sin cerrar el modal
                            cargarEstudiantesAsignar(idVacante);
                            filtrarVacantes();
                        } else {
                            Swal.fire('Error', res.msg || 'No se pudo desasignar la práctica.', 'error');
                        }
                    })
                    .fail(() => Swal.fire('Error', 'Error de conexión al servidor.', 'error'))
                    .always(() => {
                        if (modalInstance?._focustrap) modalInstance._focustrap.activate();
                    });
            });
        });


    });
})(jQuery);

