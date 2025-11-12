$(function () {
    console.log("✅ PracticasCoordinador.js cargado correctamente");

    if (window._PracticasScriptLoaded) {
        console.warn("⚠️ PracticasCoordinador.js se está cargando más de una vez.");
    } else {
        window._PracticasScriptLoaded = true;
        console.log("🟢 Script PracticasCoordinador.js cargado por primera vez.");
    }

    // === Helper para mostrar badges según el estado de práctica ===
    function badgeEstado(estadoOriginal) {
        const estado = (estadoOriginal || '').normalize('NFD')
            .replace(/[\u0300-\u036f]/g, '')
            .toLowerCase()
            .trim();

        const mapa = {
            'en proceso de aplicacion': { cls: 'badge-en-proceso', txt: 'En proceso de Aplicación' },
            'rechazada': { cls: 'badge-rechazada', txt: 'Rechazada' },
            'asignada': { cls: 'badge-asignada', txt: 'Asignada' },
            'aprobada': { cls: 'badge-aprobada', txt: 'Aprobada' },
            'retirada': { cls: 'badge-retirada', txt: 'Retirada' },
            'finalizada': { cls: 'badge-finalizada', txt: 'Finalizada' },
            'rezagado': { cls: 'badge-rezagado', txt: 'Rezagado' },
            'archivada': { cls: 'badge-archivado', txt: 'Archivada' },
            'en curso': { cls: 'badge-en-curso', txt: 'En Curso' },
            'sin proceso activo': { cls: 'badge-no-asignada', txt: 'Sin proceso activo' }
        };

        const info = mapa[estado] || { cls: 'badge-no-asignada', txt: estadoOriginal || 'Sin proceso activo' };
        return `<span class="badge ${info.cls}">${info.txt}</span>`;
    }

    // === Refrescar tabla del modal de asignación (para actualizar íconos sin cerrar) ===
    function recargarModalVacantes(idUsuario) {
        const tbody = $('#miTablaAsignar tbody');
        if (!tbody.length) return;

        tbody.html('<tr><td colspan="7" class="text-center text-muted">Actualizando vacantes...</td></tr>');

        $.getJSON(`/Practicas/ObtenerVacantesAsignar?idUsuario=${idUsuario}`)
            .done(res => {
                tbody.empty();

                if (!res.ok || !res.data?.length) {
                    tbody.html('<tr><td colspan="7" class="text-center text-muted">No hay vacantes disponibles</td></tr>');
                    return;
                }

                res.data.forEach(v => {
                    const estado = (v.EstadoVacante || v.EstadoDescripcion || v.EstadoPractica || 'Sin proceso activo')
                        .normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase().trim();

                    let btn = '';
                    if (['sin proceso activo', 'retirada', 'en proceso de aplicacion'].includes(estado)) {
                        btn = `
                            <button class="btn bg-transparent btn-accion btn-asignar-estudiante"
                                data-idvacante="${v.IdVacante}"
                                data-idusuario="${idUsuario}"
                                title="Asignar o confirmar asignación"
                                style="color:#198754;">
                                <i class="fas fa-user-plus fa-lg"></i>
                            </button>`;
                    } else if (estado === 'asignada' && v.IdPracticaVacante > 0) {
                        btn = `
                            <button class="btn bg-transparent btn-accion btn-retirar-estudiante"
                                data-idpractica="${v.IdPracticaVacante}"
                                data-idusuario="${idUsuario}"
                                data-nombre="${(v.NombreCompleto || v.NombreEstudiante || '—').replace(/"/g, '&quot;')}"
                                data-estadoacademico="${v.EstadoAcademicoDescripcion || 'Activo'}"
                                title="Retirar estudiante"
                                style="color:#b02a37;">
                                <i class="fas fa-trash-alt fa-lg"></i>
                            </button>`;
                    } else {
                        btn = `
                            <button class="btn bg-transparent btn-accion btn-bloqueado"
                                title="No disponible"
                                style="color:#6c757d;" disabled>
                                <i class="fas fa-ban fa-lg"></i>
                            </button>`;
                    }

                    const estadoReal = v.EstadoPractica || v.EstadoPracticaVacante || 'Sin proceso activo';

                    tbody.append(`
                        <tr>
                            <td>${v.NombreVacante}</td>
                            <td>${v.NombreEmpresa}</td>
                            <td>${v.Especialidad}</td>
                            <td>${v.NumCupos}</td>
                            <td>${v.CuposOcupados}</td>
                            <td class="text-center">${badgeEstado(estadoReal)}</td>
                            <td>${btn}</td>
                        </tr>`);
                });
            })
            .fail(() => {
                tbody.html('<tr><td colspan="7" class="text-center text-danger">Error al recargar vacantes</td></tr>');
            });
    }

    // === Recargar tabla de vacantes disponibles (tabla auxiliar del coordinador) ===
    function refrescarVacantes(idUsuario) {
        $.getJSON(`/Practicas/ObtenerVacantesAsignar?idUsuario=${idUsuario}`, function (res) {
            if (!res.ok) return Swal.fire("Error", "No se pudieron recargar las vacantes.", "error");

            const tbody = $("#tablaVacantes tbody");
            if (!tbody.length) return;
            tbody.empty();

            res.data.forEach(v => {
                let btn = v.PuedeAsignar
                    ? `<button class="btn btn-azul asignar" data-id="${v.IdVacante}">Asignar</button>`
                    : `<button class="btn btn-rojo desasignar" data-id="${v.IdVacante}">Desasignar</button>`;

                tbody.append(`
                <tr>
                    <td>${v.NombreVacante}</td>
                    <td>${v.NombreEmpresa}</td>
                    <td>${v.Especialidad}</td>
                    <td>${v.NumCupos}</td>
                    <td>${v.CuposOcupados}</td>
                    <td class="text-center">${badgeEstado(v.EstadoPractica)}</td>
                    <td>${btn}</td>
                </tr>`);
            });
        });
    }

    // === Desasignar práctica (desde tabla del coordinador) ===
    $(document).on('click', '.desasignar', function () {
        const idVacante = $(this).data('id');
        const idUsuario = $('#IdUsuario').val(); // o el id del estudiante que estés mostrando

        Swal.fire({
            title: '¿Deseas desasignar esta práctica?',
            text: 'El estado del estudiante pasará a "Retirada".',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Sí, desasignar',
            cancelButtonText: 'Cancelar',
            confirmButtonColor: '#2d594d'
        }).then(r => {
            if (!r.isConfirmed) return;

            $.post(PracticasCfg.urls.retirarEstudiante, { idVacante, idUsuario })
                .done(res => {
                    if (res.ok) {
                        Swal.fire({
                            icon: 'success',
                            title: 'Desasignado',
                            text: res.message || 'La práctica fue desasignada correctamente.',
                            timer: 1500,
                            showConfirmButton: false
                        });
                        refrescarVacantes(idUsuario); // 🔁 recargar tabla auxiliar
                    } else {
                        Swal.fire('Error', res.message || 'No se pudo desasignar.', 'error');
                    }
                })
                .fail(() => Swal.fire('Error', 'Error al conectar con el servidor.', 'error'));
        });
    });

    console.log("🧩 Inicializando DataTable de prácticas");

    // === FIX: prevenir duplicados de estudiantes en DataTable ===
    if ($.fn.dataTable.isDataTable('#miTabla')) {
        console.warn("⚠️ La tabla #miTabla ya estaba inicializada, se destruye para evitar duplicados");
        $('#miTabla').DataTable().clear().destroy(); // elimina la anterior
        $('#miTabla').empty(); // limpia el contenido de la tabla
    }

    $.ajaxSetup({
        beforeSend: function (xhr, settings) {
            if (settings.url.includes('/Practicas/ListarEstudiantesJson')) {
                console.log("🚀 Llamada enviada a:", settings.url);
            }
        },
        complete: function (xhr, status) {
            if (xhr.responseJSON && xhr.responseJSON.data) {
                console.log("✅ Respuesta recibida:", xhr.responseJSON.data.length, "registros");
            } else {
                console.warn("⚠️ No hubo data en la respuesta o error:", status);
            }
        }
    });

    const table = $('#miTabla').DataTable({
        responsive: true,
        processing: true,
        ajax: {
            url: '/Practicas/ListarEstudiantesJson',
            type: 'GET',
            data: function (d) {
                d._ts = new Date().getTime(); // evita caché
            },
            dataSrc: 'data'
        },
        columns: [
            { data: 'Cedula' },
            {
                data: null,
                render: function (row) {
                    return row.Nombre || row.NombreCompleto || row.NombreEstudiante || '—';
                }
            },
            { data: 'Especialidad' },
            { data: 'Telefono', render: d => d || '—' },
            {
                data: 'EstadoPostulacion',
                render: function (d) {
                    if (!d) return '—';
                    const cls =
                        d === 'Asignada' ? 'badge-asignada' :
                            d === 'Con Procesos Activos' ? 'badge-procesos-activos' :
                                'badge-no-asignada';
                    return `<span class="badge ${cls}">${d}</span>`;
                }
            },
            { data: 'Empresa', render: d => d || '—' },
            {
                data: 'Tipo',
                render: function (d) {
                    if (!d || d === '—')
                        return '<span class="badge badge-secondary">—</span>';
                    const cls = d.toLowerCase().includes('asign')
                        ? 'badge-asignada'
                        : 'badge-en-proceso';
                    return `<span class="badge ${cls}">${d}</span>`;
                }
            },
            {
                data: null,
                orderable: false,
                render: function (row) {
                    let html = '';

                    if (row.IdVacanteUltima && row.IdUsuario) {
                        html += `
                <a href="javascript:void(0);" class="btn-ver"
                   data-idvacante="${row.IdVacanteUltima}"
                   data-idusuario="${row.IdUsuario}"
                   title="Ver detalle"
                   style="color:#2d594d; margin-right:8px;">
                   <i class="fas fa-eye"></i>
                </a>`;
                    }

                    html += `
            <a href="javascript:void(0);" class="btn-asignar"
               data-idvacante="${row.IdVacanteUltima || 0}"
               data-idusuario="${row.IdUsuario}"
               title="Asignar práctica"
               style="color:#2d594d; margin-right:8px;">
               <i class="fas fa-user-plus"></i>
            </a>`;

                    html += `
            <a href="javascript:void(0);" class="btn-cambiar-estado"
               data-idusuario="${row.IdUsuario}"
               data-nombre="${row.Nombre || row.NombreCompleto || '—'}"
               title="Cambiar estado académico"
               style="color:#768C46; margin-right:8px;">
               <i class="fas fa-user-graduate"></i>
            </a>`;

                    return html || '—';
                }
            }
        ],
        language: {
            url: "//cdn.datatables.net/plug-ins/1.13.6/i18n/es-ES.json"
        }
    });

    // === FILTROS ===
    $('#filtroPractica').on('change', function () {
        table.column(4).search(this.value).draw();
    });

    $('#filtroEspecialidad').on('change', function () {
        table.column(2).search(this.value).draw();
    });

    // === CAMBIAR ESTADO ACADÉMICO ===
    $(document).on('click', '.btn-cambiar-estado', function () {
        const idUsuario = $(this).data('idusuario');
        const nombre = $(this).data('nombre');

        Swal.fire({
            title: 'Estado académico de ' + nombre,
            input: 'select',
            inputOptions: {
                'Aprobado': 'Aprobado',
                'Rezagado': 'Rezagado'
            },
            inputPlaceholder: 'Selecciona un estado',
            showCancelButton: true,
            confirmButtonText: 'Actualizar',
            cancelButtonText: 'Cancelar'
        }).then(res => {
            if (res.isConfirmed && res.value) {
                $.post('/Practicas/CambiarEstadoAcademico', {
                    idUsuario,
                    nuevoEstado: res.value
                }).done(r => {
                    Swal.fire(r.ok ? 'Listo' : 'Error', r.msg, r.ok ? 'success' : 'error');
                    if (r.ok) table.ajax.reload(null, false);
                }).fail(() => Swal.fire('Error', 'No se pudo actualizar', 'error'));
            }
        });
    });

    // === VER DETALLE ===
    $(document).on('click', '.btn-ver', function () {
        const idVacante = $(this).data('idvacante');
        const idUsuario = $(this).data('idusuario');
        if (!idVacante || !idUsuario) return;
        window.location.href = `/Practicas/VisualizacionPostulacion?idVacante=${idVacante}&idUsuario=${idUsuario}`;
    });

    // === DESASIGNAR DESDE ICONO EN TABLA PRINCIPAL ===
    $(document).on('click', '.btn-desasignar', function () {
        const idPractica = $(this).data('idpractica');
        const nombre = $(this).data('nombre');

        Swal.fire({
            title: 'Desasignar práctica',
            html: `¿Deseas desasignar a <b>${nombre}</b>?<br/><small>Se cambiará el estado a <b>Retirada</b>.</small>`,
            input: 'text',
            inputLabel: 'Comentario (opcional)',
            showCancelButton: true,
            confirmButtonText: 'Sí, desasignar',
            cancelButtonText: 'Cancelar',
            confirmButtonColor: '#2d594d'
        }).then(r => {
            if (r.isConfirmed) {
                $.post('/Practicas/DesasignarPractica', {
                    idPractica,
                    comentario: r.value || ''
                }).done(res => {
                    Swal.fire(res.ok ? 'Hecho' : 'Ups', res.msg, res.ok ? 'success' : 'error');
                    if (res.ok) table.ajax.reload(null, false);
                }).fail(() => Swal.fire('Error', 'No se pudo desasignar', 'error'));
            }
        });
    });

    // === INICIAR PRÁCTICAS (masivo) ===
    $('#btnIniciarPracticas').click(function () {
        Swal.fire({
            title: '¿Iniciar todas las prácticas?',
            html: 'Las prácticas <b>Asignadas</b> pasarán a <b>En Curso</b>.<br>Las demás se marcarán como <b>Retirada</b>.',
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: 'Sí, iniciar',
            cancelButtonText: 'Cancelar',
            confirmButtonColor: '#2d594d'
        }).then(res => {
            if (res.isConfirmed) {
                $.post('/Practicas/IniciarPracticas')
                    .done(r => {
                        Swal.fire(r.ok ? 'Hecho' : 'Error', r.message, r.ok ? 'success' : 'error');
                        table.ajax.reload(null, false);
                    })
                    .fail(() => Swal.fire('Error', 'No se pudo iniciar el proceso', 'error'));
            }
        });
    });

    // === FINALIZAR PRÁCTICAS (masivo) ===
    $('#btnFinalizarPracticas').click(function () {
        Swal.fire({
            title: '¿Finalizar todas las prácticas?',
            html: 'Las prácticas <b>Aprobadas</b> o <b>Rezagadas</b> se marcarán como <b>Finalizadas</b>.<br>Los estudiantes aprobados pasarán a <b>Egresado</b>.',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Sí, finalizar',
            cancelButtonText: 'Cancelar',
            confirmButtonColor: '#768C46'
        }).then(res => {
            if (res.isConfirmed) {
                $.post('/Practicas/FinalizarPracticas')
                    .done(r => {
                        Swal.fire(r.ok ? 'Hecho' : 'Error', r.message, r.ok ? 'success' : 'error');
                        table.ajax.reload(null, false);
                    })
                    .fail(() => Swal.fire('Error', 'No se pudo finalizar el proceso', 'error'));
            }
        });
    });

    // === HANDLER: abrir modal de asignación y cargar vacantes ===
    $(document).on('click', '.btn-asignar', function () {
        const idUsuario = $(this).data('idusuario');
        console.log("🟦 Clic en asignar → idUsuario:", idUsuario);

        const tbody = $('#miTablaAsignar tbody');
        if (tbody.length) {
            tbody.html('<tr><td colspan="7" class="text-center text-muted">Cargando vacantes...</td></tr>');
        }
        $('#modalAsignar').modal('show');

        // Carga inicial de vacantes usando la función unificada
        recargarModalVacantes(idUsuario);
    });

    // === ASIGNAR VACANTE A ESTUDIANTE (desde modal) ===
    $(document).on('click', '.btn-asignar-estudiante', function () {
        const idVacante = $(this).data('idvacante');
        const idUsuario = $(this).data('idusuario');

        Swal.fire({
            title: '¿Confirmar asignación?',
            html: '¿Deseas asignar esta vacante al estudiante?<br><b>El estado progresará automáticamente: "En proceso de aplicación" → "Asignada"</b>',
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: 'Sí, asignar',
            cancelButtonText: 'Cancelar',
            confirmButtonColor: '#2d594d'
        }).then(res => {
            if (!res.isConfirmed) return;

            $.post('/Practicas/AsignarEstudiante', { idVacante, idUsuario })
                .done(resp => {
                    if (!resp.ok) {
                        Swal.fire('Error', resp.message || 'No se pudo asignar el estudiante.', 'error');
                        return;
                    }

                    Swal.fire({
                        icon: 'success',
                        title: 'Éxito',
                        text: resp.message || 'Asignación completada correctamente.',
                        timer: 1800,
                        showConfirmButton: false
                    });

                    // Actualizar modal sin cerrarlo y refrescar tabla principal
                    recargarModalVacantes(idUsuario);
                    $('#miTabla').DataTable().ajax.reload(null, false);
                })
                .fail(() => Swal.fire('Error', 'Error al conectar con el servidor.', 'error'));
        });
    });

    // === DESASIGNAR VACANTE DESDE EL MODAL ===
    $(document).on('click', '.btn-retirar-estudiante', function () {
        const idPractica = $(this).data('idpractica') || 0;
        const idUsuario = $(this).data('idusuario') || 0;
        const nombre = $(this).data('nombre') || '—';
        const estadoAcademico = $(this).data('estadoacademico') ?? 'Activo';

        if (!idPractica) {
            Swal.fire('Error', 'No se encontró una práctica activa para este estudiante.', 'error');
            return;
        }

        // Evitar bloqueo de foco del modal Bootstrap mientras aparece SweetAlert
        const modal = document.getElementById('modalAsignar');
        const modalInstance = modal ? bootstrap.Modal.getInstance(modal) : null;
        if (modalInstance?._focustrap) modalInstance._focustrap.deactivate();

        Swal.fire({
            title: '¿Deseas desasignar esta práctica?',
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

                        // 🔁 Actualizar modal sin cerrarlo y refrescar tabla principal
                        recargarModalVacantes(idUsuario);
                        $('#miTabla').DataTable().ajax.reload(null, false);
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
