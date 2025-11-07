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

        function badgeEstado(estadoOriginal) {
            const est = (estadoOriginal || '')
                .toString()
                .normalize('NFD').replace(/[\u0300-\u036f]/g, '')
                .toLowerCase()
                .replace(/\s+/g, ' ')
                .trim();

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

       
        function validarFechas(fechaAplic, fechaCierre) {
            const f1 = new Date(fechaAplic);
            const f2 = new Date(fechaCierre);
            return f1 && f2 && f1 > f2;
        }

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
                data: d => ({
                    idEstado: $('#filtroPractica').val() || 0,
                    idEspecialidad: $('#filtroEspecialidad').val() || 0,
                    idModalidad: $('#filtroModalidad').val() || 0
                }),
                dataSrc: function (json) {
                   
                    if (typeof json === 'string') {
                        // ¿vino HTML?
                        if (json.indexOf('<!DOCTYPE html') >= 0 || json.indexOf('<html') >= 0) {
                            console.error('⚠️ El servidor devolvió HTML (posible login/500).');
                            Swal.fire('Error', 'La sesión puede haber expirado o el servidor devolvió HTML.', 'error');
                            return [];
                        }
                      
                        try { json = JSON.parse(json); } catch (e) {
                            console.error('⚠️ Respuesta no JSON:', json);
                            Swal.fire('Error', 'Respuesta no válida del servidor.', 'error');
                            return [];
                        }
                    }

                   
                    if (json && json.ok === false) {
                        console.error('❌ Backend error:', json.error);
                        Swal.fire('Error', json.error || 'Error en servidor.', 'error');
                        return [];
                    }

                   
                    return (json && Array.isArray(json.data)) ? json.data : [];
                },
                error: function (xhr) {
                    const ct = xhr.getResponseHeader('content-type') || '';
                    if (ct.indexOf('text/html') >= 0) {
                        console.error('⚠️ HTML recibido en AJAX:', xhr.responseText?.substring(0, 500));
                        Swal.fire('Error', 'Se recibió HTML en lugar de JSON (¿login/500?).', 'error');
                    } else {
                        console.error('❌ Error AJAX:', xhr.status, xhr.responseText);
                        Swal.fire('Error', `Error consultando vacantes (${xhr.status}).`, 'error');
                    }
                }
            },
            columns: [
                { data: 'EmpresaNombre', title: 'Empresa' },
                { data: 'EspecialidadNombre', title: 'Especialidad' },
                { data: 'Requerimientos', title: 'Requisitos' },
                { data: 'NumCupos', title: 'Cupos Disponibles' },
                { data: 'NumPostulados', title: 'Estudiantes Postulados', render: d => `<strong>${d || 0}</strong>` },
                { data: 'EstadoNombre', title: 'Estado', render: d => badgeEstado(d) },
                {
                    data: 'IdVacante',
                    orderable: false,
                    title: 'Acciones',
                    render: (data, type, row) => {
                        const estado = (row.EstadoNombre || '').toLowerCase();
                        const inactivo = estado === 'inactivo' || estado === 'archivado';
                        const dis = inactivo ? 'disabled aria-disabled="true"' : '';
                        const muted = inactivo ? 'opacity:0.35; cursor:not-allowed;' : '';

                        let acc = `
                    <button class="btn bg-transparent btn-visualizar" data-id="${data}" title="Visualizar" style="color:#2d594d">
                        <i class="fas fa-eye"></i>
                    </button>`;

                        if ((CFG.rol === 2 || CFG.rol === 3) && !(row.Nombre || '').includes('Práctica Autogestionada')) {
                            acc += `
                        <button class="btn bg-transparent btn-asignar" data-id="${data}" style="color:#2d594d; ${muted}" ${dis}>
                            <i class="fas fa-user-plus"></i>
                        </button>`;
                        }

                        if (CFG.rol === 2) {
                            acc += `
                        <button class="btn bg-transparent btn-editar" data-id="${data}" style="color:#2d594d; ${muted}" ${dis}>
                            <i class="fas fa-sync-alt"></i>
                        </button>
                        <button class="btn bg-transparent btn-eliminar" data-id="${data}" style="color:#2d594d; ${muted}" ${dis}>
                            <i class="fas fa-archive"></i>
                        </button>`;
                        }
                        return acc;
                    }
                }
            ],
            language: { url: "//cdn.datatables.net/plug-ins/1.13.6/i18n/es-ES.json" }
        });

        $('#filtroPractica, #filtroEspecialidad, #filtroModalidad').on('change', () => tabla.ajax.reload());



        // =====================================================
        // 🔹 Visualizar Vacante + Postulaciones
        // =====================================================
        $('#miTabla').on('click', '.btn-visualizar', function () {
            const id = $(this).data('id');
            $.get(CFG.urls.detalle, { id }, (res, _, xhr) => {
                if (redirSiLogin(res, xhr)) return;
                if (!res.ok || !res.data) return Swal.fire('Error', 'No se pudo cargar la vacante.', 'error');

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
                $('#modalVisualizarVacante').modal('show');

                $('#modalVisualizarVacante').data('idVacante', id);

                // =====================================================
                // 🔹 Cargar postulaciones en el modal "Visualizar Vacante"
                // =====================================================
                $.getJSON(CFG.urls.obtenerPostulaciones, { idVacante: id }, r2 => {
                    console.log("📦 Datos devueltos por el backend:", r2);

                    const $lista = $('#listaPostulaciones').empty();
                    $('#mensajeSinPostulaciones').toggle(!r2.ok || !r2.data?.length);

                    if (r2.ok && r2.data?.length) {
                        r2.data.forEach(p => {
                            console.log("🧾 Postulación procesada:", p);

                            const estado = p.EstadoDescripcion || 'Sin estado';
                            const estNorm = estado
                                .normalize('NFD').replace(/[\u0300-\u036f]/g, '')
                                .toLowerCase().trim();

                            const badge = badgeEstado(estado);
                            const mostrarBoton = ['asignada', 'en proceso de aplicacion'].includes(estNorm);

                            const btnDes = mostrarBoton
                                ? `<button class="btn bg-transparent BtnDesasignarPracticaEstudiante"
         data-idpractica="${p.IdPractica}" 
         data-nombre="${escapeHtml(p.NombreCompleto)}"
         title="Desasignar práctica" style="color:#2D594D;">
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
        // 🔹 Asignar Estudiantes
        // =====================================================
        $('#miTabla').on('click', '.btn-asignar', function () {
            const idVacante = $(this).data('id');
            $('#modalAsignar').data('idVacante', idVacante).modal('show');

            $.getJSON(CFG.urls.obtenerEstudiantesAsignar, {
                idVacante,
                idUsuarioSesion: CFG.idUsuarioSesion || 0
            }, res => {
                const tbody = $('#miTablaAsignar tbody').empty();
                if (!res?.ok || !res.data?.length)
                    return tbody.append('<tr><td colspan="6" class="text-center text-muted">No hay estudiantes disponibles</td></tr>');

                res.data.forEach(e => {
                    const estadoPractica = (e.EstadoPractica || 'Sin proceso activo').toLowerCase();
                    const badge = badgeEstado(e.EstadoPractica);

                    let btn = ''; 

                    const estadoVacante = (e.EstadoVacante || e.EstadoPractica || 'Sin proceso activo')
                        .normalize('NFD')
                        .replace(/[\u0300-\u036f]/g, '')
                        .toLowerCase()
                        .trim();

                    if (["sin proceso activo", "retirada", "en proceso de aplicacion"].includes(estadoVacante)) {
                        btn = `<button class="btn btn-sm btn-outline-success btn-asignar-estudiante"
            data-idusuario="${e.IdUsuario}"
            data-nombre="${escapeHtml(e.NombreCompleto)}">
            <i class="fas fa-user-plus"></i> Asignar
        </button>`;
                    } else if (estadoVacante === "asignada") {
                        btn = `<button class="btn btn-sm btn-outline-warning btn-retirar-estudiante"
            data-idusuario="${e.IdUsuario}"
            data-nombre="${escapeHtml(e.NombreCompleto)}">
            <i class="fas fa-user-minus"></i> Retirar
        </button>`;
                    } else if (["rechazada", "aprobada", "en curso", "finalizada", "rezagado", "archivado"].includes(estadoVacante)) {
                        btn = `<button class="btn btn-sm btn-outline-secondary" disabled>
            <i class="fas fa-ban"></i> No disponible
        </button>`;
                    } else {
                        btn = `<button class="btn btn-sm btn-outline-secondary" disabled>
            <i class="fas fa-question"></i> Estado desconocido
        </button>`;
                    }

                    

                    tbody.append(`
        <tr class="$">
            <td>${escapeHtml(e.NombreCompleto)}</td>
            <td>${escapeHtml(e.Cedula || '')}</td>
            <td>${escapeHtml(e.Especialidad || '')}</td>
            <td class="text-center">${badge}</td>
            <td class="text-center">${btn}</td>
        </tr>
    `);

                });
            });
        });

        // =====================================================
        // 🔹 Validación previa al asignar estudiante
        // =====================================================
        $(document).on('click', '.btn-asignar-estudiante', function () {
            const idUsuario = $(this).data('idusuario');
            const idVacante = $('#modalAsignar').data('idVacante');
            const nombre = $(this).data('nombre');

            $.post(CFG.urls.asignarEstudiante, { idUsuario, idVacante })
                .done(res => {
                    
                    if (res.ok) {
                        Swal.fire({
                            icon: 'success',
                            title: 'Éxito',
                            text: `El estudiante ${nombre} fue asignado correctamente.`,
                            timer: 2000,
                            showConfirmButton: false
                        });
                        $('#modalAsignar').modal('hide');
                        tabla.ajax.reload(null, false);
                    } else {
                        Swal.fire('Error', res.message || 'No se pudo asignar el estudiante.', 'error');
                    }
                })
                .fail(() => Swal.fire('Error', 'Error al asignar estudiante.', 'error'));
        });




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
                if (!res.ok || !res.data) return Swal.fire('Error', 'No se pudo cargar la información.', 'error');
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
                    } else Swal.fire('Error', res.message, 'error');
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
                        } else Swal.fire('Aviso', res.message, 'warning');
                    })
                    .fail(() => Swal.fire('Error', 'Error al archivar la vacante.', 'error'));
            });
        });

       

        
        // =====================================================
        // 🔹 Desasignar práctica (mantiene el modal abierto y actualiza lista)
        // =====================================================
        $(document).on("click", ".BtnDesasignarPracticaEstudiante", function (e) {
            e.preventDefault();

            const boton = $(this);
            const idPractica = boton.data("idpractica");
            const nombreEst = boton.data("nombre");
            const idVacante = $('#modalVisualizarVacante').data('idVacante');

            if (!idPractica) {
                Swal.fire('Error', 'No se encontró el identificador de la práctica.', 'error');
                return;
            }

            const modalVisual = document.getElementById("modalVisualizarVacante");
            const modalInstance = modalVisual ? bootstrap.Modal.getInstance(modalVisual) : null;

            if (modalInstance && modalInstance._focustrap) modalInstance._focustrap.deactivate();

            Swal.fire({
                title: '¿Deseas desasignar esta práctica?',
                text: `El estado de ${nombreEst || 'el estudiante'} se cambiará a "Retirada".`,
                icon: 'warning',
                input: 'textarea',
                inputLabel: 'Comentario (opcional)',
                inputPlaceholder: 'Escribe un comentario...',
                showCancelButton: true,
                confirmButtonText: 'Sí, desasignar',
                cancelButtonText: 'Cancelar',
                allowOutsideClick: false,
                confirmButtonColor: '#2d594d',
                didOpen: () => {
                    const textarea = Swal.getInput();
                    if (textarea) {
                        textarea.removeAttribute("readonly");
                        textarea.removeAttribute("disabled");
                        textarea.focus();
                    }
                },
                didClose: () => {
                    if (modalInstance && modalInstance._focustrap) modalInstance._focustrap.activate();
                }
            }).then(result => {
                if (!result.isConfirmed) {
                    if (modalInstance && modalInstance._focustrap) modalInstance._focustrap.activate();
                    return;
                }

                $.ajax({
                    url: CFG.urls.desasignarPractica,
                    type: 'POST',
                    data: {
                        idPractica: idPractica,
                        comentario: result.value || ''
                    },
                    success: function (res, status, xhr) {
                        if (redirSiLogin(res, xhr)) return;

                        if (res.ok) {
                            Swal.fire({
                                title: "Desasignado",
                                text: res.msg || "La práctica fue desasignada correctamente.",
                                icon: "success",
                                timer: 1500,
                                showConfirmButton: false
                            }).then(() => {
                                
                                if (modalInstance && modalInstance._focustrap)
                                    modalInstance._focustrap.deactivate();

                              
                                $.getJSON(CFG.urls.obtenerPostulaciones, { idVacante: idVacante }, r2 => {
                                    const $lista = $('#listaPostulaciones').empty();
                                    $('#mensajeSinPostulaciones').toggle(!r2.ok || !r2.data?.length);

                                    if (r2.ok && r2.data?.length) {
                                        r2.data.forEach(p => {
                                            const estado = p.EstadoDescripcion || 'Sin estado';
                                            const estNorm = estado.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase().trim();
                                            const badge = badgeEstado(estado);
                                            const mostrarBoton = ['asignada', 'en proceso de aplicacion'].includes(estNorm);

                                            const btnDes = mostrarBoton
                                                ? `<button class="btn bg-transparent BtnDesasignarPracticaEstudiante"
                                             data-idpractica="${p.IdPractica}" 
                                             data-nombre="${escapeHtml(p.NombreCompleto)}"
                                             title="Desasignar práctica" style="color:#2D594D;">
                                             <i class="fas fa-trash-alt"></i>
                                           </button>`
                                                : '';

                                            // ✅ Enlace funcional directo
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

                                   
                                    if (modalInstance && modalInstance._focustrap)
                                        modalInstance._focustrap.activate();
                                });

                               
                                if (typeof tabla !== "undefined")
                                    tabla.ajax.reload(null, false);
                            });
                        } else {
                            Swal.fire("Error", res.msg || "No se pudo desasignar la práctica.", "error");
                        }
                    },
                    error: function () {
                        Swal.fire("Error", "Ocurrió un error al procesar la solicitud.", "error");
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
