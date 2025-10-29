$(function () {

    console.log("✅ PracticasCoordinador.js cargado correctamente");

    var table = $('#miTabla').DataTable({
        responsive: true,
        processing: true,
        ajax: {
            url: '/Practicas/ListarEstudiantesJson',
            type: 'GET',
            dataSrc: json => json.data || []
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
                    const estado = d.toLowerCase();
                    const cls =
                        estado.includes('asignada') ? 'badge-asignada' :
                            estado.includes('proceso') ? 'badge-en-proceso' :
                                estado.includes('curso') ? 'badge-en-curso' :
                                    estado.includes('finalizada') ? 'badge-finalizada' :
                                        estado.includes('rechazada') ? 'badge-rechazada' :
                                            estado.includes('retirada') ? 'badge-retirada' :
                                                estado.includes('aprobada') ? 'badge-aprobada' :
                                                    estado.includes('rezagado') ? 'badge-rezagado' :
                                                        'badge-no-asignada';
                    return `<span class="badge ${cls}">${d}</span>`;
                }
            },
            { data: 'Empresa', render: d => d || '—' },
            {
                data: 'Tipo',
                render: function (d, type, row) {
                    // ✅ mostrar el estado académico si existe
                    const val = row.EstadoVacante || d || row.EstadoAcademico || '—';
                    const cls =
                        val.toLowerCase().includes('aprob') ? 'badge-aprobada' :
                            val.toLowerCase().includes('rezag') ? 'badge-rezagado' :
                                'badge-secondary';
                    return `<span class="badge ${cls}">${val}</span>`;
                }
            },
            {
                data: null,
                orderable: false,
                render: function (row) {
                    let btns = '';

                    // 👁️ Ver detalle
                    if (row.IdVacanteUltima && row.IdUsuario) {
                        btns += `
                            <a href="javascript:void(0);" class="btn-ver"
                               data-idvacante="${row.IdVacanteUltima}"
                               data-idusuario="${row.IdUsuario}"
                               title="Ver detalle"
                               style="color:#2d594d; margin-right:8px;">
                               <i class="fas fa-eye"></i>
                            </a>`;
                    }

                    // 🎓 Cambiar estado académico
                    btns += `
                        <a href="javascript:void(0);" class="btn-cambiar-estado"
                           data-idusuario="${row.IdUsuario}"
                           data-nombre="${row.Nombre}"
                           title="Cambiar estado académico"
                           style="color:#768C46; margin-right:8px;">
                           <i class="fas fa-user-graduate"></i>
                        </a>`;

                    // 🗑️ Desasignar
                    if (row.IdPracticaVacante &&
                        (row.EstadoPostulacion === 'Asignada' || row.EstadoPostulacion === 'En proceso de aplicacion')) {
                        btns += `
                            <a href="javascript:void(0);" class="btn-desasignar"
                               data-idpractica="${row.IdPracticaVacante}"
                               data-nombre="${row.Nombre}"
                               title="Desasignar práctica"
                               style="color:#c00;">
                               <i class="fas fa-trash"></i>
                            </a>`;
                    }

                    return btns || '—';
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
    $('#filtroEspecialidad').on('keyup', function () {
        table.column(2).search(this.value).draw();
    });
    $('#filtroEstadoAcademico').on('change', function () {
        table.column(6).search(this.value).draw(); // estado académico
    });

    // === CAMBIAR ESTADO ACADÉMICO ===
    $(document).on('click', '.btn-cambiar-estado', function () {
        const idUsuario = $(this).data('idusuario');
        const nombre = $(this).data('nombre');

        Swal.fire({
            title: 'Estado académico de ' + nombre,
            input: 'select',
            inputOptions: { 'Aprobado': 'Aprobado', 'Rezagado': 'Rezagado' },
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
        window.open(`/Practicas/VisualizacionPostulacion?idVacante=${idVacante}&idUsuario=${idUsuario}`, '_blank');
    });

    // === DESASIGNAR ===
    $(document).on('click', '.btn-desasignar', function () {
        const idPractica = $(this).data('idpractica');
        const nombre = $(this).data('nombre');
        Swal.fire({
            title: 'Desasignar práctica',
            html: `¿Deseas desasignar a <b>${nombre}</b>?<br/><small>Se cambiará a estado <b>Retirada</b>.</small>`,
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
                $.post('/Practicas/IniciarPracticas').done(r => {
                    Swal.fire(r.ok ? 'Hecho' : 'Error', r.message, r.ok ? 'success' : 'error');
                    table.ajax.reload(null, false);
                }).fail(() => Swal.fire('Error', 'No se pudo iniciar el proceso', 'error'));
            }
        });
    });

    // === FINALIZAR PRÁCTICAS ===
    $('#btnFinalizarPracticas').click(function () {
        Swal.fire({
            title: '¿Finalizar todas las prácticas?',
            html: 'Las prácticas <b>Aprobadas</b> o <b>En Curso</b> se marcarán como <b>Finalizadas</b>.<br>Las vacantes serán archivadas y los estudiantes aprobados pasarán a <b>Egresado</b>.',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Sí, finalizar',
            cancelButtonText: 'Cancelar',
            confirmButtonColor: '#768C46'
        }).then(res => {
            if (res.isConfirmed) {
                $.post('/Practicas/FinalizarPracticas').done(r => {
                    Swal.fire(r.ok ? 'Hecho' : 'Error', r.message, r.ok ? 'success' : 'error');
                    table.ajax.reload(null, false);
                }).fail(() => Swal.fire('Error', 'No se pudo finalizar el proceso', 'error'));
            }
        });
    });

});
