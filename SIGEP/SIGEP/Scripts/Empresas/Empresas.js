$(function () {
    let tabla;

    // ===================== DataTable (carga desde BD) =====================
    function initTabla() {
        tabla = $('#tablaListaEmpresas').DataTable({
            ajax: {
                url: urlCrearEmpresas,
                type: 'GET',
                dataSrc: 'data'
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

    // ===================== Guardar (Crear) =====================
    $('#BtnGuardarEmpresa').on('click', function () {
        const payload = {
            NombreEmpresa: $('#nombreEmpresa').val().trim(),
            NombreContacto: $('#nombreContacto').val().trim(),
            Email: $('#emailContacto').val().trim(),
            Telefono: $('#telefonoContacto').val().trim(),
            Provincia: $('#ProvinciaEmpresa').val(),
            Canton: $('#CantonEmpresa').val(),
            Distrito: $('#DistritoEmpresa').val(),
            Direccion: $('#direccion').val().trim(),
            Areas: $('#areas').val().trim()
        };

        let errores = [];

        if (!payload.NombreEmpresa) errores.push("Debe ingresar el nombre de la empresa.");
        if (!payload.NombreContacto) errores.push("Debe ingresar el nombre del contacto.");
        if (!payload.Email) {
            errores.push("Debe ingresar el correo electrónico.");
        } else {
            const regexEmail = /^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$/;
            if (!regexEmail.test(payload.Email)) {
                errores.push("El correo electrónico no es válido (ej: correo@empresa.com).");
            }
        }
        if (!payload.Telefono) {
            errores.push("Debe ingresar el teléfono.");
        } else {
        
            const tel = payload.Telefono;
            const soloDigitos = /^[0-9]{8}$/;

            if (!soloDigitos.test(tel)) {
                errores.push("El teléfono debe contener exactamente 8 dígitos numéricos.");
            }
        }
        if (!payload.Provincia) errores.push("Debe seleccionar una provincia.");
        if (!payload.Canton) errores.push("Debe seleccionar un cantón.");
        if (!payload.Distrito) errores.push("Debe seleccionar un distrito.");

        if (errores.length > 0) {
            Swal.fire({
                icon: 'warning',
                title: 'Campos requeridos',
                html: errores.join('<br>')
            });
            return;
        }

        $.post(urlCrear, payload)
            .done(resp => {
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
            .fail(() => Swal.fire({ icon: 'error', title: 'Error', text: 'No se pudo conectar.' }));
    });

    // ===================== Editar: abrir modal y precargar =====================
    document.addEventListener('click', function (e) {
        const btn = e.target.closest('.btn-editar-empresa');
        if (!btn) return;

        const id = btn.getAttribute('data-id');

        fetch(urlObtener + '?id=' + encodeURIComponent(id), {
            method: 'GET'
        })
            .then(response => response.json())
            .then(resp => {
                if (!resp.ok) {
                    Swal.fire({ icon: 'error', title: 'Error', text: resp.msg });
                    return;
                }

                const e = resp.data;

            
                const empresaIdEditar = document.getElementById('empresaIdEditar');
                const nombreEmpresaEditar = document.getElementById('nombreEmpresaEditar');
                const contactoEmpresaEditar = document.getElementById('contactoEmpresaEditar');
                const emailEmpresaEditar = document.getElementById('emailEmpresaEditar');
                const telefonoEmpresaEditar = document.getElementById('telefonoEmpresaEditar');
                const direccionEmpresaEditar = document.getElementById('direccionEmpresaEditar');
                const areasEmpresaEditar = document.getElementById('areasEmpresaEditar');

                if (empresaIdEditar) empresaIdEditar.value = e.IdEmpresa;
                if (nombreEmpresaEditar) nombreEmpresaEditar.value = e.NombreEmpresa;
                if (contactoEmpresaEditar) contactoEmpresaEditar.value = e.NombreContacto;
                if (emailEmpresaEditar) emailEmpresaEditar.value = e.Email;
                if (telefonoEmpresaEditar) telefonoEmpresaEditar.value = e.Telefono;
                if (direccionEmpresaEditar) direccionEmpresaEditar.value = e.Direccion || '';
                if (areasEmpresaEditar) areasEmpresaEditar.value = e.Areas || '';

               
                const provinciaSelect = document.getElementById('ProvinciaEmpresaEditar');
                const cantonSelect = document.getElementById('CantonEmpresaEditar');
                const distritoSelect = document.getElementById('DistritoEmpresaEditar');

                if (provinciaSelect && cantonSelect && distritoSelect) {

                   
                    cantonSelect.innerHTML = '<option value="">Seleccione un cantón</option>';
                    distritoSelect.innerHTML = '<option value="">Seleccione un distrito</option>';

                    provinciaSelect.value = e.Provincia || '';

                    if (e.Provincia) {
                       
                        cargarCantones(provinciaSelect);

                        
                        setTimeout(() => {
                            cantonSelect.value = e.Canton || '';

                            if (cantonSelect.value) {
                            
                                cargarDistritos(cantonSelect);

                            
                                setTimeout(() => {
                                    distritoSelect.value = e.Distrito || '';
                                }, 80);
                            }
                        }, 80);
                    }
                }

                // Mostrar el modal
                const modalElement = document.getElementById('ModalEditarEmpresa');
                const modalInstance = new bootstrap.Modal(modalElement);
                modalInstance.show();
            })
            .catch(() => {
                Swal.fire({
                    icon: 'error',
                    title: 'Error',
                    text: 'No se pudo obtener la empresa.'
                });
            });
    });


    // ===================== Guardar cambios (Editar) =====================
    $('#btnGuardarCambiosEmpresa').on('click', function () {
        const payload = {
            IdEmpresa: $('#empresaIdEditar').val(),
            NombreEmpresa: $('#nombreEmpresaEditar').val().trim(),
            NombreContacto: $('#contactoEmpresaEditar').val().trim(),
            Email: $('#emailEmpresaEditar').val().trim(),
            Telefono: $('#telefonoEmpresaEditar').val().trim(),
            Provincia: $('#ProvinciaEmpresaEditar').val(),
            Canton: $('#CantonEmpresaEditar').val(),
            Distrito: $('#DistritoEmpresaEditar').val(),
            Direccion: $('#direccionEmpresaEditar').val().trim(),
            Areas: $('#areasEmpresaEditar').val().trim()
        };

        let errores = [];

        if (!payload.NombreEmpresa) errores.push("Debe ingresar el nombre de la empresa.");
        if (!payload.NombreContacto) errores.push("Debe ingresar el nombre del contacto.");
        if (!payload.Email) {
            errores.push("Debe ingresar el correo electrónico.");
        } else {
            const regexEmail = /^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$/;
            if (!regexEmail.test(payload.Email)) {
                errores.push("El correo electrónico no es válido (ej: correo@empresa.com).");
            }
        }

        if (!payload.Telefono) {
            errores.push("Debe ingresar el teléfono.");
        } else {
            const soloDigitos = /^[0-9]{8}$/;
            if (!soloDigitos.test(payload.Telefono)) {
                errores.push("El teléfono debe contener exactamente 8 dígitos numéricos.");
            }
        }

        if (!payload.Provincia) errores.push("Debe seleccionar una provincia.");
        if (!payload.Canton) errores.push("Debe seleccionar un cantón.");
        if (!payload.Distrito) errores.push("Debe seleccionar un distrito.");

        if (errores.length > 0) {
            Swal.fire({
                icon: 'warning',
                title: 'Campos requeridos',
                html: errores.join('<br>')
            });
            return;
        }

        $.post(urlEditar, payload)
            .done(resp => {
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
            .fail(() => Swal.fire({ icon: 'error', title: 'Error', text: 'No se pudo conectar.' }));
    });


    // ===================== Eliminar (soft delete + cancelar vacantes) =====================
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

            $.post(urlEliminar, { id })
                .done(resp => {
                    if (resp.ok) {
                        Swal.fire({ icon: 'success', title: 'Eliminada', text: resp.msg })
                            .then(() => tabla.ajax.reload(null, false));
                    } else {
                        Swal.fire({ icon: 'error', title: 'Error', text: resp.msg });
                    }
                })
                .fail(() => Swal.fire({ icon: 'error', title: 'Error', text: 'No se pudo conectar.' }));
        });
    });

    // ===================== Init =====================
    $(document).ready(function () {
        initTabla();
    });

});