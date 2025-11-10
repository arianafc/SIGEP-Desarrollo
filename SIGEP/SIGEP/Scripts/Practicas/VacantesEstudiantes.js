(function ($) {
    $(function () {
        const CFG = window.VacantesCfg || { urls: {}, rol: 0 };

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

        function validarFechas(fechaAplic, fechaCierre) {
            if (!fechaAplic || !fechaCierre) return false;
            const f1 = new Date(fechaAplic);
            const f2 = new Date(fechaCierre);
            return f1 && f2 && f1 > f2;
        }

        // =====================================================
        // 🔹 Ubicación empresa en Crear/Editar
        // =====================================================
        $('#IdEmpresa, #edit-IdEmpresa').on('change', function () {
            const idEmpresa = $(this).val();
            const $inputUbicacion = $(this).attr('id') === 'IdEmpresa'
                ? $('#ubicacionEmpresa')
                : $('#edit-Ubicacion');

            if (!idEmpresa) {
                $inputUbicacion.val('');
                return;
            }

            $.getJSON(CFG.urls.getUbicacionEmpresa, { idEmpresa })
                .done(res => {
                    if (res && res.ok) $inputUbicacion.val(res.ubicacion);
                    else $inputUbicacion.val('No registrada');
                })
                .fail(() => $inputUbicacion.val('Error al obtener ubicación'));
        });

        // =====================================================
        // 🔹 Crear Vacante
        // =====================================================
        $('#formCrearVacante').on('submit', function (e) {
            e.preventDefault();

            const nombre = $('[name="Nombre"]').val().trim();
            const idEmpresa = $('[name="IdEmpresa"]').val();
            const requerimientos = $('[name="Requerimientos"]').val().trim();
            const numCupos = parseInt($('[name="NumCupos"]').val()) || 0;
            const idEspecialidad = $('[name="IdEspecialidad"]').val();
            const idModalidad = $('[name="IdModalidad"]').val();
            const fechaAplic = $('[name="FechaMaxAplicacion"]').val();
            const fechaCierre = $('[name="FechaCierre"]').val();

            if (!nombre || !idEmpresa || !requerimientos || numCupos < 1 || !idEspecialidad || !idModalidad) {
                Swal.fire({
                    icon: 'warning',
                    title: 'Campos obligatorios',
                    text: 'Debe completar todos los campos requeridos.'
                });
                return false;
            }

            if (validarFechas(fechaAplic, fechaCierre)) {
                Swal.fire({
                    icon: 'warning',
                    title: 'Fechas inválidas',
                    text: 'La fecha de aplicación no puede ser mayor que la de cierre.'
                });
                return false;
            }

            const formData = $(this).serialize();

            $.ajax({
                url: CFG.urls.crear,
                type: 'POST',
                data: formData,
                success: function (res, status, xhr) {
                    if (redirSiLogin(res, xhr)) return;
                    if (res.ok) {
                        Swal.fire('Éxito', res.message || 'Vacante creada correctamente.', 'success')
                            .then(() => {
                                $('#modalCrearVacante').modal('hide');
                                $('#formCrearVacante')[0].reset();
                                tabla.ajax.reload(null, false);
                            });
                    } else {
                        Swal.fire('Error', res.message || 'No se pudo crear la vacante.', 'error');
                    }
                },
                error: () => Swal.fire('Error', 'Error al crear la vacante.', 'error')
            });
        });

        // =====================================================
        // 🔹 DataTable principal
        // =====================================================
        const tabla = $('#miTabla').DataTable({
            responsive: true,
            processing: true,
            ajax: {
                url: CFG.urls.getVacantes,
                type: 'GET',
                cache: false,
                dataType: 'json',
                data: () => ({
                    idEstado: $('#filtroPractica').val() || 0,
                    idEspecialidad: $('#filtroEspecialidad').val() || 0,
                    idModalidad: $('#filtroModalidad').val() || 0
                }),
                dataSrc: function (json) {
                    if (typeof json === 'string') {
                        if (json.indexOf('<!DOCTYPE html') >= 0 || json.indexOf('<html') >= 0) {
                            Swal.fire('Error', 'La sesión puede haber expirado o el servidor devolvió HTML.', 'error');
                            return [];
                        }
                        try { json = JSON.parse(json); } catch {
                            Swal.fire('Error', 'Respuesta no válida del servidor.', 'error');
                            return [];
                        }
                    }

                    if (json && json.ok === false) {
                        Swal.fire('Error', json.error || 'Error en servidor.', 'error');
                        return [];
                    }

                    return (json && Array.isArray(json.data)) ? json.data : [];
                },
                error: function (xhr) {
                    const ct = xhr.getResponseHeader('content-type') || '';
                    if (ct.indexOf('text/html') >= 0) {
                        Swal.fire('Error', 'Se recibió HTML en lugar de JSON (¿login/500?).', 'error');
                    } else {
                        Swal.fire('Error', `Error consultando vacantes (${xhr.status}).`, 'error');
                    }
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
                    render: d => `<strong>${d || 0}</strong>`
                },
                {
                    data: 'EstadoNombre',
                    title: 'Estado',
                    render: d => badgeEstado(d)
                },
                {
                    data: 'IdVacante',
                    orderable: false,
                    title: 'Acciones',
                    render: (data, type, row) => {
                        const estado = normalizarEstado(row.EstadoNombre);
                        const inactivo = (estado === 'inactivo' || estado === 'archivado');
                        const dis = inactivo ? 'disabled aria-disabled="true"' : '';
                        const muted = inactivo ? 'opacity:0.35; cursor:not-allowed;' : '';

                        let acc = `
                            <button class="btn bg-transparent btn-visualizar"
                                    data-id="${data}"
                                    title="Visualizar"
                                    style="color:#2d594d">
                                <i class="fas fa-eye"></i>
                            </button>`;

                        if ((CFG.rol === 2 || CFG.rol === 3) &&
                            !(row.Nombre || '').includes('Práctica Autogestionada')) {
                            acc += `
                                <button class="btn bg-transparent btn-asignar"
                                        data-id="${data}"
                                        style="color:#2d594d; ${muted}" ${dis}>
                                    <i class="fas fa-user-plus"></i>
                                </button>`;
                        }

                        if (CFG.rol === 2) {
                            acc += `
                                <button class="btn bg-transparent btn-editar"
                                        data-id="${data}"
                                        style="color:#2d594d; ${muted}" ${dis}>
                                    <i class="fas fa-sync-alt"></i>
                                </button>
                                <button class="btn bg-transparent btn-eliminar"
                                        data-id="${data}"
                                        style="color:#2d594d; ${muted}" ${dis}>
                                    <i class="fas fa-archive"></i>
                                </button>`;
                        }
                        return acc;
                    }
                }
            ],
            language: { url: "//cdn.datatables.net/plug-ins/1.13.6/i18n/es-ES.json" }
        });

        $('#filtroPractica, #filtroEspecialidad, #filtroModalidad')
            .on('change', () => tabla.ajax.reload());

        // =====================================================
        // 🔹 Visualizar Vacante + Postulaciones
        // =====================================================
        $('#miTabla').on('click', '.btn-visualizar', function () {
            const id = $(this).data('id');

            $.get(CFG.urls.detalle, { id }, (res, _, xhr) => {
                if (redirSiLogin(res, xhr)) return;
                if (!res.ok || !res.data)
                    return Swal.fire('Error', 'No se pudo cargar la vacante.', 'error');

                const d = res.data;

                $('#vis-Nombre').val(d.Nombre);
                $('#vis-Empresa').val(d.IdEmpresa);
                $('#vis-Ubicacion').val(d.Ubicacion);
                $('#vis-Especialidad').val(d.IdEspecialidad);
                $('#vis-NumCupos').val(d.NumCupos);
                $('#vis-Modalidad').val(d.IdModalidad);
                $('#vis-Requerimientos').val(d.Requerimientos);
                $('#vis-Descripcion').val(d.Descripcion);
                $('#vis-FechaAplicacion').val(d.FechaMaxAplicacion?.split('T')[0] || '');
                $('#vis-FechaCierre').val(d.FechaCierre?.split('T')[0] || '');

                $('#modalVisualizarVacante').data('idVacante', id);
                $('#modalVisualizarVacante').modal('show');

                // Cargar postulaciones
                $.getJSON(CFG.urls.obtenerPostulaciones, { idVacante: id }, r2 => {
                    const $lista = $('#listaPostulaciones').empty();
                    $('#mensajeSinPostulaciones').toggle(!r2.ok || !r2.data?.length);

                    if (r2.ok && r2.data?.length) {
                        r2.data.forEach(p => {
                            const estado = p.EstadoDescripcion || 'Sin estado';
                            const estNorm = normalizarEstado(estado);
                            const badge = badgeEstado(estado);
                            const mostrarBoton = ['asignada', 'en proceso de aplicacion'].includes(estNorm);

                            const btnDes = mostrarBoton
                                ? `<button class="btn bg-transparent BtnDesasignarPracticaEstudiante"
                                           data-idpractica="${p.IdPractica}"
                                           data-nombre="${escapeHtml(p.NombreCompleto)}"
                                           title="Desasignar práctica"
                                           style="color:#2D594D;">
                                       <i class="fas fa-trash-alt"></i>
                                   </button>`
                                : '';

                            $lista.append(`
                                <li class="d-flex justify-content-between align-items-center p-2 border rounded mb-2">
                                    <div>
                                        <a href="${CFG.urls.visualizacionPostulacion}?idVacante=${p.IdVacante}&idUsuario=${p.IdUsuario}"
                                           class="text-decoration-none fw-bold"
                                           style="color:#2d594d;">
                                            ${escapeHtml(p.NombreCompleto)}
                                        </a>
                                    </div>
                                    <div class="d-flex align-items-center gap-2">${badge}${btnDes}</div>
                                </li>`);
                        });
                    }
                });
            });
        });

        // =====================================================
        // 🔹 Cargar estudiantes para asignar (helper único)
        // =====================================================
        // =====================================================
        // 🔹 Cargar estudiantes para asignar (helper único)
        // =====================================================
        function cargarEstudiantesAsignar(idVacante) {
            $.getJSON(CFG.urls.obtenerEstudiantesAsignar, {
                idVacante,
                idUsuarioSesion: CFG.idUsuarioSesion || 0
            }, res => {
                const tbody = $('#miTablaAsignar tbody').empty();

                if (!res?.ok || !res.data?.length) {
                    tbody.append('<tr><td colspan="5" class="text-center text-muted">No hay estudiantes disponibles</td></tr>');
                    return;
                }

                res.data.forEach(e => {
                    const estadoVacante = normalizarEstado(e.EstadoVacante || e.EstadoPractica || 'Sin proceso activo');
                    const badge = badgeEstado(e.EstadoVacante || e.EstadoPractica);
                    const estadoAcademico = parseInt(e.EstadoAcademico || 0); // ✅ viene del SP
                    let btn = '';

                    // 1️⃣ Filtrar: si está rezagado, no mostrar
                    if (estadoAcademico === 9) {
                        // Omitir por completo
                        return;
                    }

                    // 2️⃣ Si tiene práctica activa en otra vacante → botón bloqueado
                    if (e.TienePracticaActiva && !['asignada', 'en proceso de aplicacion', 'retirada'].includes(estadoVacante)) {
                        btn = `<button class="btn btn-sm btn-outline-secondary btn-bloqueado">
                           <i class="fas fa-ban"></i> No disponible
                       </button>`;
                    }

                    // 3️⃣ Si está rezagado (doble seguridad, por si el SP no lo filtró)
                    else if (estadoAcademico === 9) {
                        btn = `<button class="btn btn-sm btn-outline-secondary" disabled>
                           <i class="fas fa-ban"></i> No disponible (Rezagado)
                       </button>`;
                    }

                    // 4️⃣ Si puede aplicar
                    else if (['sin proceso activo', 'retirada', 'en proceso de aplicacion'].includes(estadoVacante)) {
                        btn = `<button class="btn btn-sm btn-outline-success btn-asignar-estudiante"
                               data-idusuario="${e.IdUsuario}"
                               data-nombre="${escapeHtml(e.NombreCompleto)}">
                           <i class="fas fa-user-plus"></i> Asignar
                       </button>`;
                    }

                    // 5️⃣ Si ya está asignada en esta vacante
                    else if (estadoVacante === 'asignada') {
                        btn = `<button class="btn btn-sm btn-outline-warning btn-retirar-estudiante"
                               data-idusuario="${e.IdUsuario}"
                               data-idpractica="${e.IdPracticaVacante || 0}"
                               data-nombre="${escapeHtml(e.NombreCompleto)}">
                           <i class="fas fa-user-minus"></i> Retirar
                       </button>`;
                    }

                    // 6️⃣ Otros estados bloqueados
                    else if (['rechazada', 'aprobada', 'en curso', 'finalizada', 'rezagado', 'archivado'].includes(estadoVacante)) {
                        btn = `<button class="btn btn-sm btn-outline-secondary" disabled>
                           <i class="fas fa-ban"></i> No disponible
                       </button>`;
                    }

                    // 7️⃣ Cualquier otro caso
                    else {
                        btn = `<button class="btn btn-sm btn-outline-secondary" disabled>
                           <i class="fas fa-question"></i> Estado desconocido
                       </button>`;
                    }

                    tbody.append(`
                <tr>
                    <td>${escapeHtml(e.NombreCompleto)}</td>
                    <td>${escapeHtml(e.Cedula || '')}</td>
                    <td>${escapeHtml(e.Especialidad || '')}</td>
                    <td class="text-center">${badge}</td>
                    <td class="text-center">${btn}</td>
                </tr>`);
                });
            });
        }


        // =====================================================
        // 🔹 Abrir modal Asignar
        // =====================================================
        $('#miTabla').on('click', '.btn-asignar', function () {
            const idVacante = $(this).data('id');
            $('#modalAsignar').data('idVacante', idVacante).modal('show');
            cargarEstudiantesAsignar(idVacante);
        });

        // =====================================================
        // 🔹 Asignar estudiante (primer/segundo clic → SP maneja lógica)
        // =====================================================
        $(document).on('click', '.btn-asignar-estudiante', function () {
            const idUsuario = $(this).data('idusuario');
            const idVacante = $('#modalAsignar').data('idVacante');
            const nombre = $(this).data('nombre');

            Swal.fire({
                title: 'Confirmar acción',
                html: `¿Deseas asignar la vacante al estudiante <b>${nombre}</b>?<br>
                       <small>Primer clic: "En proceso de aplicación"<br>
                       Segundo clic: "Asignada"</small>`,
                icon: 'question',
                showCancelButton: true,
                confirmButtonText: 'Sí, continuar',
                cancelButtonText: 'Cancelar',
                confirmButtonColor: '#2d594d'
            }).then(r => {
                if (!r.isConfirmed) return;

                $.post(CFG.urls.asignarEstudiante, { idUsuario, idVacante })
                    .done(res => {
                        if (!res) {
                            Swal.fire('Error', 'Sin respuesta del servidor.', 'error');
                            return;
                        }

                        if (res.ok) {
                            Swal.fire({
                                icon: 'success',
                                title: 'Éxito',
                                text: res.message || 'Acción completada correctamente.',
                                timer: 2000,
                                showConfirmButton: false
                            });

                            cargarEstudiantesAsignar(idVacante);
                            tabla.ajax.reload(null, false);
                        } else {
                            Swal.fire('Aviso', res.message || 'No se pudo completar la acción.', 'warning');
                        }
                    })
                    .fail(() => Swal.fire('Error', 'Error de conexión al servidor.', 'error'));
            });
        });

        // =====================================================
        // 🔹 Retirar estudiante (estado "Retirada" usando DesasignarPractica)
        // =====================================================
        // =====================================================
        // 🔹 Retirar estudiante (usa idPractica si viene, si no RetirarEstudiante)
        // =====================================================
        $(document).on('click', '.btn-retirar-estudiante', function () {
            const idVacante = $('#modalAsignar').data('idVacante');
            const idUsuario = $(this).data('idusuario');
            const idPractica = $(this).data('idpractica') || 0;
            const nombre = $(this).data('nombre');

            Swal.fire({
                title: '¿Deseas retirar al estudiante?',
                html: `El estudiante <b>${nombre}</b> quedará con estado <b>"Retirada"</b> para esta vacante.`,
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: 'Sí, retirar',
                cancelButtonText: 'Cancelar',
                confirmButtonColor: '#2d594d'
            }).then(r => {
                if (!r.isConfirmed) return;

                // Si tengo idPractica → usar el endpoint clásico
                if (idPractica > 0) {
                    $.post(CFG.urls.desasignarPractica, { idPractica, comentario: '' })
                        .done(res => procesarRetiro(res, idVacante))
                        .fail(() => Swal.fire('Error', 'Error de conexión con el servidor.', 'error'));
                }
                // Si no tengo idPractica → usar el nuevo endpoint por usuario/vacante
                else {
                    $.post(CFG.urls.retirarEstudiante, { idVacante, idUsuario })
                        .done(res => procesarRetiro(res, idVacante))
                        .fail(() => Swal.fire('Error', 'Error de conexión con el servidor.', 'error'));
                }
            });
        });

        function procesarRetiro(res, idVacante) {
            if (res.ok) {
                Swal.fire({
                    icon: 'success',
                    title: 'Estudiante retirado',
                    text: res.message || 'El estudiante fue retirado correctamente.',
                    timer: 1500,
                    showConfirmButton: false
                });
                cargarEstudiantesAsignar(idVacante);
                tabla.ajax.reload(null, false);
            } else {
                Swal.fire('Error', res.message || res.msg || 'No se pudo retirar al estudiante.', 'error');
            }
        }

        // =====================================================
        // 🔹 Botón bloqueado (info)
        // =====================================================
        $(document).on('click', '.btn-bloqueado', function () {
            Swal.fire({
                icon: 'warning',
                title: 'Estudiante no disponible',
                text: 'Este estudiante ya tiene una práctica activa o completada y no puede ser asignado.'
            });
        });

        // =====================================================
        // 🔹 Editar Vacante
        // =====================================================
        $('#miTabla').on('click', '.btn-editar', function () {
            const id = $(this).data('id');

            $.get(CFG.urls.detalle, { id }, (res, _, xhr) => {
                if (redirSiLogin(res, xhr)) return;
                if (!res.ok || !res.data)
                    return Swal.fire('Error', 'No se pudo cargar la información.', 'error');

                const d = res.data;
                $('#edit-IdVacante').val(d.IdVacante);
                $('#edit-Nombre').val(d.Nombre);
                $('#edit-IdEmpresa').val(d.IdEmpresa);
                $('#edit-Ubicacion').val(d.Ubicacion);
                $('#edit-IdEspecialidad').val(d.IdEspecialidad);
                $('#edit-NumCupos').val(d.NumCupos);
                $('#edit-IdModalidad').val(d.IdModalidad);
                $('#edit-Requerimientos').val(d.Requerimientos);
                $('#edit-Descripcion').val(d.Descripcion);
                $('#edit-FechaMaxAplicacion').val(d.FechaMaxAplicacion?.split('T')[0] || '');
                $('#edit-FechaCierre').val(d.FechaCierre?.split('T')[0] || '');

                $('#modalEditarVacante').modal('show');
            });
        });

        $('#formEditarVacante').on('submit', function (e) {
            e.preventDefault();

            const fAplic = $('#edit-FechaMaxAplicacion').val();
            const fCierre = $('#edit-FechaCierre').val();

            if (validarFechas(fAplic, fCierre)) {
                Swal.fire('Fechas inválidas', 'La fecha de aplicación no puede ser mayor que la de cierre.', 'warning');
                return false;
            }

            $.post(CFG.urls.editar, $(this).serialize())
                .done((res, _, xhr) => {
                    if (redirSiLogin(res, xhr)) return;
                    if (res.ok) {
                        Swal.fire('Éxito', res.message, 'success');
                        $('#modalEditarVacante').modal('hide');
                        tabla.ajax.reload(null, false);
                    } else {
                        Swal.fire('Error', res.message, 'error');
                    }
                })
                .fail(() => Swal.fire('Error', 'Ocurrió un problema al actualizar.', 'error'));
        });

        // =====================================================
        // 🔹 Eliminar / Archivar Vacante
        // =====================================================
        $('#miTabla').on('click', '.btn-eliminar', function () {
            const id = $(this).data('id');

            Swal.fire({
                title: '¿Deseas archivar esta vacante?',
                text: 'Solo se archivará si no tiene estudiantes activos.',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#2d594d',
                confirmButtonText: 'Sí, archivar',
                cancelButtonText: 'Cancelar'
            }).then(r => {
                if (!r.isConfirmed) return;

                $.post(CFG.urls.eliminar, { id })
                    .done((res, _, xhr) => {
                        if (redirSiLogin(res, xhr)) return;
                        if (res.ok) {
                            Swal.fire('Éxito', res.message, 'success');
                            tabla.ajax.reload(null, false);
                        } else {
                            Swal.fire('Aviso', res.message, 'warning');
                        }
                    })
                    .fail(() => Swal.fire('Error', 'Error al archivar la vacante.', 'error'));
            });
        });

        // =====================================================
        // 🔹 Desasignar desde modal Visualizar (BtnDesasignarPracticaEstudiante)
        // =====================================================
        // =====================================================
        // 🔹 Desasignar desde modal Visualizar (BtnDesasignarPracticaEstudiante)
        // =====================================================
        $(document).on('click', '.BtnDesasignarPracticaEstudiante', function (e) {
            e.preventDefault();

            const idPractica = $(this).data('idpractica');
            const nombreEst = $(this).data('nombre');
            const idVacante = $('#modalVisualizarVacante').data('idVacante');

            if (!idPractica) {
                Swal.fire('Error', 'No se encontró el identificador de la práctica.', 'error');
                return;
            }

            const modalVisual = document.getElementById('modalVisualizarVacante');
            const modalInstance = modalVisual ? bootstrap.Modal.getInstance(modalVisual) : null;

            if (modalInstance && modalInstance._focustrap) modalInstance._focustrap.deactivate();

            Swal.fire({
                title: '¿Deseas desasignar esta práctica?',
                html: `
            <p>El estado de <b>${escapeHtml(nombreEst || 'el estudiante')}</b> se cambiará a <b>"Retirada"</b>.</p>
        `,
                icon: 'warning',
                input: 'textarea',
                inputLabel: 'Comentario (opcional)',
                inputPlaceholder: 'Escribe el motivo de la desasignación...',
                inputAttributes: { maxlength: 500 },
                showCancelButton: true,
                confirmButtonText: 'Sí, desasignar',
                cancelButtonText: 'Cancelar',
                confirmButtonColor: '#2d594d',
                allowOutsideClick: false,
                didOpen: () => {
                    // 🔹 Asegura que el textarea reciba foco y no quede bloqueado por el modal
                    const textarea = Swal.getInput();
                    if (textarea) setTimeout(() => textarea.focus(), 150);
                }
            }).then(result => {
                if (!result.isConfirmed) {
                    if (modalInstance && modalInstance._focustrap) modalInstance._focustrap.activate();
                    return;
                }

                const comentario = (result.value || '').trim();

                $.ajax({
                    url: CFG.urls.desasignarPractica,
                    type: 'POST',
                    data: {
                        idPractica,
                        comentario // 👈 el comentario se envía tal cual al SP
                    },
                    success: function (res, status, xhr) {
                        if (redirSiLogin(res, xhr)) return;

                        if (res.ok) {
                            Swal.fire({
                                title: 'Desasignado',
                                text: res.msg || 'La práctica fue desasignada correctamente.',
                                icon: 'success',
                                timer: 1500,
                                showConfirmButton: false
                            }).then(() => {
                                // 🔁 Recargar listado de postulaciones
                                $.getJSON(CFG.urls.obtenerPostulaciones, { idVacante }, r2 => {
                                    const $lista = $('#listaPostulaciones').empty();
                                    $('#mensajeSinPostulaciones').toggle(!r2.ok || !r2.data?.length);

                                    if (r2.ok && r2.data?.length) {
                                        r2.data.forEach(p => {
                                            const estado = p.EstadoDescripcion || 'Sin estado';
                                            const estNorm = normalizarEstado(estado);
                                            const badge = badgeEstado(estado);
                                            const mostrarBoton =
                                                ['asignada', 'en proceso de aplicacion'].includes(estNorm);

                                            const btnDes = mostrarBoton
                                                ? `<button class="btn bg-transparent BtnDesasignarPracticaEstudiante"
                                               data-idpractica="${p.IdPractica}"
                                               data-nombre="${escapeHtml(p.NombreCompleto)}"
                                               title="Desasignar práctica"
                                               style="color:#2D594D;">
                                               <i class="fas fa-trash-alt"></i>
                                           </button>`
                                                : '';

                                            $lista.append(`
                                        <li class="d-flex justify-content-between align-items-center p-2 border rounded mb-2">
                                            <div>
                                                <a href="${CFG.urls.visualizacionPostulacion}?idVacante=${p.IdVacante}&idUsuario=${p.IdUsuario}"
                                                   class="text-decoration-none fw-bold"
                                                   style="color:#2d594d;">
                                                    ${escapeHtml(p.NombreCompleto)}
                                                </a>
                                            </div>
                                            <div class="d-flex align-items-center gap-2">${badge}${btnDes}</div>
                                        </li>`);
                                        });
                                    }

                                    if (typeof tabla !== 'undefined') {
                                        tabla.ajax.reload(null, false);
                                    }
                                });

                                if (modalInstance && modalInstance._focustrap)
                                    modalInstance._focustrap.activate();
                            });
                        } else {
                            Swal.fire('Error', res.msg || 'No se pudo desasignar la práctica.', 'error');
                        }
                    },
                    error: function () {
                        Swal.fire('Error', 'Ocurrió un error al procesar la solicitud.', 'error');
                    },
                    complete: function () {
                        if (modalInstance && modalInstance._focustrap)
                            modalInstance._focustrap.activate();
                    }
                });
            });
        });



    });
})(jQuery);
