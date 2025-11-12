// ~/Scripts/Estudiantes/ListaEstudiantes.js
(function ($) {
    $(function () {
        const CFG = window.EstuCfg || { rol: 0, urls: {} };

        
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

       
        function badgeEstado(estadoOriginal) {
            var est = (estadoOriginal || '')
                .toString()
                .normalize('NFD').replace(/[\u0300-\u036f]/g, '')
                .toLowerCase().replace(/\s+/g, ' ').trim();

            var mapa = {
                'con procesos activos': { cls: 'badge-procesos-activos', txt: 'Con Procesos Activos' },
                'en proceso de aplicacion': { cls: 'badge-en-progreso', txt: 'En proceso de Aplicación' },
                'rechazada': { cls: 'badge-rechazada', txt: 'Rechazada' },
                'asignada': { cls: 'badge-asignada', txt: 'Asignada' },
                'aprobada': { cls: 'badge-aprobada', txt: 'Aprobada' },
                'retirada': { cls: 'badge-retirada', txt: 'Retirada' },
                'finalizada': { cls: 'badge-finalizada', txt: 'Finalizada' },
                'rezagado': { cls: 'badge-rezagado', txt: 'Rezagado' },
                'archivado': { cls: 'badge-archivado', txt: 'Archivado' },
                'en curso': { cls: 'badge-en-curso', txt: 'En Curso' },
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
                    data: 'EstadoAcademico',
                    render: function (data) {
                        if (data === true) {
                            return '<span class="badge badge-aprobado">Aprobada</span>';
                        } else if (data === false) {
                            return '<span class="badge badge-rezagado">Rezagado</span>';
                        } else {
                            return '<span class="badge badge-no-asignada">Sin Estado</span>';
                        }
                    }
                },

                {
                    
                    data: 'EstadoPractica',
                    render: function (data) {
                        var estado = (data || '').toString().trim().toLowerCase();

                        // Lista de estados que consideramos "procesos activos"
                        var procesosActivos = [
                            'asignada',
                            'en curso',
                            'en proceso de aplicacion',
                            'aprobada',
                            'rechazada',
                            'retirada',
                            'finalizada',
                            'rezagado',
                            'archivado',
                            'en progreso'
                        ];

                        // Si el estado contiene alguno de los "activos"
                        var tieneProcesoActivo = procesosActivos.some(e => estado.includes(e));

                        if (tieneProcesoActivo) {
                            return '<span class="badge badge-procesos-activos">Con Procesos Activos</span>';
                        } else {
                            return '<span class="badge badge-no-asignada">Sin Procesos Activos</span>';
                        }
                    }
                },

                {
                    
                    data: 'IdUsuario',
                    render: function (data, type, row) {
                        var html =
                            '<button class="btn bg-transparent btn-accion verPerfil" data-id="' + data + '" style="color:#2d594d" title="Ver perfil">' +
                            '<i class="fas fa-eye"></i>' +
                            '</button>';
                        if (rol === 2 || rol === 3) {
                            html +=
                                '<button class="btn bg-transparent btn-accion btn-actualizar-estado" data-id="' + data + '" data-estado="' + (row.IdEstado || 0) + '" style="color:#2d594d" title="Actualizar estado">' +
                                '<i class="fas fa-sync-alt"></i>' +
                                '</button>';
                        }
                        return html;
                    }
                }
            ],
            columnDefs: [{ targets: -1, orderable: false, searchable: false, width: "100px" }]
        });

       
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

            const boton = $(this);
            const idPractica = boton.data("idpractica");
            const idUsuario = boton.data("idusuario");

           
            const modalPerfil = bootstrap.Modal.getInstance(document.getElementById("modalPerfil"));

           
            if (modalPerfil && modalPerfil._focustrap) {
                modalPerfil._focustrap.deactivate();
            }

            Swal.fire({
                title: '¿Desea desasignar esta práctica?',
                text: 'El estado se cambiará a "Retirada".',
                icon: 'warning',
                input: 'textarea',
                inputLabel: 'Comentario (opcional)',
                inputPlaceholder: 'Escribe un comentario...',
                showCancelButton: true,
                confirmButtonText: 'Sí, desasignar',
                cancelButtonText: 'Cancelar',
                allowOutsideClick: false,
                didOpen: () => {
                    
                    const textarea = Swal.getInput();
                    if (textarea) {
                        textarea.removeAttribute("readonly");
                        textarea.removeAttribute("disabled");
                        textarea.focus();
                        setTimeout(() => textarea.focus(), 100);
                    }
                },
                didClose: () => {
                   
                    if (modalPerfil && modalPerfil._focustrap) {
                        modalPerfil._focustrap.activate();
                    }
                }
            }).then((result) => {
                if (result.isConfirmed) {
                    
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
                                    text: res.msg || "La práctica fue desasignada correctamente",
                                    icon: "success",
                                    timer: 1500,
                                    showConfirmButton: false
                                }).then(() => {
                                    
                                    $.ajax({
                                        url: CFG.urls.detalle,
                                        type: 'GET',
                                        data: { id: idUsuario },
                                        success: function (html) {
                                            if (html && html.trim() !== "") {
                                                $("#perfilBody").html(html);
                                            }
                                        }
                                    });

                                    
                                    if (typeof table !== "undefined") {
                                        table.ajax.reload(null, false);
                                    }
                                });

                            } else {
                                Swal.fire("Error", res.msg || "No se pudo desasignar", "error");
                            }
                        },
                        error: function () {
                            Swal.fire("Error", "Ocurrió un error al procesar la solicitud", "error");
                        }
                    });
                } else {
                    
                    if (modalPerfil && modalPerfil._focustrap) {
                        modalPerfil._focustrap.activate();
                    }
                }
            });
        });


    });
})(jQuery);
