$(document).ready(function () {
    if ($('#miTabla').length) {
        $('#miTabla').DataTable({
            responsive: true,
            autoWidth: false, // evita que DataTables fije anchos rígidos
            language: {
                url: '//cdn.datatables.net/plug-ins/1.13.6/i18n/es-ES.json'
            }
        });
    }
});