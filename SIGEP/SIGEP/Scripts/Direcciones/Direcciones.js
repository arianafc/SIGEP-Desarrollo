// ~/Scripts/direcciones.js

let direccionesData = null;

// Carga el JSON al inicio
async function cargarJSONDirecciones() {
    try {
        const response = await fetch('/Content/Direcciones/provincias_cantones_distritos_costa_rica.json');

        if (!response.ok) {
            throw new Error('No se pudo cargar el archivo JSON');
        }

        direccionesData = await response.json();
        console.log('JSON de direcciones cargado correctamente');
    } catch (error) {
        console.error('Error cargando direcciones:', error);
        if (window.Swal) {
            Swal.fire('Error', 'Error al cargar las direcciones. Por favor, recarga la página.', 'error');
        }
    }
}

function obtenerGrupoSelects(select) {
    if (!select) return {};

 
    let contenedor = select.closest('form');
    if (!contenedor) {
   
        contenedor = select.closest('.modal-content') ||
            select.closest('.row') ||
            document;
    }

    const provinciaSelect = contenedor.querySelector('.ddl-provincia');
    const cantonSelect = contenedor.querySelector('.ddl-canton');
    const distritoSelect = contenedor.querySelector('.ddl-distrito');

    return { provinciaSelect, cantonSelect, distritoSelect };
}

/**
 * Cargar cantones para la provincia seleccionada en el grupo de selects
 * @param {HTMLSelectElement} provinciaSelectEl - el select de provincia (this desde onchange)
 */
function cargarCantones(provinciaSelectEl) {
    if (!direccionesData) {
        console.error('JSON de direcciones no está cargado');
        return;
    }

    const { provinciaSelect, cantonSelect, distritoSelect } = obtenerGrupoSelects(provinciaSelectEl);

    if (!provinciaSelect || !cantonSelect || !distritoSelect) {
        console.warn('No se encontraron los selects de provincia/cantón/distrito en este grupo');
        return;
    }

    const provinciaNombre = provinciaSelect.value;
    console.log('Provincia seleccionada:', provinciaNombre);

   
    cantonSelect.innerHTML = '<option value="">Seleccione un cantón</option>';
    distritoSelect.innerHTML = '<option value="">Seleccione un distrito</option>';

    if (!provinciaNombre) {
        return;
    }

    const provincias = direccionesData.provincias || direccionesData.Provincias || direccionesData; 
    const provinciaKey = Object.keys(provincias).find(
        key => provincias[key].nombre === provinciaNombre
    );

    if (!provinciaKey) {
        console.error('Provincia no encontrada en JSON:', provinciaNombre);
        return;
    }

    const provincia = provincias[provinciaKey];

    const cantones = Object.keys(provincia.cantones).map(key => ({
        key: key,
        nombre: provincia.cantones[key].nombre
    })).sort((a, b) => a.nombre.localeCompare(b.nombre));

    console.log('Cantones encontrados:', cantones);

    cantones.forEach(canton => {
        const option = document.createElement('option');
        option.value = canton.nombre;
        option.textContent = canton.nombre;
        cantonSelect.appendChild(option);
    });
}

/**
 * Cargar distritos para el cantón seleccionado en el grupo de selects
 * @param {HTMLSelectElement} cantonSelectEl - el select de cantón (this desde onchange)
 */
function cargarDistritos(cantonSelectEl) {
    if (!direccionesData) {
        console.error('JSON de direcciones no está cargado');
        return;
    }

    const { provinciaSelect, cantonSelect, distritoSelect } = obtenerGrupoSelects(cantonSelectEl);

    if (!provinciaSelect || !cantonSelect || !distritoSelect) {
        console.warn('No se encontraron los selects de provincia/cantón/distrito en este grupo');
        return;
    }

    const provinciaNombre = provinciaSelect.value;
    const cantonNombre = cantonSelect.value;

    console.log('Cargar distritos para:', provinciaNombre, cantonNombre);

    distritoSelect.innerHTML = '<option value="">Seleccione un distrito</option>';

    if (!provinciaNombre || !cantonNombre) {
        return;
    }

    const provincias = direccionesData.provincias || direccionesData.Provincias || direccionesData;

    const provinciaKey = Object.keys(provincias).find(
        key => provincias[key].nombre === provinciaNombre
    );

    if (!provinciaKey) {
        console.error('Provincia no encontrada:', provinciaNombre);
        return;
    }

    const provincia = provincias[provinciaKey];

    const cantonKey = Object.keys(provincia.cantones).find(
        key => provincia.cantones[key].nombre === cantonNombre
    );

    if (!cantonKey) {
        console.error('Cantón no encontrado:', cantonNombre);
        return;
    }

    const canton = provincia.cantones[cantonKey];

    const distritos = Object.keys(canton.distritos).map(key => ({
        key: key,
        nombre: canton.distritos[key]
    })).sort((a, b) => a.nombre.localeCompare(b.nombre));

    console.log('Distritos encontrados:', distritos);

    distritos.forEach(distrito => {
        const option = document.createElement('option');
        option.value = distrito.nombre;
        option.textContent = distrito.nombre;
        distritoSelect.appendChild(option);
    });
}

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', cargarJSONDirecciones);
} else {
    cargarJSONDirecciones();
}

