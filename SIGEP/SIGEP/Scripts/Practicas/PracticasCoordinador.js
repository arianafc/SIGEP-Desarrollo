$(function () {
    console.log("✅ PracticasCoordinador.js cargado correctamente");

    const table = $('#miTabla').DataTable({
        responsive: true,
        processing: true,
        ajax: {
            url: '/Practicas/ListarEstudiantesJson',
            type: 'GET',
            dataSrc: 'data'
        },
        columns: [
            { data: 'Cedula' },
            { data: 'Nombre' },
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

                    // 👁️ Ver detalle
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

                    // ➕ Asignar estudiante (solo si no tiene práctica activa)
                    if (row.EstadoPostulacion === 'Sin Procesos Activos') {
                        html += `
                            <a href="javascript:void(0);" class="btn-asignar"
                               data-idvacante="${row.IdVacanteUltima || 0}"
                               data-idusuario="${row.IdUsuario}"
                               title="Asignar práctica"
                               style="color:#2d594d; margin-right:8px;">
                               <i class="fas fa-user-plus"></i>
                            </a>`;
                    }

                    // 🎓 Cambiar estado académico
                    html += `
                        <a href="javascript:void(0);" class="btn-cambiar-estado"
                           data-idusuario="${row.IdUsuario}"
                           data-nombre="${row.Nombre}"
                           title="Cambiar estado académico"
                           style="color:#768C46; margin-right:8px;">
                           <i class="fas fa-user-graduate"></i>
                        </a>`;

                    // 🗑️ Desasignar
                    if (row.IdPracticaVacante && (row.EstadoPostulacion === 'Asignada' || row.EstadoPostulacion === 'Con Procesos Activos')) {
                        html += `
                            <a href="javascript:void(0);" class="btn-desasignar"
                               data-idpractica="${row.IdPracticaVacante}"
                               data-nombre="${row.Nombre}"
                               title="Desasignar práctica"
                               style="color:#c00;">
                               <i class="fas fa-trash"></i>
                            </a>`;
                    }

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

    // Filtro Estado de práctica
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
        window.location.href= `/Practicas/VisualizacionPostulacion?idVacante=${idVacante}&idUsuario=${idUsuario}`;
    });

    // === DESASIGNAR ===
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

    // === INICIAR PRÁCTICAS ===
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

    // === FINALIZAR PRÁCTICAS ===
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

    //handler de asignar
    $(document).on('click', '.btn-asignar', function () {
        const idVacante = $(this).data('idvacante');
        const idUsuario = $(this).data('idusuario');
        Swal.fire({
            title: 'Asignar práctica',
            text: '¿Deseas asignar esta vacante al estudiante?',
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: 'Sí, asignar',
            cancelButtonText: 'Cancelar',
            confirmButtonColor: '#2d594d'
        }).then(res => {
            if (res.isConfirmed) {
                $.post('/Practicas/AsignarEstudiante', { idVacante, idUsuario })
                    .done(r => {
                        Swal.fire(r.ok ? 'Éxito' : 'Error', r.message, r.ok ? 'success' : 'error');
                        if (r.ok) table.ajax.reload(null, false);
                    })
                    .fail(() => Swal.fire('Error', 'No se pudo asignar.', 'error'));
            }
        });
    });

});
