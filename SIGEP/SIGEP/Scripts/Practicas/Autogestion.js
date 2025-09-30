$(document).ready(function () {
    // Variable global para almacenar los datos de Costa Rica
    let datosCR = null;

    // Cargar datos de Costa Rica al inicializar
    cargarDatosCR();

    async function cargarDatosCR() {
        try {
            const response = await fetch('https://gist.githubusercontent.com/josuenoel/80daca657b71bc1cfd95a4e27d547abe/raw/');

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            datosCR = await response.json();

        } catch (error) {
            console.error('Error al cargar datos de Costa Rica:', error);
            // Fallback con datos estáticos básicos
            datosCR = {
                "provincias": {
                    "1": { "nombre": "San José", "cantones": {} },
                    "2": { "nombre": "Alajuela", "cantones": {} },
                    "3": { "nombre": "Cartago", "cantones": {} },
                    "4": { "nombre": "Heredia", "cantones": {} },
                    "5": { "nombre": "Guanacaste", "cantones": {} },
                    "6": { "nombre": "Puntarenas", "cantones": {} },
                    "7": { "nombre": "Limón", "cantones": {} }
                }
            };
        }
    }

    // Función para registrar práctica autogestionada
    window.registrarPostulacion = function () {
        const form = document.getElementById("formAutogestion");

        const camposRequeridos = [
            { selector: '[name="empresa"]', nombre: 'Nombre de la Empresa' },
            { selector: '[name="sector"]', nombre: 'Sector' },
            { selector: '[name="encargado"]', nombre: 'Nombre del Encargado' },
            { selector: '[name="puesto"]', nombre: 'Puesto' },
            { selector: '[name="correo"]', nombre: 'Correo Electrónico' },
            { selector: '[name="telefono"]', nombre: 'Teléfono' },
            { selector: '#provincia', nombre: 'Provincia' },
            { selector: '#canton', nombre: 'Cantón' },
            { selector: '#distrito', nombre: 'Distrito' },
            { selector: '[name="direccion"]', nombre: 'Dirección Exacta' },
            { selector: '[name="descripcion"]', nombre: 'Descripción de Tareas' },
            { selector: '[name="duracion"]', nombre: 'Duración' },
            { selector: '[name="modalidad"]', nombre: 'Modalidad' }
        ];

        let camposVaciosDetalle = [];

        camposRequeridos.forEach(campo => {
            const elemento = form.querySelector(campo.selector);
            if (!elemento) {
                console.error(`Campo no encontrado: ${campo.selector}`);
                camposVaciosDetalle.push(`${campo.nombre} (no encontrado)`);
                return;
            }

            const valor = elemento.value ? elemento.value.trim() : "";
        });

        if (camposVaciosDetalle.length > 0) {
            console.log("Campos vacíos:", camposVaciosDetalle);
            Swal.fire({
                icon: 'warning',
                title: 'Campos incompletos',
                text: `Los siguientes campos son obligatorios: ${camposVaciosDetalle.join(', ')}`,
                confirmButtonColor: '#2D594D'
            });
            return;
        }

        // Validar que se haya seleccionado un distrito
        const distritoValue = document.getElementById('distrito').value;
        if (!distritoValue || distritoValue === "") {
            Swal.fire({
                icon: 'warning',
                title: 'Distrito requerido',
                text: 'Debe seleccionar un distrito',
                confirmButtonColor: '#2D594D'
            });
            return;
        }

        // Preparar datos para envío - usando nombres en lugar de IDs
        const data = {
            NombreEmpresa: form.querySelector('[name="empresa"]').value,
            Sector: form.querySelector('[name="sector"]').value,
            NombreEncargado: form.querySelector('[name="encargado"]').value,
            Puesto: form.querySelector('[name="puesto"]').value,
            Correo: form.querySelector('[name="correo"]').value,
            Telefono: form.querySelector('[name="telefono"]').value,
            Provincia: document.getElementById('provincia').value,
            Canton: document.getElementById('canton').value,
            Distrito: distritoValue,
            DireccionExacta: form.querySelector('[name="direccion"]').value,
            DescripcionTareas: form.querySelector('[name="descripcion"]').value,
            Duracion: form.querySelector('[name="duracion"]').value,
            IdModalidad: parseInt(form.querySelector('[name="modalidad"]').value)
        };

        // Mostrar loading
        Swal.fire({
            title: 'Registrando práctica...',
            allowOutsideClick: false,
            didOpen: () => {
                Swal.showLoading();
            }
        });

        // Enviar AJAX
        $.ajax({
            url: '/Practicas/RegistrarAutogestion',
            type: 'POST',
            data: data,
            success: function (response) {
                if (response.success) {
                    Swal.fire({
                        icon: 'success',
                        title: 'Práctica registrada',
                        text: response.message,
                        confirmButtonColor: '#2D594D'
                    }).then(() => {
                        // Cerrar modal y recargar página
                        const modal = bootstrap.Modal.getInstance(document.getElementById('modalAutogestion'));
                        if (modal) {
                            modal.hide();
                        }
                        window.location.reload();
                    });
                } else {
                    Swal.fire({
                        icon: 'error',
                        title: 'Error',
                        text: response.message,
                        confirmButtonColor: '#2D594D'
                    });
                }
            },
            error: function (xhr, status, error) {
                console.error('Error:', error);
                Swal.fire({
                    icon: 'error',
                    title: 'Error de conexión',
                    text: 'No se pudo registrar la práctica. Inténtalo de nuevo.',
                    confirmButtonColor: '#2D594D'
                });
            }
        });
    };

    // Función para cargar cantones - hacer global
    window.cargarCantones = function () {
        const provinciaSeleccionada = document.getElementById("provincia").value;
        const cantonSelect = document.getElementById("canton");
        const distritoSelect = document.getElementById("distrito");

        console.log("Cargando cantones para provincia:", provinciaSeleccionada);

        // Limpiar selects
        cantonSelect.innerHTML = '<option value="">Seleccione un cantón</option>';
        distritoSelect.innerHTML = '<option value="">Seleccione un distrito</option>';

        if (!provinciaSeleccionada || !datosCR) {
            console.log("No hay provincia seleccionada o datos no cargados");
            return;
        }

        try {
            // Buscar la provincia por nombre
            let provinciaId = null;
            for (const [id, provincia] of Object.entries(datosCR.provincias)) {
                if (provincia.nombre === provinciaSeleccionada) {
                    provinciaId = id;
                    break;
                }
            }

            if (!provinciaId) {
                console.log("Provincia no encontrada:", provinciaSeleccionada);
                return;
            }

            const cantones = datosCR.provincias[provinciaId].cantones;
            console.log("Cantones encontrados:", Object.keys(cantones).length);

            // Agregar cantones al select
            for (const [cantonId, canton] of Object.entries(cantones)) {
                const option = document.createElement("option");
                option.value = canton.nombre;
                option.textContent = canton.nombre;
                cantonSelect.appendChild(option);
            }

            console.log(`${Object.keys(cantones).length} cantones cargados`);

        } catch (error) {
            console.error('Error al procesar cantones:', error);
            cantonSelect.innerHTML = '<option value="">Error al cargar cantones</option>';
        }
    };

    // Función para cargar distritos - hacer global
    window.cargarDistritos = function () {
        const provinciaSeleccionada = document.getElementById("provincia").value;
        const cantonSeleccionado = document.getElementById("canton").value;
        const distritoSelect = document.getElementById("distrito");

        console.log("Cargando distritos para cantón:", cantonSeleccionado);

        distritoSelect.innerHTML = '<option value="">Seleccione un distrito</option>';

        if (!cantonSeleccionado || !datosCR || !provinciaSeleccionada) {
            console.log("No hay cantón seleccionado o datos no cargados");
            return;
        }

        try {
            // Buscar la provincia por nombre
            let provinciaId = null;
            for (const [id, provincia] of Object.entries(datosCR.provincias)) {
                if (provincia.nombre === provinciaSeleccionada) {
                    provinciaId = id;
                    break;
                }
            }

            if (!provinciaId) {
                console.log("Provincia no encontrada");
                return;
            }

            // Buscar el cantón por nombre
            let cantonData = null;
            for (const [cantonId, canton] of Object.entries(datosCR.provincias[provinciaId].cantones)) {
                if (canton.nombre === cantonSeleccionado) {
                    cantonData = canton;
                    break;
                }
            }

            if (!cantonData || !cantonData.distritos) {
                console.log("Cantón no encontrado o sin distritos");
                return;
            }

            const distritos = cantonData.distritos;
            console.log("Distritos encontrados:", Object.keys(distritos).length);

            // Agregar distritos al select
            for (const [distritoId, distritoNombre] of Object.entries(distritos)) {
                const option = document.createElement("option");
                option.value = distritoNombre;
                option.textContent = distritoNombre;
                distritoSelect.appendChild(option);
            }

            console.log(`${Object.keys(distritos).length} distritos cargados`);

        } catch (error) {
            console.error('Error al procesar distritos:', error);
            distritoSelect.innerHTML = '<option value="">Error al cargar distritos</option>';
        }
    };
});