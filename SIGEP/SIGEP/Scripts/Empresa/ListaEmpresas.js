// ===================== Variables Globales =====================
let tabla;

// ===================== Validar sesión =====================
function validarSesion(response) {
    if (response && response.success === false && response.message === "Sesión expirada") {
        Swal.fire({
            icon: 'warning',
            title: 'Sesión Expirada',
            text: 'Su sesión ha expirado. Será redirigido al login.',
            confirmButtonText: 'Aceptar'
        }).then(() => {
            window.location.href = '/Home/Login';
        });
        return false;
    }
    return true;
}

// ===================== DataTable (carga desde BD) =====================
function initTabla() {
    tabla = $('#tablaEmpresas').DataTable({
        ajax: {
            url: '/Empresa/GetEmpresas',
            type: 'GET',
            dataSrc: function (json) {
                if (!validarSesion(json)) return [];
                return json.data || [];
            },
            error: function (xhr, error, code) {
                console.error('Error al cargar empresas:', error);
                if (xhr.status === 401 || xhr.status === 403) {
                    window.location.href = '/Home/Login';
                }
            }
        },
        columns: [
            { data: 'NombreEmpresa' },
            { data: 'AreasAfines', defaultContent: '' },
            { data: 'Ubicacion', defaultContent: '' },
            {
                data: 'HistorialVacantes',
                render: function (x) { return (x || 0) + ' vacantes anteriores'; }
            },
            {
                data: 'IdEmpresa',
                orderable: false,
                render: function (id, type, row) {
                    return `
                        <button class="btn-accion btn-editar-empresa" data-id="${id}" title="Editar">
                            <i class="fas fa-edit"></i>
                        </button>
                        <a href="#" class="btn-accion btn-eliminar-empresa" data-id="${id}" data-nombre="${row.NombreEmpresa}" title="Eliminar">
                            <i class="fas fa-trash-alt"></i>
                        </a>`;
                }
            }
        ],
        responsive: true,
        language: {
            url: '//cdn.datatables.net/plug-ins/1.13.6/i18n/es-ES.json'
        }
    });
}

// ===================== Utilidades Catálogos =====================
function cargarProvincias($select, selected) {
    $select.empty().append('<option value="">Seleccione una provincia</option>');
    $.get('/Empresa/GetProvincias', function (response) {
        if (!validarSesion(response)) return;
        const list = Array.isArray(response) ? response : [];
        list.forEach(x => $select.append(`<option value="${x.Nombre}" data-id="${x.IdProvincia}">${x.Nombre}</option>`));
        if (selected) $select.val(selected);
    }).fail(function (xhr) {
        if (xhr.status === 401 || xhr.status === 403) {
            window.location.href = '/Home/Login';
        }
    });
}

function obtenerIdProvinciaPorNombre(nombre, cb) {
    $.get('/Empresa/GetProvincias', function (response) {
        if (!validarSesion(response)) return;
        const list = Array.isArray(response) ? response : [];
        const p = list.find(x => x.Nombre === nombre);
        cb(p ? p.IdProvincia : null);
    }).fail(function (xhr) {
        if (xhr.status === 401 || xhr.status === 403) {
            window.location.href = '/Home/Login';
        }
    });
}

function cargarCantonesPorProvincia($select, idProvincia, selected) {
    $select.empty().append('<option value="">Seleccione un cantón</option>');
    if (!idProvincia) return;
    $.get('/Empresa/GetCantones', { idProvincia }, function (response) {
        if (!validarSesion(response)) return;
        const list = Array.isArray(response) ? response : [];
        list.forEach(x => $select.append(`<option value="${x.Nombre}" data-id="${x.IdCanton}">${x.Nombre}</option>`));
        if (selected) $select.val(selected);
    }).fail(function (xhr) {
        if (xhr.status === 401 || xhr.status === 403) {
            window.location.href = '/Home/Login';
        }
    });
}

function obtenerIdCantonPorNombre(idProvincia, nombreCanton, cb) {
    $.get('/Empresa/GetCantones', { idProvincia }, function (response) {
        if (!validarSesion(response)) return;
        const list = Array.isArray(response) ? response : [];
        const c = list.find(x => x.Nombre === nombreCanton);
        cb(c ? c.IdCanton : null);
    }).fail(function (xhr) {
        if (xhr.status === 401 || xhr.status === 403) {
            window.location.href = '/Home/Login';
        }
    });
}

function cargarDistritosPorCanton($select, idCanton, selected) {
    $select.empty().append('<option value="">Seleccione un distrito</option>');
    if (!idCanton) return;
    $.get('/Empresa/GetDistritos', { idCanton }, function (response) {
        if (!validarSesion(response)) return;
        const list = Array.isArray(response) ? response : [];
        list.forEach(x => $select.append(`<option value="${x.Nombre}">${x.Nombre}</option>`));
        if (selected) $select.val(selected);
    }).fail(function (xhr) {
        if (xhr.status === 401 || xhr.status === 403) {
            window.location.href = '/Home/Login';
        }
    });
}

// ===================== Event Handlers - Modal Agregar =====================
function initModalAgregar() {
    $('#ModalAgregarEmpresa').on('shown.bs.modal', function () {
        cargarProvincias($('#provincia'));
        $('#canton').empty().append('<option value="">Seleccione un cantón</option>');
        $('#distrito').empty().append('<option value="">Seleccione un distrito</option>');
    });

    $('#provincia').on('change', function () {
        const nombreProv = $(this).val();
        $('#canton').empty().append('<option value="">Seleccione un cantón</option>');
        $('#distrito').empty().append('<option value="">Seleccione un distrito</option>');
        if (!nombreProv) return;
        obtenerIdProvinciaPorNombre(nombreProv, function (idProv) {
            cargarCantonesPorProvincia($('#canton'), idProv);
        });
    });

    $('#canton').on('change', function () {
        const nombreProv = $('#provincia').val();
        const nombreCanton = $(this).val();
        $('#distrito').empty().append('<option value="">Seleccione un distrito</option>');
        if (!nombreProv || !nombreCanton) return;

        obtenerIdProvinciaPorNombre(nombreProv, function (idProv) {
            if (!idProv) return;
            obtenerIdCantonPorNombre(idProv, nombreCanton, function (idCanton) {
                cargarDistritosPorCanton($('#distrito'), idCanton);
            });
        });
    });
}

// ===================== Event Handlers - Modal Editar =====================
function initModalEditar() {
    $('#provinciaEditar').on('change', function () {
        const nombreProv = $(this).val();
        $('#cantonEditar').empty().append('<option value="">Seleccione un cantón</option>');
        $('#distritoEditar').empty().append('<option value="">Seleccione un distrito</option>');
        if (!nombreProv) return;
        obtenerIdProvinciaPorNombre(nombreProv, function (idProv) {
            cargarCantonesPorProvincia($('#cantonEditar'), idProv);
        });
    });

    $('#cantonEditar').on('change', function () {
        const nombreProv = $('#provinciaEditar').val();
        const nombreCanton = $(this).val();
        $('#distritoEditar').empty().append('<option value="">Seleccione un distrito</option>');
        if (!nombreProv || !nombreCanton) return;

        obtenerIdProvinciaPorNombre(nombreProv, function (idProv) {
            if (!idProv) return;
            obtenerIdCantonPorNombre(idProv, nombreCanton, function (idCanton) {
                cargarDistritosPorCanton($('#distritoEditar'), idCanton);
            });
        });
    });
}

// ===================== Guardar (Crear) =====================
function guardarEmpresa() {
    $('#BtnGuardarEmpresa').on('click', function () {
        const payload = {
            NombreEmpresa: $('#nombreEmpresa').val().trim(),
            NombreContacto: $('#nombreContacto').val().trim(),
            Email: $('#emailContacto').val().trim(),
            Telefono: $('#telefonoContacto').val().trim(),
            Provincia: $('#provincia').val(),
            Canton: $('#canton').val(),
            Distrito: $('#distrito').val(),
            Direccion: $('#direccion').val().trim(),
            Areas: $('#areas').val().trim()
        };

        let errores = [];
        if (!payload.NombreEmpresa) errores.push("Debe ingresar el nombre de la empresa.");
        if (!payload.NombreContacto) errores.push("Debe ingresar el nombre del contacto.");
        if (!payload.Email) errores.push("Debe ingresar el correo electrónico.");
        if (!payload.Telefono) errores.push("Debe ingresar el teléfono.");
        if (!payload.Provincia) errores.push("Debe seleccionar una provincia.");
        if (!payload.Canton) errores.push("Debe seleccionar un cantón.");
        if (!payload.Distrito) errores.push("Debe seleccionar un distrito.");

        if (errores.length > 0) {
            Swal.fire({ icon: 'warning', title: 'Campos requeridos', html: errores.join('<br>') });
            return;
        }

        $.post('/Empresa/CrearEmpresa', payload)
            .done(resp => {
                if (!validarSesion(resp)) return;
                if (resp.ok) {
                    Swal.fire({ icon: 'success', title: 'Guardado', text: resp.msg })
                        .then(() => {
                            $('#ModalAgregarEmpresa').modal('hide');
                            tabla.ajax.reload(null, false);
                            $('#formAgregarEmpresa')[0].reset();
                        });
                } else {
                    Swal.fire({ icon: 'error', title: 'Error', text: resp.msg });
                }
            })
            .fail(function (xhr) {
                if (xhr.status === 401 || xhr.status === 403) {
                    window.location.href = '/Home/Login';
                } else {
                    Swal.fire({ icon: 'error', title: 'Error', text: 'No se pudo conectar.' });
                }
            });
    });
}

// ===================== Editar: abrir modal y precargar =====================
function editarEmpresa() {
    $(document).on('click', '.btn-editar-empresa', function () {
        const id = $(this).data('id');

        // Primero cargar provincias al select de editar
        cargarProvincias($('#provinciaEditar'));

        $.get('/Empresa/GetEmpresa', { id })
            .done(resp => {
                if (!validarSesion(resp)) return;
                if (!resp.ok) {
                    Swal.fire({ icon: 'error', title: 'Error', text: resp.msg });
                    return;
                }

                const e = resp.data;
                $('#empresaIdEditar').val(e.IdEmpresa);
                $('#nombreEmpresaEditar').val(e.NombreEmpresa);
                $('#contactoEmpresaEditar').val(e.NombreContacto);
                $('#emailEmpresaEditar').val(e.Email);
                $('#telefonoEmpresaEditar').val(e.Telefono);
                $('#direccionEmpresaEditar').val(e.Direccion || '');
                $('#areasEmpresaEditar').val(e.Areas || '');

                // Set Provincia y cargar niveles dependientes
                const prov = e.Provincia || '';
                const cant = e.Canton || '';
                const dist = e.Distrito || '';

                if (prov) {
                    // Espera corta para asegurar que provincias ya están en el DOM
                    setTimeout(function () {
                        $('#provinciaEditar').val(prov);
                        obtenerIdProvinciaPorNombre(prov, function (idProv) {
                            cargarCantonesPorProvincia($('#cantonEditar'), idProv, cant);
                            if (cant) {
                                obtenerIdCantonPorNombre(idProv, cant, function (idCanton) {
                                    cargarDistritosPorCanton($('#distritoEditar'), idCanton, dist);
                                });
                            }
                        });
                    }, 100);
                } else {
                    $('#cantonEditar').empty().append('<option value="">Seleccione un cantón</option>');
                    $('#distritoEditar').empty().append('<option value="">Seleccione un distrito</option>');
                }

                new bootstrap.Modal(document.getElementById('ModalEditarEmpresa')).show();
            })
            .fail(function (xhr) {
                if (xhr.status === 401 || xhr.status === 403) {
                    window.location.href = '/Home/Login';
                } else {
                    Swal.fire({ icon: 'error', title: 'Error', text: 'No se pudo obtener la empresa.' });
                }
            });
    });
}

// ===================== Guardar cambios (Editar) =====================
function guardarCambiosEmpresa() {
    $('#btnGuardarCambiosEmpresa').on('click', function () {
        const payload = {
            IdEmpresa: $('#empresaIdEditar').val(),
            NombreEmpresa: $('#nombreEmpresaEditar').val().trim(),
            NombreContacto: $('#contactoEmpresaEditar').val().trim(),
            Email: $('#emailEmpresaEditar').val().trim(),
            Telefono: $('#telefonoEmpresaEditar').val().trim(),
            Provincia: $('#provinciaEditar').val(),
            Canton: $('#cantonEditar').val(),
            Distrito: $('#distritoEditar').val(),
            Direccion: $('#direccionEmpresaEditar').val().trim(),
            Areas: $('#areasEmpresaEditar').val().trim()
        };

        if (!payload.NombreEmpresa || !payload.NombreContacto || !payload.Email || !payload.Telefono) {
            Swal.fire({ icon: 'warning', title: 'Error', text: 'Complete los campos obligatorios.' });
            return;
        }

        $.post('/Empresa/EditarEmpresa', payload)
            .done(resp => {
                if (!validarSesion(resp)) return;
                if (resp.ok) {
                    Swal.fire({ icon: 'success', title: 'Guardado', text: resp.msg })
                        .then(() => {
                            $('#ModalEditarEmpresa').modal('hide');
                            tabla.ajax.reload(null, false);
                        });
                } else {
                    Swal.fire({ icon: 'error', title: 'Error', text: resp.msg });
                }
            })
            .fail(function (xhr) {
                if (xhr.status === 401 || xhr.status === 403) {
                    window.location.href = '/Home/Login';
                } else {
                    Swal.fire({ icon: 'error', title: 'Error', text: 'No se pudo conectar.' });
                }
            });
    });
}

// ===================== Eliminar (soft delete + cancelar vacantes) =====================
function eliminarEmpresa() {
    $(document).on('click', '.btn-eliminar-empresa', function (e) {
        e.preventDefault();
        const id = $(this).data('id');
        const nombre = $(this).data('nombre');

        Swal.fire({
            title: '¿Eliminar empresa?',
            html: `¿Está seguro que desea eliminar <strong>${nombre}</strong>?<br><small>Las vacantes asociadas se marcarán como <b>Cancelado</b>.</small>`,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#d33',
            cancelButtonText: 'Cancelar',
            confirmButtonText: 'Sí, eliminar'
        }).then((r) => {
            if (!r.isConfirmed) return;

            $.post('/Empresa/EliminarEmpresa', { id })
                .done(resp => {
                    if (!validarSesion(resp)) return;
                    if (resp.ok) {
                        Swal.fire({ icon: 'success', title: 'Eliminada', text: resp.msg })
                            .then(() => tabla.ajax.reload(null, false));
                    } else {
                        Swal.fire({ icon: 'error', title: 'Error', text: resp.msg });
                    }
                })
                .fail(function (xhr) {
                    if (xhr.status === 401 || xhr.status === 403) {
                        window.location.href = '/Home/Login';
                    } else {
                        Swal.fire({ icon: 'error', title: 'Error', text: 'No se pudo conectar.' });
                    }
                });
        });
    });
}

// ===================== Inicialización =====================
$(document).ready(function () {
    initTabla();
    initModalAgregar();
    initModalEditar();
    guardarEmpresa();
    editarEmpresa();
    guardarCambiosEmpresa();
    eliminarEmpresa();
});