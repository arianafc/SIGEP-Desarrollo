/**
 * Navega a visualización guardando datos en sessionStorage
 */
function irAVisualizacion(idVacante, idUsuario) {
    sessionStorage.setItem('visuPostulacion_idVacante', idVacante);
    sessionStorage.setItem('visuPostulacion_idUsuario', idUsuario);

    // Navegar CON parámetros (el controller los necesita)
    window.location.href = '/Practicas/VisualizacionPostulacion?idVacante=' + idVacante + '&idUsuario=' + idUsuario;
}

/**
 * Obtener datos de sessionStorage
 */
function obtenerDatosVisualizacion() {
    var idVacante = sessionStorage.getItem('visuPostulacion_idVacante');
    var idUsuario = sessionStorage.getItem('visuPostulacion_idUsuario');

    if (idVacante && idUsuario) {
        return {
            idVacante: parseInt(idVacante),
            idUsuario: parseInt(idUsuario)
        };
    }
    return null;
}

/**
 * Limpiar datos de sessionStorage
 */
function limpiarDatosVisualizacion() {
    sessionStorage.removeItem('visuPostulacion_idVacante');
    sessionStorage.removeItem('visuPostulacion_idUsuario');
}

// ============================================
// EVENTO DELEGADO GLOBAL
// ============================================
if (typeof jQuery !== 'undefined') {
    $(document).on('click', '.link-visualizacion', function (e) {
        e.preventDefault();
        var idVacante = $(this).data('idvacante');
        var idUsuario = $(this).data('idusuario');

        if (!idVacante || !idUsuario) {
            console.error('Faltan parámetros: idVacante o idUsuario');
            return;
        }

        irAVisualizacion(idVacante, idUsuario);
    });
}

// ============================================
// LIMPIAR AL SALIR DE LA VISTA
// ============================================
(function () {
    function limpiarSiNoEstamosEnVisualizacion() {
        var pathname = window.location.pathname.toLowerCase();
        if (!pathname.includes('visualizacionpostulacion')) {
            limpiarDatosVisualizacion();
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', limpiarSiNoEstamosEnVisualizacion);
    } else {
        limpiarSiNoEstamosEnVisualizacion();
    }
})();