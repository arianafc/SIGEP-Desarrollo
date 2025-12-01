
$(function () {

    var tabla = $('#miTabla').DataTable({
        responsive: true,
        language: {
            url: 'https://cdn.datatables.net/plug-ins/1.13.6/i18n/es-ES.json'
        },
        pageLength: 10,
        lengthMenu: [5, 10, 25, 50],
        order: [[3, 'desc']], 
        columnDefs: [
            { orderable: false, targets: [7] } 
        ]
    });

    $('#filtroPractica, #filtroModalidad').on('change', function () {
        var colIndex = $(this).data('col');  
        var value = $(this).val();            

        if (value) {

            tabla.column(colIndex).search('^' + value + '$', true, false).draw();
        } else {
     
            tabla.column(colIndex).search('').draw();
        }
    });

    const formEditar = document.getElementById('formEditarEmpleo');
    if (!formEditar) return;

    formEditar.addEventListener('submit', async function (e) {
        e.preventDefault();

        const idEmpleo = document.getElementById('IdEmpleoEditar');
        const nombre = document.getElementById('NombreEmpleoEditar');
        const idModalidad = document.getElementById('IdModalidadEmpleoEditar');
        const descripcion = document.getElementById('DescripcionEmpleoEditar');
        const requisitos = document.getElementById('RequisitosEmpleoEditar');
        const fechaLimite = document.getElementById('FechaLimiteEmpleoEditar');
        const provincia = document.getElementById('ProvinciaEmpleoEditar');
        const canton = document.getElementById('CantonEmpleoEditar');
        const distrito = document.getElementById('DistritoEmpleoEditar');
        const direccionExacta = document.getElementById('DireccionBolsaEmpleoEditar');
        const areaAfin = document.getElementById('AreaAfinEmpleoEditar');

        // Validaciones básicas
        if (!nombre.value.trim()) {
            Swal.fire('Campo requerido', 'Por favor indique el nombre de la empresa.', 'warning');
            nombre.focus();
            return;
        }

        if (!idModalidad.value) {
            Swal.fire('Campo requerido', 'Por favor seleccione la modalidad del empleo.', 'warning');
            idModalidad.focus();
            return;
        }

        if (!descripcion.value.trim()) {
            Swal.fire('Campo requerido', 'Por favor indique una descripción del empleo.', 'warning');
            descripcion.focus();
            return;
        }

        if (!requisitos.value.trim()) {
            Swal.fire('Campo requerido', 'Por favor indique los requisitos.', 'warning');
            requisitos.focus();
            return;
        }

        if (!fechaLimite.value) {
            Swal.fire('Campo requerido', 'Por favor seleccione la fecha límite de aplicación.', 'warning');
            fechaLimite.focus();
            return;
        }

        const hoy = new Date();
        hoy.setHours(0, 0, 0, 0);
        const fechaIngresada = new Date(fechaLimite.value);
        if (fechaIngresada < hoy) {
            Swal.fire('Fecha inválida', 'La fecha límite no puede ser anterior a hoy.', 'warning');
            fechaLimite.focus();
            return;
        }

        if (!provincia.value) {
            Swal.fire('Campo requerido', 'Por favor seleccione una provincia.', 'warning');
            provincia.focus();
            return;
        }

        if (!canton.value) {
            Swal.fire('Campo requerido', 'Por favor seleccione un cantón.', 'warning');
            canton.focus();
            return;
        }

        if (!distrito.value) {
            Swal.fire('Campo requerido', 'Por favor seleccione un distrito.', 'warning');
            distrito.focus();
            return;
        }

        if (!direccionExacta.value.trim()) {
            Swal.fire('Campo requerido', 'Por favor indique la dirección exacta.', 'warning');
            direccionExacta.focus();
            return;
        }

        if (!areaAfin.value.trim()) {
            Swal.fire('Campo requerido', 'Por favor indique el área afín.', 'warning');
            areaAfin.focus();
            return;
        }

        const formData = new FormData(formEditar);

        try {
            Swal.fire({
                title: 'Guardando cambios...',
                text: 'Por favor espere.',
                allowOutsideClick: false,
                didOpen: () => Swal.showLoading()
            });

            const response = await fetch('/Egresados/EditarEmpleo', {
                method: 'POST',
                body: formData
            });

            const result = await response.json();

            if (result.ok) {
                Swal.fire({
                    icon: 'success',
                    title: 'Empleo actualizado',
                    text: result.msg || 'Los cambios se han guardado correctamente.',
                }).then(() => {
                    const modal = document.getElementById('modalEditarEmpleo');
                    const modalInstance = bootstrap.Modal.getInstance(modal);
                    modalInstance.hide();
                    location.reload();
                });
            } else {
                Swal.fire('Error', result.msg || 'Ocurrió un error al guardar los cambios.', 'error');
            }

        } catch (error) {
            console.error(error);
            Swal.fire('Error', 'Error al comunicarse con el servidor.', 'error');
        }

    });

    document.addEventListener('click', function (e) {
        const btn = e.target.closest('.btn-editar-empleo');
        if (!btn) return;

        const IdDireccion = btn.getAttribute('data-IdDireccion');
        const idEmpleo = btn.getAttribute('data-id');
        const empresa = btn.getAttribute('data-empresa') || '';
        const descripcion = btn.getAttribute('data-descripcion') || '';
        const requisitos = btn.getAttribute('data-requisitos') || '';
        const idModalidad = btn.getAttribute('data-idmodalidad') || '';
        const fechaLimite = btn.getAttribute('data-fechalimite') || '';
        const areaAfin = btn.getAttribute('data-area') || '';
        const direccion = btn.getAttribute('data-direccion') || '';
        const provincia = btn.getAttribute('data-provincia') || '';
        const canton = btn.getAttribute('data-canton') || '';
        const distrito = btn.getAttribute('data-distrito') || '';
        const nombrePuesto = btn.getAttribute('data-nombrepuesto') || '';

        const IdDireccionInput = document.getElementById('IdDireccionEditar');
        const idInput = document.getElementById('IdEmpleoEditar');
        const nombreInput = document.getElementById('NombreEmpleoEditar');
        const modalidadSelect = document.getElementById('IdModalidadEmpleoEditar');
        const descripcionInput = document.getElementById('DescripcionEmpleoEditar');
        const requisitosInput = document.getElementById('RequisitosEmpleoEditar');
        const fechaLimiteInput = document.getElementById('FechaLimiteEmpleoEditar');
        const provinciaSelect = document.getElementById('ProvinciaEmpleoEditar');
        const cantonSelect = document.getElementById('CantonEmpleoEditar');
        const distritoSelect = document.getElementById('DistritoEmpleoEditar');
        const direccionInput = document.getElementById('DireccionBolsaEmpleoEditar');
        const areaAfinInput = document.getElementById('AreaAfinEmpleoEditar');
        const nombrePuestoInput = document.getElementById('NombrePuestoEmpleoEditar');
     
        if (idInput) idInput.value = idEmpleo;
        if (nombreInput) nombreInput.value = empresa;
        if (descripcionInput) descripcionInput.value = descripcion;
        if (requisitosInput) requisitosInput.value = requisitos;
        if (fechaLimiteInput) fechaLimiteInput.value = fechaLimite;
        if (direccionInput) direccionInput.value = direccion;
        if (areaAfinInput) areaAfinInput.value = areaAfin;
        if (IdDireccionInput) IdDireccionInput.value = IdDireccion;
        if (nombrePuestoInput) nombrePuestoInput.value = nombrePuesto;

        if (modalidadSelect) {
            modalidadSelect.value = idModalidad;
        }

   
        if (provinciaSelect && cantonSelect && distritoSelect) {
            provinciaSelect.value = provincia || '';

            cantonSelect.innerHTML = '<option value="">Seleccione un cantón</option>';
            distritoSelect.innerHTML = '<option value="">Seleccione un distrito</option>';

            if (provincia) {
       
                cargarCantones(provinciaSelect);

            
                setTimeout(() => {
                    cantonSelect.value = canton || '';

                    if (cantonSelect.value) {
                        cargarDistritos(cantonSelect);

                        setTimeout(() => {
                            distritoSelect.value = distrito || '';
                        }, 80);
                    }
                }, 80);
            }
        }

        const modalEditar = document.getElementById('modalEditarEmpleo');
        const modalInstance = new bootstrap.Modal(modalEditar);
        modalInstance.show();
    });

    const form = document.getElementById('formCrearEmpleo');
    if (!form) return; 

    form.addEventListener('submit', async function (e) {
        e.preventDefault();

      
        const nombre = document.getElementById('NombreEmpleo');
        const idModalidad = document.getElementById('IdModalidadEmpleo');
        const descripcion = document.getElementById('DescripcionEmpleo');
        const requisitos = document.getElementById('RequisitosEmpleo');
        const fechaLimite = document.getElementById('FechaLimiteEmpleo');
        const provincia = document.getElementById('ProvinciaEmpleo');
        const canton = document.getElementById('CantonEmpleo');
        const distrito = document.getElementById('DistritoEmpleo');
        const direccionExacta = document.getElementById('DireccionBolsaEmpleo');
        const areaAfin = document.getElementById('AreaAfinEmpleo');
        const nombrePuesto = document.getElementById('NombrePuestoEmpleo');
     

        if (!nombre || !nombre.value.trim()) {
            Swal.fire('Campo requerido', 'Por favor indique el nombre de la empresa.', 'warning');
            if (nombre) nombre.focus();
            return;
        }

        if (!nombrePuesto || !nombrePuesto.value.trim()) {
            Swal.fire('Campo requerido', 'Por favor indique el nombre del puesto.', 'warning');
            if (nombrePuesto) nombrePuesto.focus();
            return;
        }

        if (!idModalidad || !idModalidad.value) {
            Swal.fire('Campo requerido', 'Por favor seleccione la modalidad del empleo.', 'warning');
            if (idModalidad) idModalidad.focus();
            return;
        }

        if (!descripcion || !descripcion.value.trim()) {
            Swal.fire('Campo requerido', 'Por favor indique una descripción del empleo.', 'warning');
            if (descripcion) descripcion.focus();
            return;
        }

        if (!requisitos || !requisitos.value.trim()) {
            Swal.fire('Campo requerido', 'Por favor indique los requisitos.', 'warning');
            if (requisitos) requisitos.focus();
            return;
        }

        if (!fechaLimite || !fechaLimite.value) {
            Swal.fire('Campo requerido', 'Por favor seleccione la fecha límite de aplicación.', 'warning');
            if (fechaLimite) fechaLimite.focus();
            return;
        }

        const hoy = new Date();
        hoy.setHours(0, 0, 0, 0);
        const fechaIngresada = new Date(fechaLimite.value);
        if (fechaIngresada < hoy) {
            Swal.fire('Fecha inválida', 'La fecha límite no puede ser anterior a hoy.', 'warning');
            if (fechaLimite) fechaLimite.focus();
            return;
        }

        if (!provincia || !provincia.value) {
            Swal.fire('Campo requerido', 'Por favor seleccione una provincia.', 'warning');
            if (provincia) provincia.focus();
            return;
        }

        if (!canton || !canton.value) {
            Swal.fire('Campo requerido', 'Por favor seleccione un cantón.', 'warning');
            if (canton) canton.focus();
            return;
        }

        if (!distrito || !distrito.value) {
            Swal.fire('Campo requerido', 'Por favor seleccione un distrito.', 'warning');
            if (distrito) distrito.focus();
            return;
        }

        if (!direccionExacta || !direccionExacta.value.trim()) {
            Swal.fire('Campo requerido', 'Por favor indique la dirección exacta.', 'warning');
            if (direccionExacta) direccionExacta.focus();
            return;
        }

        if (!areaAfin || !areaAfin.value.trim()) {
            Swal.fire('Campo requerido', 'Por favor indique el área afín.', 'warning');
            if (areaAfin) areaAfin.focus();
            return;
        }

        const formData = new FormData(form);

        try {
         
            if (typeof urlCrearEmpleo === 'undefined') {
                console.error('urlCrearEmpleo no está definida en la vista.');
                Swal.fire('Error', 'No se encuentra la URL de creación de empleo.', 'error');
                return;
            }

            Swal.fire({
                title: 'Guardando...',
                text: 'Por favor espere.',
                allowOutsideClick: false,
                didOpen: () => {
                    Swal.showLoading();
                }
            });

            const response = await fetch(urlCrearEmpleo, {
                method: 'POST',
                body: formData
            });

            const result = await response.json();

            if (result.ok) {
                Swal.fire({
                    icon: 'success',
                    title: 'Empleo registrado',
                    text: result.msg || 'El empleo se ha creado correctamente.'
                }).then(() => {
                    const modal = document.getElementById('modalCrearEmpleo');
                    if (modal) {
                        const modalInstance = bootstrap.Modal.getInstance(modal) ||
                            new bootstrap.Modal(modal);
                        modalInstance.hide();
                    }

          
                    location.reload();
                });
            } else {
                Swal.fire('Error', result.msg || 'Ocurrió un error al guardar el empleo.', 'error');
            }

        } catch (error) {
            console.error(error);
            Swal.fire('Error', 'Error al comunicarse con el servidor.', 'error');
        }
    });

  
        $(document).on('click', '.btn-eliminar-empleo', function (e) {
            e.preventDefault();

            const idEmpleo = $(this).data('id');
            if (!idEmpleo) {
                console.error('IdEmpleo no encontrado en data-id');
                Swal.fire('Error', 'No se pudo identificar el empleo.', 'error');
                return;
            }

            Swal.fire({
                title: '¿Está seguro?',
                text: 'Esta acción cambiará el estado del empleo (activar/desactivar).',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: 'Sí, continuar',
                cancelButtonText: 'Cancelar'
            }).then(async (result) => {
                if (!result.isConfirmed) {
                    return; // Usuario canceló
                }

                try {
                    // Loading
                    Swal.fire({
                        title: 'Procesando...',
                        text: 'Por favor espere.',
                        allowOutsideClick: false,
                        didOpen: () => {
                            Swal.showLoading();
                        }
                    });

                    const formData = new FormData();
                    formData.append('IdEmpleo', idEmpleo);

                    const response = await fetch('/Egresados/CambiarEstado', {
                        method: 'POST',
                        body: formData
                    });

                    const resultJson = await response.json();

                    if (resultJson.ok) {
                        Swal.fire({
                            icon: 'success',
                            title: 'Listo',
                            text: resultJson.msg || 'Estado actualizado correctamente.'
                        }).then(() => {
                            // Recargar página o refrescar tabla
                            location.reload();
                        });
                    } else {
                        Swal.fire('Error', resultJson.msg || 'Ocurrió un error al actualizar el estado.', 'error');
                    }

                } catch (error) {
                    console.error(error);
                    Swal.fire('Error', 'Error al comunicarse con el servidor.', 'error');
                }
            });
        });

    });

