(function ($) {
    $(function () {
        const cfg = window.VacantesProfesorCfg || { urls: {} };

       
        function escapeHtml(text) { if (!text && text !== 0) return ''; return $('<div>').text(text).html(); }
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

      
        function badgeEstado(estadoOriginal) {
            var est = (estadoOriginal || '')
                .toString()
                .normalize('NFD').replace(/[\u0300-\u036f]/g, '')
                .toLowerCase().replace(/\s+/g, ' ').trim();

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

       
        filtrarVacantes();
        $("#filtroPractica,#filtroModalidad").on('change', filtrarVacantes);

        function filtrarVacantes() {
           
            var estado = ($("#filtroPractica").val() || '').toString().trim().toLowerCase();
            var modalidad = parseInt($("#filtroModalidad").val() || '0', 10) || 0;

            $.getJSON(cfg.urls.getVacantesProfesor, { estado: estado, idModalidad: modalidad })
                .done(function (resp) {
                    if (resp && resp.ok) renderVacantes(resp.data);
                    else $(".vacantes-lista").html(
                        '<div class="vacante-alerta"><strong>Información:</strong> No se encontraron vacantes.</div>'
                    );
                })
                .fail(function (xhr) {
                    console.error('Error AJAX GetVacantesProfesor:', xhr.responseText || xhr.statusText);
                    $(".vacantes-lista").html(
                        '<div class="vacante-alerta"><strong>Error:</strong> No se pudo cargar la información.</div>'
                    );
                });
        } 

        function renderVacantes(vacantes) {
            var $c = $(".vacantes-lista").empty();
            if (!vacantes || vacantes.length === 0) {
                $c.append('<div class="vacante-alerta"><strong>Información:</strong> No se encontraron vacantes.</div>');
                return;
            }
            vacantes.forEach(function (v) {
                if ((v.EstadoNombre || '').toLowerCase() === 'autogestionada') return; // blindaje
                var aplicaciones = v.EstudiantesPostulados || 0;
                var card = `
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
                <button class="w-100 btn btn-cta btn-detalle" data-id="${v.IdVacante}">Ver más</button>
              </div>
              <div class="col-12 col-md-6">
                <button class="w-100 btn btn-cta btn-asignar" data-id="${v.IdVacante}" ${(v.NumCupos ?? 0) <= 0 ? 'disabled' : ''}>Asignar</button>
              </div>
            </div>
          </article>`;
                $c.append(card);
            });
        }

      
        $(document).on('click', '.btn-detalle', function () {
            var idVacante = $(this).data('id');
            if (!idVacante) return;

            $.getJSON(cfg.urls.detalle, { id: idVacante }, function (res) {
                if (!res || !res.ok) { Swal.fire('Error', 'No se pudo cargar la vacante', 'error'); return; }
                var d = res.data;

              
                $('#vis-Nombre').text(d.Nombre || '');
                $('#vis-Empresa').text(d.EmpresaNombre || '');
                $('#vis-Descripcion').text(d.Descripcion || '');
                $('#vis-Requisitos').text(d.Requerimientos || '');
                $('#vis-Modalidad').text(d.ModalidadNombre || '');
                $('#vis-Ubicacion').text(d.Ubicacion || '');
                $('#vis-FechaAplicacion').text(formatFecha(d.FechaMaxAplicacion));
                $('#vis-NombreContacto').text(d.NombreContacto || '-');
                $('#vis-Telefonos').text((d.Telefonos && d.Telefonos.length) ? d.Telefonos.join(', ') : 'No disponible');
                $('#vis-Emails').text((d.Emails && d.Emails.length) ? d.Emails.join(', ') : 'No disponible');

                $('#modalVisualizar').modal('show');
                cargarPostulaciones(idVacante);
            });
        });

        function cargarPostulaciones(idVacante) {
            $.getJSON(cfg.urls.obtenerPostulaciones, { idVacante }, function (res) {
                var $lista = $('#listaPostulaciones').empty();

                if (!res || !res.ok || !res.data || res.data.length === 0) {
                    $('#mensajeSinAsignados').show();
                    return;
                }
                $('#mensajeSinAsignados').hide();

                res.data.forEach(function (p) {
                    var estado = (p.EstadoDescripcion || p.EstadoVacante || '').trim();
                    var url = cfg.urls.visualizacionPostulacion
                        + '?idVacante=' + encodeURIComponent(idVacante)
                        + '&idUsuario=' + encodeURIComponent(p.IdUsuario);

                    var nombreLink = `<a href="${url}" class="text-decoration-none" style="color:#2d594d; font-weight:600;">${escapeHtml(p.NombreCompleto)}</a>`;
                    var badge = badgeEstado(estado);

                    
                    var estadoLower = estado.toLowerCase();
                    var mostrarBoton = ['asignada', 'en curso', 'en proceso de aplicacion'].includes(estadoLower);

                    var botonDesasignar = mostrarBoton
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
        </li>
    `);
                });
            });
        }

       
        $(document).on("click", ".btn-desasignar-estudiante", function (e) {
            e.preventDefault();

            const boton = $(this);
            const idVacante = boton.data("idvacante");
            const idUsuario = boton.data("idusuario");
            const modalVisualizarEl = document.getElementById("modalVisualizar");
            const modalVisualizar = bootstrap.Modal.getInstance(modalVisualizarEl);

           
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
                inputAttributes: { 'aria-label': 'Comentario (opcional)' },
                showCancelButton: true,
                confirmButtonText: 'Sí, desasignar',
                cancelButtonText: 'Cancelar',
                confirmButtonColor: "#2D594D",
                cancelButtonColor: "#6c757d",
                allowOutsideClick: false,
                didOpen: () => {
                    const textarea = Swal.getInput();
                    if (textarea) {
                        textarea.removeAttribute("readonly");
                        textarea.removeAttribute("disabled");
                        setTimeout(() => textarea.focus(), 100);
                    }
                },
                didClose: () => {
                    if (modalVisualizar && modalVisualizar._focustrap) {
                        modalVisualizar._focustrap.activate();
                    }
                }
            }).then((result) => {
                if (!result.isConfirmed) {
                   
                    if (modalVisualizar && modalVisualizar._focustrap) {
                        modalVisualizar._focustrap.activate();
                    }
                    return;
                }

                const comentario = (result.value || '').trim();

                function ejecutarRetiro() {
                    $.post(cfg.urls.retirarEstudiante, {
                        idVacante: idVacante,
                        idUsuario: idUsuario
                    })
                        .done(function (resp) {
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

                                    
                                    $.get(cfg.urls.detalle, { id: idVacante })
                                        .done(function (html) {
                                            if (html && html.trim()) {
                                                $("#modalVisualizar .modal-body").html(html);
                                            }
                                        });
                                });
                            } else {
                                Swal.fire("Error", (resp && resp.message) || "No se pudo desasignar.", "error");
                            }
                        })
                        .fail(function (xhr) {
                            console.error("Error al llamar RetirarEstudiante:", xhr.responseText || xhr.statusText);
                            Swal.fire("Error", "Ocurrió un error al procesar la solicitud.", "error");
                        })
                        .always(function () {
                            if (modalVisualizar && modalVisualizar._focustrap) {
                                modalVisualizar._focustrap.activate();
                            }
                        });
                }

                if (comentario) {
                    $.post(cfg.urls.agregarComentario, {
                        idVacante: idVacante,
                        idUsuario: idUsuario,
                        comentario: comentario
                    })
                        .done(function (res) {
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
                        .fail(function (xhr) {
                            console.error("Error al agregar comentario:", xhr.responseText || xhr.statusText);
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




        var tablaAsignar = $('#tablaAsignar').DataTable({
            language: { url: '//cdn.datatables.net/plug-ins/1.13.6/i18n/es-ES.json' },
            responsive: true, pageLength: 5, autoWidth: false, scrollX: false, destroy: true
        });

        $(document).on('click', '.btn-asignar', function () {
            var idVacante = $(this).data('id');
            $('#modalAsignarVacante').data('idvacante', idVacante);

            $.getJSON(cfg.urls.obtenerEstudiantesAsignar, { idVacante }, function (res) {
                tablaAsignar.clear();

                if (!res.ok || !res.data || res.data.length === 0) {
                    tablaAsignar.row.add(['—', '—', '—', '—', '<span class="text-danger">No hay estudiantes disponibles</span>']);
                    tablaAsignar.draw(); return;
                }

                res.data.forEach(function (e) {
                    var estado = (e.EstadoVacante || e.EstadoMostrar || '').trim();
                    var est = (estado || '').toLowerCase();

                    var cls =
                        estado === 'Asignada' ? 'badge-asignada' :
                            estado === 'Con Procesos Activos' ? 'badge-procesos-activos' :
                                'badge-no-asignada';
                    var badge = `<span class="badge ${cls}">${escapeHtml(estado || '—')}</span>`;

                    var btn = '';
                    if (e.TienePracticaActiva) {
                       
                        btn = `<button class="btn btn-sm btn-outline-secondary" disabled title="Ya tiene práctica activa">
               <i class="fas fa-ban"></i> No disponible
           </button>`;
                    }
                    else if (est === 'retirada') {
                      
                        btn = `<button class="btn btn-sm btn-verde-personalizado btn-reactivar-estudiante"
                   data-idusuario="${e.IdUsuario}">
               <i class="fas fa-redo"></i> Reactivar
           </button>`;
                    }
                    else if (!['rechazada', 'aprobada', 'finalizada'].includes(est)) {
                      
                        btn = `<button class="btn btn-sm btn-outline-danger btn-retirar-estudiante"
                   data-idusuario="${e.IdUsuario}">
               <i class="fas fa-user-minus"></i> Retirar
           </button>`;
                    }
                    else {
                        
                        btn = `<button class="btn btn-sm btn-verde-personalizado btn-asignar-estudiante"
                   data-idusuario="${e.IdUsuario}">
               <i class="fas fa-user-plus"></i> Asignar
           </button>`;
                    }

                    tablaAsignar.row.add([
                        escapeHtml(e.NombreCompleto),
                        escapeHtml(e.Cedula),
                        escapeHtml(e.Especialidad),
                        badge,
                        btn
                    ]);
                });

                tablaAsignar.draw();
            });

            $('#modalAsignarVacante').modal('show');
        });

        $(document).on('click', '.btn-asignar-estudiante', function () {
            var idUsuario = $(this).data('idusuario');
            var idVacante = $('#modalAsignarVacante').data('idvacante');

            $.post(cfg.urls.asignarEstudiante, { idUsuario, idVacante }, function (resp) {
                if (resp && resp.ok) {
                    Swal.fire('Éxito', resp.message, 'success');
                    $('.btn-asignar[data-id="' + idVacante + '"]').trigger('click');
                    filtrarVacantes();
                } else {
                    Swal.fire('Error', (resp && resp.message) || 'No se pudo asignar.', 'error');
                }
            });
        });

        $(document).on('click', '.btn-retirar-estudiante', function () {
            var idUsuario = $(this).data('idusuario');
            var idVacante = $('#modalAsignarVacante').data('idvacante');

            $.post(cfg.urls.retirarEstudiante, { idUsuario, idVacante }, function (resp) {
                if (resp && resp.ok) {
                    Swal.fire('Listo', resp.message || 'Marcado como Retirada.', 'success');
                    $('.btn-asignar[data-id="' + idVacante + '"]').trigger('click');
                    filtrarVacantes();
                } else {
                    Swal.fire('Error', (resp && resp.message) || 'No se pudo retirar.', 'error');
                }
            });
        });

        $(document).on('click', '.btn-reactivar-estudiante', function () {
            var idUsuario = $(this).data('idusuario');
            var idVacante = $('#modalAsignarVacante').data('idvacante');

            $.post(cfg.urls.asignarEstudiante, { idUsuario, idVacante }, function (resp) {
                if (resp && resp.ok) {
                    Swal.fire('Listo', resp.message || 'Reactivado a "En proceso".', 'success');
                    $('.btn-asignar[data-id="' + idVacante + '"]').trigger('click');
                    filtrarVacantes();
                } else {
                    Swal.fire('Error', (resp && resp.message) || 'No se pudo reactivar.', 'error');
                }
            });
        });

    })
})(jQuery);
