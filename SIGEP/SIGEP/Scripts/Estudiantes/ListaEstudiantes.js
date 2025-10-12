// ~/Scripts/Estudiantes/ListaEstudiantes.js
(function ($) {
    $(function () {
        const CFG = window.EstuCfg || { rol: 0, urls: {} };

        // === Guard: redirigir SOLO si realmente vino el Login ===
        function redirSiLogin(res, xhr) {
            try {
                if (typeof res !== 'string') return false;
                var urlLogin = CFG.urls.login || '';
                var looksLikeLogin =
                    res.indexOf('id="formLogin"') >= 0 ||
                    (urlLogin && res.indexOf('action="' + urlLogin + '"') >= 0) ||
                    /Iniciar sesi[óo]n/i.test(res);
                var isFullDoc = res.indexOf('<!DOCTYPE html') >= 0 && /login/i.test(res);
                if (looksLikeLogin || isFullDoc) {
                    window.location.href = urlLogin;
                    return true;
                }
            } catch (e) { }
            return false;
        }

        var rol = parseInt(CFG.rol || 0, 10); // 1=Est, 2=Coord, 3=Prof, 4=Egr

        // === Helper visual para Estado de Práctica (coincide con clases CSS del proyecto)
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
                // compatibilidad
                'en progreso': { cls: 'badge-en-progreso', txt: 'En progreso' }
            };

            var info = mapa[est] || { cls: 'badge-no-asignada', txt: (estadoOriginal || 'No Asignada') };
            return '<span class="badge ' + info.cls + '">' + info.txt + '</span>';
        }

        // ===============================
        // DataTable PRINCIPAL
        // ===============================
        var table = $('#tablaEstudiantes').DataTable({
            ajax: {
                url: CFG.urls.get,
                data: function (d) {
                    d.estado = $('#filtroEstado').val();

                    // Si hay dropdown de especialidad (coordinador), envíalo; si es profesor (no existe), manda 0
                    var $esp = $('#filtroEspecialidad');
                    d.idEspecialidad = $esp.length ? ($esp.val() || 0) : 0;
                },
                dataSrc: 'data'
            },
            columns: [
                { data: 'Cedula' },
                { data: 'NombreCompleto' },
                { data: 'EspecialidadNombre' },
                { data: 'Telefono' },
                {
                    data: 'EstadoNombre',
                    render: function (data) {
                        var text = (data || '').toString().trim();
                        if (!text) return '<span class="badge badge-no-asignada">Sin Estado</span>';
                        var d = text.toLowerCase();
                        if (d === 'activo') return '<span class="badge badge-activo">' + text + '</span>';
                        if (d === 'inactivo') return '<span class="badge badge-inactivo">' + text + '</span>';
                        if (d === 'aprobada' || d === 'aprobado') return '<span class="badge badge-aprobado">' + text + '</span>';
                        if (d === 'rezagado') return '<span class="badge badge-rezagado">' + text + '</span>';
                        return '<span class="badge badge-no-asignada">' + text + '</span>';
                    }
                },
                {
                    data: 'EstadoPractica',
                    render: function (data) {
                        var text = (data || '').toString().trim();
                        if (!text || /^no asignada$/i.test(text) || /^sin practica$/i.test(
                            text.normalize('NFD').replace(/[\u0300-\u036f]/g, '')
                        )) {
                            return '<span class="badge badge-no-asignada">No Asignada</span>';
                        }
                        return badgeEstado(text);
                    }
                },
                {
                    // Acciones: "Ver perfil" para todos; "Actualizar estado" solo Coordinador(2)/Profesor(3)
                    data: 'IdUsuario',
                    render: function (data, type, row) {
                        var html =
                            '<button class="btn bg-transparent verPerfil" data-id="' + data + '" style="color:#2d594d" title="Ver perfil">' +
                            '<i class="fas fa-eye"></i>' +
                            '</button>';
                        if (rol === 2 || rol === 3) {
                            html +=
                                '<button class="btn bg-transparent btn-actualizar-estado" data-id="' + data + '" data-estado="' + (row.IdEstado || 0) + '" style="color:#2d594d" title="Actualizar estado">' +
                                '<i class="fas fa-sync-alt"></i>' +
                                '</button>';
                        }
                        return html;
                    }
                }
            ],
            columnDefs: [{ targets: -1, orderable: false, searchable: false, width: "100px" }]
        });

        // Filtros (el de especialidad solo existe para coordinador)
        $('#filtroEstado').on('change', function () { table.ajax.reload(); });
        if ($('#filtroEspecialidad').length) {
            $('#filtroEspecialidad').on('change', function () { table.ajax.reload(); });
        }

        // ===============================
        // Abrir modal de perfil
        // ===============================
        $('#tablaEstudiantes').on('click', '.verPerfil', function () {
            const id = $(this).data('id');
            const modalPerfilEl = document.getElementById('modalPerfil');
            const modalPerfil = bootstrap.Modal.getOrCreateInstance(modalPerfilEl);

            $('#perfilBody').html(
                '<div class="text-center p-3">' +
                '<div class="spinner-border text-success" role="status"></div>' +
                '<p class="mt-2 text-muted">Cargando perfil...</p>' +
                '</div>'
            );

            $.ajax({
                url: CFG.urls.detalle,
                type: 'GET',
                data: { id: id },
                success: function (html, _status, xhr) {
                    if (redirSiLogin(html, xhr)) return;
                    if (!html || html.trim() === "") {
                        $('#perfilBody').html('<div class="alert alert-warning">No se pudo cargar el perfil del estudiante.</div>');
                    } else {
                        $('#perfilBody').html(html);
                    }
                    modalPerfil.show();
                },
                error: function (xhr) {
                    var detail = xhr && (xhr.responseText || xhr.statusText) ? (xhr.responseText || xhr.statusText) : 'Error desconocido';
                    $('#perfilBody').html('<div class="alert alert-danger">Error al cargar el perfil.<br/><small>' + $('<div/>').text(detail).html() + '</small></div>');
                    modalPerfil.show();
                }
            });
        });

        // ===============================
        // Modal actualizar estado (solo rol 2/3)
        // ===============================
        $(document).on("click", ".btn-actualizar-estado", function () {
            var idUsuario = $(this).data("id");
            var estadoActual = $(this).data("estado");
            $("#hdnIdUsuario").val(idUsuario);
            $("#ddlNuevoEstado").val(estadoActual);
            $("#modalActualizarEstado").modal("show");
        });

        $("#btnConfirmarActualizar").click(function () {
            var idUsuario = $("#hdnIdUsuario").val();
            var nuevoEstado = $("#ddlNuevoEstado").val();

            $.ajax({
                url: CFG.urls.actualizarEstado,
                type: 'POST',
                data: { idUsuario: idUsuario, nuevoEstadoId: nuevoEstado },
                success: function (res, _status, xhr) {
                    if (redirSiLogin(res, xhr)) return;
                    if (res.success) {
                        $("#modalActualizarEstado").modal("hide");
                        table.ajax.reload();
                        Swal.fire("Éxito", res.message, "success");
                    } else {
                        Swal.fire("Error", res.message, "error");
                    }
                },
                error: function () {
                    Swal.fire("Error", "Ocurrió un error al procesar la solicitud", "error");
                }
            });
        });

        // ===============================
        // Eliminar documento
        // ===============================
        $(document).on("click", ".btn-eliminar-doc", function (e) {
            e.preventDefault();
            var boton = $(this);
            var idDoc = boton.data("id");

            Swal.fire({
                title: '¿Eliminar documento?',
                text: "No podrás deshacer esta acción",
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: 'Sí, eliminar',
                cancelButtonText: 'Cancelar'
            }).then((result) => {
                if (result.isConfirmed) {
                    $.ajax({
                        type: "POST",
                        url: CFG.urls.eliminarDocumento,
                        data: { id: idDoc },
                        success: function (res, _status, xhr) {
                            if (redirSiLogin(res, xhr)) return;
                            if (res.success) {
                                Swal.fire({
                                    title: "Eliminado",
                                    text: "Documento eliminado con éxito",
                                    icon: "success",
                                    timer: 1000,
                                    showConfirmButton: false
                                });
                                boton.closest('.d-flex').fadeOut(300, function () { $(this).remove(); });
                            } else {
                                Swal.fire("Error", res.message || "No se pudo eliminar", "error");
                            }
                        },
                        error: function () {
                            Swal.fire("Error", "Ocurrió un error al procesar la solicitud", "error");
                        }
                    });
                }
            });
        });

        // ===============================
        // Desasignar práctica
        // ===============================
        $(document).on("click", ".BtnDesasignarPracticaEstudiante", function (e) {
            e.preventDefault();
            var boton = $(this);
            var idPractica = boton.data("idpractica");

            Swal.fire({
                title: '¿Desea desasignar esta práctica?',
                text: 'El estado se cambiará a "Retirada".',
                icon: 'warning',
                input: 'textarea',
                inputLabel: 'Comentario (opcional)',
                inputPlaceholder: 'Escribe un comentario...',
                showCancelButton: true,
                confirmButtonText: 'Sí, desasignar',
                cancelButtonText: 'Cancelar'
            }).then((result) => {
                if (result.isConfirmed) {
                    $.post(CFG.urls.desasignarPractica,
                        { idPractica: idPractica, comentario: result.value || '' })
                        .done(function (res, status, xhr) {
                            if (redirSiLogin(res, xhr)) return;
                            if (res.ok) {
                                Swal.fire("Desasignado", res.msg || "La práctica fue desasignada correctamente", "success");
                            } else {
                                Swal.fire("Error", res.msg || "No se pudo desasignar", "error");
                            }
                        })
                        .fail(function () {
                            Swal.fire("Error", "Ocurrió un error al procesar la solicitud", "error");
                        });
                }
            });
        });

    });
})(jQuery);
