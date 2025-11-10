//(function ($) {
//    $(function () {
//        const cfg = window.VacantesProfesorCfg || { urls: {} };


//        function escapeHtml(text) { if (!text && text !== 0) return ''; return $('<div>').text(text).html(); }
//        function formatFecha(val) {
//            if (!val) return '';
//            if (typeof val === 'string' && /^\/Date\(/.test(val)) {
//                const ticks = parseInt(val.substr(6), 10);
//                const d = new Date(ticks);
//                return isNaN(d.getTime()) ? '' : d.toLocaleDateString('es-CR');
//            }
//            const d = new Date(val);
//            if (!isNaN(d.getTime())) return d.toLocaleDateString('es-CR');
//            const s = (val + '').split('T')[0].split('-');
//            return (s.length === 3) ? (s[2] + '/' + s[1] + '/' + s[0]) : (val + '');
//        }


//        function badgeEstado(estadoOriginal) {
//            var est = (estadoOriginal || '')
//                .toString()
//                .normalize('NFD').replace(/[\u0300-\u036f]/g, '')
//                .toLowerCase().replace(/\s+/g, ' ').trim();

//            var mapa = {
//                'en proceso de aplicacion': { cls: 'badge-en-progreso', txt: 'En proceso de Aplicación' },
//                'rechazada': { cls: 'badge-rechazada', txt: 'Rechazada' },
//                'asignada': { cls: 'badge-asignada', txt: 'Asignada' },
//                'aprobada': { cls: 'badge-aprobada', txt: 'Aprobada' },
//                'retirada': { cls: 'badge-retirada', txt: 'Retirada' },
//                'finalizada': { cls: 'badge-finalizada', txt: 'Finalizada' },
//                'rezagado': { cls: 'badge-rezagado', txt: 'Rezagado' },
//                'archivado': { cls: 'badge-archivado', txt: 'Archivado' },
//                'en curso': { cls: 'badge-en-curso', txt: 'En Curso' },

//                'activo': { cls: 'badge-activo', txt: 'Activo' },
//                'inactivo': { cls: 'badge-inactivo', txt: 'Inactivo' }
//            };

//            var info = mapa[est] || { cls: 'badge-no-asignada', txt: (estadoOriginal || '—') };
//            return `<span class="badge ${info.cls}">${info.txt}</span>`;
//        }


//        filtrarVacantes();
//        $("#filtroPractica,#filtroModalidad").on('change', filtrarVacantes);

//        function filtrarVacantes() {

//            var estado = ($("#filtroPractica").val() || '').toString().trim().toLowerCase();
//            var modalidad = parseInt($("#filtroModalidad").val() || '0', 10) || 0;

//            $.getJSON(cfg.urls.getVacantesProfesor, { estado: estado, idModalidad: modalidad })
//                .done(function (resp) {
//                    if (resp && resp.ok) renderVacantes(resp.data);
//                    else $(".vacantes-lista").html(
//                        '<div class="vacante-alerta"><strong>Información:</strong> No se encontraron vacantes.</div>'
//                    );
//                })
//                .fail(function (xhr) {
//                    console.error('Error AJAX GetVacantesProfesor:', xhr.responseText || xhr.statusText);
//                    $(".vacantes-lista").html(
//                        '<div class="vacante-alerta"><strong>Error:</strong> No se pudo cargar la información.</div>'
//                    );
//                });
//        }

//        function renderVacantes(vacantes) {
//            var $c = $(".vacantes-lista").empty();
//            if (!vacantes || vacantes.length === 0) {
//                $c.append('<div class="vacante-alerta"><strong>Información:</strong> No se encontraron vacantes.</div>');
//                return;
//            }
//            vacantes.forEach(function (v) {
//                if ((v.EstadoNombre || '').toLowerCase() === 'autogestionada') return; // blindaje
//                var aplicaciones = v.EstudiantesPostulados || 0;
//                var card = `
//          <article class="vacante-card" data-area="${escapeHtml(v.EspecialidadNombre || '')}">
//            <header class="vacante-header">
//              <h3 class="vacante-titulo">${escapeHtml(v.Nombre)}</h3>
//              <span class="vacante-empresa">${escapeHtml(v.EmpresaNombre)}</span>
//            </header>
//            <ul class="vacante-detalles">
//              <li><strong>Requisitos:</strong> ${escapeHtml(v.Requerimientos || '')}</li>
//              <li><strong>Modalidad:</strong> ${escapeHtml(v.ModalidadNombre || '')}</li>
//              <li><strong>Fecha límite de aplicación:</strong> ${formatFecha(v.FechaMaxAplicacion)}</li>
//              <li><strong>Número de cupos:</strong> ${v.NumCupos ?? 0}</li>
//              <li><strong>Número de aplicaciones:</strong> ${aplicaciones}</li>
//            </ul>
//            <div class="row g-2">
//              <div class="col-12 col-md-6">
//                <button class="w-100 btn btn-cta btn-detalle" data-id="${v.IdVacante}">Ver más</button>
//              </div>
//              <div class="col-12 col-md-6">
//                <button class="w-100 btn btn-cta btn-asignar" data-id="${v.IdVacante}" ${(v.NumCupos ?? 0) <= 0 ? 'disabled' : ''}>Asignar</button>
//              </div>
//            </div>
//          </article>`;
//                $c.append(card);
//            });
//        }


//        $(document).on('click', '.btn-detalle', function () {
//            var idVacante = $(this).data('id');
//            if (!idVacante) return;

//            $.getJSON(cfg.urls.detalle, { id: idVacante }, function (res) {
//                if (!res || !res.ok) { Swal.fire('Error', 'No se pudo cargar la vacante', 'error'); return; }
//                var d = res.data;


//                $('#vis-Nombre').text(d.Nombre || '');
//                $('#vis-Empresa').text(d.EmpresaNombre || '');
//                $('#vis-Descripcion').text(d.Descripcion || '');
//                $('#vis-Requisitos').text(d.Requerimientos || '');
//                $('#vis-Modalidad').text(d.ModalidadNombre || '');
//                $('#vis-Ubicacion').text(d.Ubicacion || '');
//                $('#vis-FechaAplicacion').text(formatFecha(d.FechaMaxAplicacion));
//                $('#vis-NombreContacto').text(d.NombreContacto || '-');
//                $('#vis-Telefonos').text((d.Telefonos && d.Telefonos.length) ? d.Telefonos.join(', ') : 'No disponible');
//                $('#vis-Emails').text((d.Emails && d.Emails.length) ? d.Emails.join(', ') : 'No disponible');

//                $('#modalVisualizar').modal('show');
//                cargarPostulaciones(idVacante);
//            });
//        });

//        function cargarPostulaciones(idVacante) {
//            $.getJSON(cfg.urls.obtenerPostulaciones, { idVacante }, function (res) {
//                var $lista = $('#listaPostulaciones').empty();

//                if (!res || !res.ok || !res.data || res.data.length === 0) {
//                    $('#mensajeSinAsignados').show();
//                    return;
//                }
//                $('#mensajeSinAsignados').hide();

//                res.data.forEach(function (p) {
//                    var estado = (p.EstadoDescripcion || p.EstadoVacante || '').trim();
//                    var url = cfg.urls.visualizacionPostulacion
//                        + '?idVacante=' + encodeURIComponent(idVacante)
//                        + '&idUsuario=' + encodeURIComponent(p.IdUsuario);

//                    var nombreLink = `<a href="${url}" class="text-decoration-none" style="color:#2d594d; font-weight:600;">${escapeHtml(p.NombreCompleto)}</a>`;
//                    var badge = badgeEstado(estado);


//                    var estadoLower = estado.toLowerCase();
//                    var mostrarBoton = ['asignada', 'en curso', 'en proceso de aplicacion'].includes(estadoLower);

//                    var botonDesasignar = mostrarBoton
//                        ? `<button class="btn bg-transparent btn-desasignar-estudiante"
//                data-idusuario="${p.IdUsuario}"
//                data-idvacante="${idVacante}"
//                title="Desasignar estudiante"
//                style="color:#2d594d">
//          <i class="fas fa-trash-alt"></i>
//       </button>`
//                        : '';

//                    $lista.append(`
//        <li class="list-group-item d-flex justify-content-between align-items-center flex-wrap">
//            <div class="col-12 col-md-6 mb-2 mb-md-0">${nombreLink}</div>
//            <div class="col-auto d-flex align-items-center gap-2">
//                ${badge}
//                ${botonDesasignar}
//            </div>
//        </li>
//    `);
//                });
//            });
//        }


//        $(document).on("click", ".btn-desasignar-estudiante", function (e) {
//            e.preventDefault();

//            const boton = $(this);
//            const idVacante = boton.data("idvacante");
//            const idUsuario = boton.data("idusuario");
//            const modalVisualizarEl = document.getElementById("modalVisualizar");
//            const modalVisualizar = bootstrap.Modal.getInstance(modalVisualizarEl);


//            if (modalVisualizar && modalVisualizar._focustrap) {
//                modalVisualizar._focustrap.deactivate();
//            }

//            Swal.fire({
//                title: '¿Desea desasignar este estudiante?',
//                text: 'El estado se cambiará a "Retirada".',
//                icon: 'warning',
//                input: 'textarea',
//                inputLabel: 'Comentario (opcional)',
//                inputPlaceholder: 'Escribe un comentario...',
//                inputAttributes: { 'aria-label': 'Comentario (opcional)' },
//                showCancelButton: true,
//                confirmButtonText: 'Sí, desasignar',
//                cancelButtonText: 'Cancelar',
//                confirmButtonColor: "#2D594D",
//                cancelButtonColor: "#6c757d",
//                allowOutsideClick: false,
//                didOpen: () => {
//                    const textarea = Swal.getInput();
//                    if (textarea) {
//                        textarea.removeAttribute("readonly");
//                        textarea.removeAttribute("disabled");
//                        setTimeout(() => textarea.focus(), 100);
//                    }
//                },
//                didClose: () => {
//                    if (modalVisualizar && modalVisualizar._focustrap) {
//                        modalVisualizar._focustrap.activate();
//                    }
//                }
//            }).then((result) => {
//                if (!result.isConfirmed) {

//                    if (modalVisualizar && modalVisualizar._focustrap) {
//                        modalVisualizar._focustrap.activate();
//                    }
//                    return;
//                }

//                const comentario = (result.value || '').trim();

//                function ejecutarRetiro() {
//                    $.post(cfg.urls.retirarEstudiante, {
//                        idVacante: idVacante,
//                        idUsuario: idUsuario
//                    })
//                        .done(function (resp) {
//                            if (resp && resp.ok) {
//                                Swal.fire({
//                                    title: "Desasignado",
//                                    text: resp.message || "El estudiante fue desasignado correctamente.",
//                                    icon: "success",
//                                    timer: 1500,
//                                    showConfirmButton: false
//                                }).then(() => {

//                                    cargarPostulaciones(idVacante);
//                                    filtrarVacantes();


//                                    $.get(cfg.urls.detalle, { id: idVacante })
//                                        .done(function (html) {
//                                            if (html && html.trim()) {
//                                                $("#modalVisualizar .modal-body").html(html);
//                                            }
//                                        });
//                                });
//                            } else {
//                                Swal.fire("Error", (resp && resp.message) || "No se pudo desasignar.", "error");
//                            }
//                        })
//                        .fail(function (xhr) {
//                            console.error("Error al llamar RetirarEstudiante:", xhr.responseText || xhr.statusText);
//                            Swal.fire("Error", "Ocurrió un error al procesar la solicitud.", "error");
//                        })
//                        .always(function () {
//                            if (modalVisualizar && modalVisualizar._focustrap) {
//                                modalVisualizar._focustrap.activate();
//                            }
//                        });
//                }

//                if (comentario) {
//                    $.post(cfg.urls.agregarComentario, {
//                        idVacante: idVacante,
//                        idUsuario: idUsuario,
//                        comentario: comentario
//                    })
//                        .done(function (res) {
//                            if (res && res.success) {

//                                ejecutarRetiro();
//                            } else {

//                                Swal.fire("Error", (res && res.message) || "No se pudo guardar el comentario.", "error")
//                                    .then(() => {
//                                        if (modalVisualizar && modalVisualizar._focustrap) {
//                                            modalVisualizar._focustrap.activate();
//                                        }
//                                    });
//                            }
//                        })
//                        .fail(function (xhr) {
//                            console.error("Error al agregar comentario:", xhr.responseText || xhr.statusText);
//                            Swal.fire("Error", "No se pudo guardar el comentario.", "error")
//                                .then(() => {
//                                    if (modalVisualizar && modalVisualizar._focustrap) {
//                                        modalVisualizar._focustrap.activate();
//                                    }
//                                });
//                        });
//                } else {

//                    ejecutarRetiro();
//                }
//            });
//        });




//        var tablaAsignar = $('#tablaAsignar').DataTable({
//            language: { url: '//cdn.datatables.net/plug-ins/1.13.6/i18n/es-ES.json' },
//            responsive: true, pageLength: 5, autoWidth: false, scrollX: false, destroy: true
//        });

//        $(document).on('click', '.btn-asignar', function () {
//            var idVacante = $(this).data('id');
//            $('#modalAsignarVacante').data('idvacante', idVacante);

//            $.getJSON(cfg.urls.obtenerEstudiantesAsignar, { idVacante }, function (res) {
//                tablaAsignar.clear();

//                if (!res.ok || !res.data || res.data.length === 0) {
//                    tablaAsignar.row.add(['—', '—', '—', '—', '<span class="text-danger">No hay estudiantes disponibles</span>']);
//                    tablaAsignar.draw(); return;
//                }

//                res.data.forEach(function (e) {
//                    var estado = (e.EstadoVacante || e.EstadoMostrar || '').trim();
//                    var est = (estado || '').toLowerCase();

//                    var cls =
//                        estado === 'Asignada' ? 'badge-asignada' :
//                            estado === 'Con Procesos Activos' ? 'badge-procesos-activos' :
//                                'badge-no-asignada';
//                    var badge = `<span class="badge ${cls}">${escapeHtml(estado || '—')}</span>`;

//                    var btn = '';
//                    if (e.TienePracticaActiva) {

//                        btn = `<button class="btn btn-sm btn-outline-secondary" disabled title="Ya tiene práctica activa">
//               <i class="fas fa-ban"></i> No disponible
//           </button>`;
//                    }
//                    else if (est === 'retirada') {

//                        btn = `<button class="btn btn-sm btn-verde-personalizado btn-reactivar-estudiante"
//                   data-idusuario="${e.IdUsuario}">
//               <i class="fas fa-redo"></i> Reactivar
//           </button>`;
//                    }
//                    else if (!['rechazada', 'aprobada', 'finalizada'].includes(est)) {

//                        btn = `<button class="btn btn-sm btn-outline-danger btn-retirar-estudiante"
//                   data-idusuario="${e.IdUsuario}">
//               <i class="fas fa-user-minus"></i> Retirar
//           </button>`;
//                    }
//                    else {

//                        btn = `<button class="btn btn-sm btn-verde-personalizado btn-asignar-estudiante"
//                   data-idusuario="${e.IdUsuario}">
//               <i class="fas fa-user-plus"></i> Asignar
//           </button>`;
//                    }

//                    tablaAsignar.row.add([
//                        escapeHtml(e.NombreCompleto),
//                        escapeHtml(e.Cedula),
//                        escapeHtml(e.Especialidad),
//                        badge,
//                        btn
//                    ]);
//                });

//                tablaAsignar.draw();
//            });

//            $('#modalAsignarVacante').modal('show');
//        });
//        //Asignar estudiante
//       // =====================================================
//// 🔹 Asignar estudiante (revisado)
//// =====================================================
//$(document).on('click', '.btn-asignar-estudiante', function () {
//    const idUsuario = $(this).data('idusuario');
//    const idVacante = $('#modalAsignarVacante').data('idvacante');

//    $.ajax({
//        url: cfg.urls.asignarEstudiante,
//        type: 'POST',
//        data: { idUsuario, idVacante },
//        success: function (res, status, xhr) {
//            // Si devolvió HTML (sesión expirada)
//            if ((xhr.getResponseHeader('content-type') || '').includes('text/html')) {
//                window.location.href = cfg.urls.login;
//                return;
//            }

//            if (res && res.ok) {
//                Swal.fire({
//                    icon: 'success',
//                    title: 'Éxito',
//                    text: res.message || 'Estudiante asignado correctamente.'
//                }).then(() => {
//                    // 🔄 Refresca la tabla de asignación y la lista general
//                    $('.btn-asignar[data-id="' + idVacante + '"]').trigger('click');
//                    filtrarVacantes();
//                });
//            } else {
//                Swal.fire('Error', (res && res.message) || 'No se pudo asignar.', 'error');
//            }
//        },
//        error: function (xhr) {
//            console.error('Error asignarEstudiante:', xhr.responseText);
//            Swal.fire('Error', 'Ocurrió un error al procesar la solicitud.', 'error');
//        }
//    });
//});

//// =====================================================
//// 🔹 Retirar estudiante (revisado)
//// =====================================================
//$(document).on('click', '.btn-retirar-estudiante', function () {
//    const idUsuario = $(this).data('idusuario');
//    const idVacante = $('#modalAsignarVacante').data('idvacante');

//    Swal.fire({
//        title: '¿Desea retirar al estudiante?',
//        text: 'El estado cambiará a "Retirada".',
//        icon: 'warning',
//        showCancelButton: true,
//        confirmButtonText: 'Sí, retirar',
//        cancelButtonText: 'Cancelar',
//        confirmButtonColor: '#2D594D',
//        cancelButtonColor: '#6c757d'
//    }).then(result => {
//        if (!result.isConfirmed) return;

//        $.ajax({
//            url: cfg.urls.retirarEstudiante,
//            type: 'POST',
//            data: { idUsuario, idVacante },
//            success: function (res, status, xhr) {
//                if ((xhr.getResponseHeader('content-type') || '').includes('text/html')) {
//                    window.location.href = cfg.urls.login;
//                    return;
//                }

//                if (res && res.ok) {
//                    Swal.fire({
//                        icon: 'success',
//                        title: 'Retirada',
//                        text: res.message || 'El estudiante fue retirado correctamente.'
//                    }).then(() => {
//                        $('.btn-asignar[data-id="' + idVacante + '"]').trigger('click');
//                        filtrarVacantes();
//                    });
//                } else {
//                    Swal.fire('Error', (res && res.message) || 'No se pudo retirar.', 'error');
//                }
//            },
//            error: function (xhr) {
//                console.error('Error retirarEstudiante:', xhr.responseText);
//                Swal.fire('Error', 'Ocurrió un error al procesar la solicitud.', 'error');
//            }
//        });
//    });
//});


//        //retirar estudiante

//        $(document).on('click', '.btn-retirar-estudiante', function () {
//            var idUsuario = $(this).data('idusuario');
//            var idVacante = $('#modalAsignarVacante').data('idvacante');

//            $.post(cfg.urls.retirarEstudiante, { idUsuario, idVacante }, function (resp) {
//                if (resp && resp.ok) {
//                    Swal.fire('Listo', resp.message || 'Marcado como Retirada.', 'success');
//                    $('.btn-asignar[data-id="' + idVacante + '"]').trigger('click');
//                    filtrarVacantes();
//                } else {
//                    Swal.fire('Error', (resp && resp.message) || 'No se pudo retirar.', 'error');
//                }
//            });
//        });

//        $(document).on('click', '.btn-reactivar-estudiante', function () {
//            var idUsuario = $(this).data('idusuario');
//            var idVacante = $('#modalAsignarVacante').data('idvacante');

//            $.post(cfg.urls.asignarEstudiante, { idUsuario, idVacante }, function (resp) {
//                if (resp && resp.ok) {
//                    Swal.fire('Listo', resp.message || 'Reactivado a "En proceso".', 'success');
//                    $('.btn-asignar[data-id="' + idVacante + '"]').trigger('click');
//                    filtrarVacantes();
//                } else {
//                    Swal.fire('Error', (resp && resp.message) || 'No se pudo reactivar.', 'error');
//                }
//            });
//        });

//    })
//})(jQuery);


///NUEVO CODIGO COMENTADO

//(function ($) {
//    $(function () {
//        const CFG = window.VacantesProfesorCfg || { urls: {}, rol: 0 };

//        // =====================================================
//        // 🔹 Helpers
//        // =====================================================
//        function redirSiLogin(res, xhr) {
//            try {
//                const ct = (xhr && xhr.getResponseHeader && xhr.getResponseHeader('content-type')) || '';
//                if ((typeof res === 'string' && res.indexOf('<!DOCTYPE html') >= 0) ||
//                    (ct && ct.indexOf('text/html') >= 0)) {
//                    window.location.href = CFG.urls.login;
//                    return true;
//                }
//            } catch (e) { }
//            return false;
//        }

//        function escapeHtml(text) {
//            if (!text && text !== 0) return '';
//            return $('<div>').text(text).html();
//        }

//        function normalizarEstado(str) {
//            return (str || '')
//                .toString()
//                .normalize('NFD').replace(/[\u0300-\u036f]/g, '')
//                .toLowerCase()
//                .replace(/\s+/g, ' ')
//                .trim();
//        }

//        function badgeEstado(estadoOriginal) {
//            const est = normalizarEstado(estadoOriginal);

//            const mapa = {
//                'en proceso de aplicacion': { cls: 'badge-en-progreso', txt: 'En proceso de Aplicación' },
//                'rechazada': { cls: 'badge-rechazada', txt: 'Rechazada' },
//                'asignada': { cls: 'badge-asignada', txt: 'Asignada' },
//                'aprobada': { cls: 'badge-aprobada', txt: 'Aprobada' },
//                'retirada': { cls: 'badge-retirada', txt: 'Retirada' },
//                'finalizada': { cls: 'badge-finalizada', txt: 'Finalizada' },
//                'rezagado': { cls: 'badge-rezagado', txt: 'Rezagado' },
//                'archivado': { cls: 'badge-archivado', txt: 'Archivado' },
//                'en curso': { cls: 'badge-en-curso', txt: 'En Curso' },
//                'activo': { cls: 'badge-activo', txt: 'Activo' },
//                'inactivo': { cls: 'badge-inactivo', txt: 'Inactivo' },
//                'sin proceso activo': { cls: 'badge-no-asignada', txt: 'Sin proceso activo' }
//            };

//            const info = mapa[est] || { cls: 'badge-no-asignada', txt: estadoOriginal || '—' };
//            return `<span class="badge ${info.cls}">${info.txt}</span>`;
//        }

//        // =====================================================
//        // 🔹 Filtros de Vacantes Profesor
//        // =====================================================
//        filtrarVacantes();
//        $("#filtroPractica,#filtroModalidad").on('change', filtrarVacantes);

//        function filtrarVacantes() {
//            const estado = ($("#filtroPractica").val() || '').toString().trim().toLowerCase();
//            const modalidad = parseInt($("#filtroModalidad").val() || '0', 10) || 0;

//            $.getJSON(CFG.urls.getVacantesProfesor, { estado, idModalidad: modalidad })
//                .done(function (resp) {
//                    if (resp && resp.ok) renderVacantes(resp.data);
//                    else $(".vacantes-lista").html('<div class="vacante-alerta"><strong>Información:</strong> No se encontraron vacantes.</div>');
//                })
//                .fail(function (xhr) {
//                    console.error('Error AJAX GetVacantesProfesor:', xhr.responseText || xhr.statusText);
//                    $(".vacantes-lista").html('<div class="vacante-alerta"><strong>Error:</strong> No se pudo cargar la información.</div>');
//                });
//        }

//        function renderVacantes(vacantes) {
//            const $c = $(".vacantes-lista").empty();
//            if (!vacantes || vacantes.length === 0) {
//                $c.append('<div class="vacante-alerta"><strong>Información:</strong> No se encontraron vacantes.</div>');
//                return;
//            }

//            vacantes.forEach(v => {
//                if ((v.EstadoNombre || '').toLowerCase() === 'autogestionada') return;
//                const aplicaciones = v.EstudiantesPostulados || 0;

//                const card = `
//                    <article class="vacante-card" data-area="${escapeHtml(v.EspecialidadNombre || '')}">
//                        <header class="vacante-header">
//                            <h3 class="vacante-titulo">${escapeHtml(v.Nombre)}</h3>
//                            <span class="vacante-empresa">${escapeHtml(v.EmpresaNombre)}</span>
//                        </header>
//                        <ul class="vacante-detalles">
//                            <li><strong>Requisitos:</strong> ${escapeHtml(v.Requerimientos || '')}</li>
//                            <li><strong>Modalidad:</strong> ${escapeHtml(v.ModalidadNombre || '')}</li>
//                            <li><strong>Fecha límite:</strong> ${escapeHtml(v.FechaMaxAplicacion?.split('T')[0] || '')}</li>
//                            <li><strong>Cupos:</strong> ${v.NumCupos ?? 0}</li>
//                            <li><strong>Postulados:</strong> ${aplicaciones}</li>
//                        </ul>
//                        <div class="row g-2">
//                            <div class="col-12 col-md-6">
//                                <button class="w-100 btn btn-cta btn-detalle" data-id="${v.IdVacante}">Ver más</button>
//                            </div>
//                            <div class="col-12 col-md-6">
//                                <button class="w-100 btn btn-cta btn-asignar" data-id="${v.IdVacante}" ${(v.NumCupos ?? 0) <= 0 ? 'disabled' : ''}>Asignar</button>
//                            </div>
//                        </div>
//                    </article>`;
//                $c.append(card);
//            });
//        }

//        // =====================================================
//        // 🔹 Modal Asignar — mismos botones que Vacantes.js
//        // =====================================================
//        const tabla = $('#tablaAsignar').DataTable({
//            language: { url: '//cdn.datatables.net/plug-ins/1.13.6/i18n/es-ES.json' },
//            responsive: true,
//            pageLength: 5,
//            autoWidth: false,
//            destroy: true
//        });

//        $(document).on('click', '.btn-asignar', function () {
//            const idVacante = $(this).data('id');
//            $('#modalAsignarVacante').data('idvacante', idVacante).modal('show');
//            cargarEstudiantesAsignar(idVacante);
//        });

//        function cargarEstudiantesAsignar(idVacante) {
//            $.getJSON(CFG.urls.obtenerEstudiantesAsignar, { idVacante }, function (res) {
//                tabla.clear();

//                if (!res?.ok || !res.data?.length) {
//                    tabla.row.add(['—', '—', '—', '—', '<span class="text-danger">No hay estudiantes disponibles</span>']);
//                    tabla.draw();
//                    return;
//                }

//                res.data.forEach(e => {
//                    const estado = normalizarEstado(e.EstadoVacante || e.EstadoPractica || 'Sin proceso activo');
//                    const badge = badgeEstado(e.EstadoVacante || e.EstadoPractica);

//                    let btn = '';
//                    if (e.TienePracticaActiva && estado !== 'asignada') {
//                        btn = `<button class="btn btn-sm btn-outline-secondary" disabled>
//                                   <i class="fas fa-ban"></i> No disponible
//                               </button>`;
//                    }
//                    else if (estado === 'asignada') {
//                        btn = `<button class="btn btn-sm btn-outline-warning btn-retirar-estudiante"
//                                   data-idusuario="${e.IdUsuario}">
//                                   <i class="fas fa-user-minus"></i> Retirar
//                               </button>`;
//                    }
//                    else if (['rechazada', 'aprobada', 'finalizada', 'en curso'].includes(estado)) {
//                        btn = `<button class="btn btn-sm btn-outline-secondary" disabled>
//                                   <i class="fas fa-ban"></i> No disponible
//                               </button>`;
//                    }
//                    else {
//                        btn = `<button class="btn btn-sm btn-outline-success btn-asignar-estudiante"
//                                   data-idusuario="${e.IdUsuario}">
//                                   <i class="fas fa-user-plus"></i> Asignar
//                               </button>`;
//                    }

//                    tabla.row.add([
//                        escapeHtml(e.NombreCompleto),
//                        escapeHtml(e.Cedula || ''),
//                        escapeHtml(e.Especialidad || ''),
//                        badge,
//                        btn
//                    ]);
//                });
//                tabla.draw();
//            });
//        }

//        // =====================================================
//        // 🔹 Asignar estudiante (idéntico a Vacantes)
//        // =====================================================
//        $(document).on('click', '.btn-asignar-estudiante', function () {
//            const idUsuario = $(this).data('idusuario');
//            const idVacante = $('#modalAsignarVacante').data('idvacante');

//            Swal.fire({
//                title: 'Confirmar asignación',
//                text: '¿Deseas asignar este estudiante a la vacante?',
//                icon: 'question',
//                showCancelButton: true,
//                confirmButtonText: 'Sí, asignar',
//                cancelButtonText: 'Cancelar',
//                confirmButtonColor: '#2D594D'
//            }).then(r => {
//                if (!r.isConfirmed) return;

//                $.post(CFG.urls.asignarEstudiante, { idUsuario, idVacante })
//                    .done(res => {
//                        if (res && res.ok) {
//                            Swal.fire('Éxito', res.message || 'Asignado correctamente.', 'success');
//                            cargarEstudiantesAsignar(idVacante);
//                            filtrarVacantes();
//                        } else {
//                            Swal.fire('Error', res.message || 'No se pudo asignar.', 'error');
//                        }
//                    })
//                    .fail(() => Swal.fire('Error', 'Error al asignar estudiante.', 'error'));
//            });
//        });

//        // =====================================================
//        // 🔹 Retirar estudiante (idéntico a Vacantes)
//        // =====================================================
//        $(document).on('click', '.btn-retirar-estudiante', function () {
//            const idUsuario = $(this).data('idusuario');
//            const idVacante = $('#modalAsignarVacante').data('idvacante');

//            Swal.fire({
//                title: '¿Deseas retirar al estudiante?',
//                text: 'El estado cambiará a "Retirada".',
//                icon: 'warning',
//                showCancelButton: true,
//                confirmButtonText: 'Sí, retirar',
//                cancelButtonText: 'Cancelar',
//                confirmButtonColor: '#2D594D'
//            }).then(r => {
//                if (!r.isConfirmed) return;

//                $.post(CFG.urls.retirarEstudiante, { idUsuario, idVacante })
//                    .done(res => {
//                        if (res && res.ok) {
//                            Swal.fire('Retirada', res.message || 'Estudiante retirado correctamente.', 'success');
//                            cargarEstudiantesAsignar(idVacante);
//                            filtrarVacantes();
//                        } else {
//                            Swal.fire('Error', res.message || 'No se pudo retirar.', 'error');
//                        }
//                    })
//                    .fail(() => Swal.fire('Error', 'Error al retirar estudiante.', 'error'));
//            });
//        });

//    });
//})(jQuery);


//CODIGO CON TODO INTEGRADO
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
                const ticks = parseInt(val.substr(6), 10);
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

                    res.data.forEach(p => {
                        const estado = (p.EstadoDescripcion || p.EstadoVacante || '').trim();
                        const url = CFG.urls.visualizacionPostulacion
                            + '?idVacante=' + encodeURIComponent(idVacante)
                            + '&idUsuario=' + encodeURIComponent(p.IdUsuario);

                        const nombreLink = `
                            <a href="${url}"
                               class="text-decoration-none"
                               style="color:#2d594d; font-weight:600;">
                               ${escapeHtml(p.NombreCompleto)}
                            </a>`;

                        const badge = badgeEstado(estado);
                        const estNorm = normalizarEstado(estado);

                        // Solo permitir desasignar cuando aplica para esta vacante
                        const mostrarBoton = ['asignada', 'en curso', 'en proceso de aplicacion'].includes(estNorm);

                        const botonDesasignar = mostrarBoton
                            ? `<button class="btn bg-transparent btn-desasignar-estudiante"
                                           data-idusuario="${p.IdUsuario}"
                                           data-idvacante="${idVacante}"
                                           title="Desasignar estudiante"
                                           style="color:#2d594d">
                                   <i class="fas fa-trash-alt"></i>
                               </button>`
                            : '';

                        $lista.append(`
                            <li class="list-group-item d-flex justify-content-between align-items-center flex-wrap">
                                <div class="col-12 col-md-6 mb-2 mb-md-0">${nombreLink}</div>
                                <div class="col-auto d-flex align-items-center gap-2">
                                    ${badge}
                                    ${botonDesasignar}
                                </div>
                            </li>`);
                    });
                })
                .fail(xhr => {
                    console.error('Error obtenerPostulaciones:', xhr.responseText || xhr.statusText);
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

        function cargarEstudiantesAsignar(idVacante) {
            $.getJSON(CFG.urls.obtenerEstudiantesAsignar, { idVacante })
                .done(res => {
                    tablaAsignar.clear();

                    if (!res || !res.ok || !res.data || res.data.length === 0) {
                        tablaAsignar.row.add([
                            '—', '—', '—',
                            '—',
                            '<span class="text-danger">No hay estudiantes disponibles</span>'
                        ]);
                        tablaAsignar.draw();
                        return;
                    }

                    res.data.forEach(e => {
                        const estadoRaw = e.EstadoVacante || e.EstadoPractica || 'Sin proceso activo';
                        const estado = normalizarEstado(estadoRaw);
                        const badge = badgeEstado(estadoRaw);
                        const tieneRelacion = !!e.TieneRelacionEnVacante;
                        const tieneActiva = !!e.TienePracticaActiva;

                        let btnHtml = '';

                        // 1️⃣ Tiene práctica activa en otra vacante → bloquear
                        if (tieneActiva && !tieneRelacion) {
                            btnHtml = `
                                <button class="btn btn-sm btn-outline-secondary" disabled
                                        title="Ya tiene práctica activa en otra vacante">
                                    <i class="fas fa-ban"></i> No disponible
                                </button>`;
                        }
                        // 2️⃣ Relación con esta vacante
                        else if (tieneRelacion) {
                            if (estado === 'asignada') {
                                // Puede retirar de ESTA vacante
                                btnHtml = `
                                    <button class="btn btn-sm btn-outline-danger btn-retirar-estudiante"
                                            data-idusuario="${e.IdUsuario}">
                                        <i class="fas fa-user-minus"></i> Retirar
                                    </button>`;
                            }
                            else if (estado === 'en proceso de aplicacion' ||
                                estado === 'retirada' ||
                                estado === 'sin proceso activo') {
                                // Usamos mismo botón Asignar (el backend decide: nuevo, pasar a asignada, reactivar, etc.)
                                btnHtml = `
                                    <button class="btn btn-sm btn-outline-success btn-asignar-estudiante"
                                            data-idusuario="${e.IdUsuario}">
                                        <i class="fas fa-user-plus"></i> Asignar
                                    </button>`;
                            }
                            else {
                                // Estados finales o bloqueantes en esta vacante
                                btnHtml = `
                                    <button class="btn btn-sm btn-outline-secondary" disabled>
                                        <i class="fas fa-ban"></i> No disponible
                                    </button>`;
                            }
                        }
                        // 3️⃣ No relación aún + sin práctica activa bloqueante → puede asignarse
                        else {
                            if (['rechazada', 'aprobada', 'finalizada', 'en curso', 'archivado', 'rezagado'].includes(estado)) {
                                btnHtml = `
                                    <button class="btn btn-sm btn-outline-secondary" disabled>
                                        <i class="fas fa-ban"></i> No disponible
                                    </button>`;
                            } else {
                                btnHtml = `
                                    <button class="btn btn-sm btn-outline-success btn-asignar-estudiante"
                                            data-idusuario="${e.IdUsuario}">
                                        <i class="fas fa-user-plus"></i> Asignar
                                    </button>`;
                            }
                        }

                        tablaAsignar.row.add([
                            escapeHtml(e.NombreCompleto || ''),
                            escapeHtml(e.Cedula || ''),
                            escapeHtml(e.Especialidad || ''),
                            badge,
                            btnHtml
                        ]);
                    });

                    tablaAsignar.draw();
                })
                .fail(xhr => {
                    console.error('Error obtenerEstudiantesAsignar:', xhr.responseText || xhr.statusText);
                    tablaAsignar.clear().row.add([
                        '—', '—', '—', '—',
                        '<span class="text-danger">Error al cargar estudiantes</span>'
                    ]).draw();
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
        $(document).on('click', '.btn-retirar-estudiante', function () {
            const idUsuario = $(this).data('idusuario');
            const idVacante = $('#modalAsignarVacante').data('idvacante');

            Swal.fire({
                title: '¿Deseas retirar al estudiante?',
                text: 'El estado se cambiará a "Retirada" para esta vacante.',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: 'Sí, retirar',
                cancelButtonText: 'Cancelar',
                confirmButtonColor: '#2D594D',
                cancelButtonColor: '#6c757d'
            }).then(r => {
                if (!r.isConfirmed) return;

                $.ajax({
                    url: CFG.urls.retirarEstudiante,
                    type: 'POST',
                    data: { idVacante, idUsuario },
                    success: function (res, status, xhr) {
                        if (redirSiLogin(res, xhr)) return;

                        if (res && res.ok) {
                            Swal.fire({
                                icon: 'success',
                                title: 'Retirada',
                                text: res.message || 'El estudiante fue retirado correctamente.',
                                timer: 1800,
                                showConfirmButton: false
                            });
                            cargarEstudiantesAsignar(idVacante);
                            filtrarVacantes();
                        } else {
                            Swal.fire('Error', (res && res.message) || 'No se pudo retirar.', 'error');
                        }
                    },
                    error: function (xhr) {
                        console.error('Error retirarEstudiante:', xhr.responseText || xhr.statusText);
                        Swal.fire('Error', 'Ocurrió un error al procesar la solicitud.', 'error');
                    }
                });
            });
        });

    });
})(jQuery);

