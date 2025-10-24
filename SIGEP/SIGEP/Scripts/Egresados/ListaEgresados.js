$(document).ready(function () {
    inicializarTabla();
    cargarEgresados();

    // Filtrado automático (sin botón)
    $("#ddlEspecialidad, #ddlAnio").on('change', function () {
        cargarEgresados();
    });
});

function inicializarTabla() {
    $("#miTabla").DataTable({
        dom: '<"d-flex justify-content-between mb-2"Bf>rt<"d-flex justify-content-between mt-3"lip>',
        buttons: [
            {
                extend: 'excelHtml5',
                text: '<i class="fas fa-file-excel"></i> Exportar a Excel',
                className: 'btn btn-verde-personalizado btn-sm'
            },
            {
                extend: 'pdfHtml5',
                text: '<i class="fas fa-file-pdf"></i> Exportar a PDF',
                className: 'btn btn-verde-personalizado btn-sm',
                orientation: 'landscape',
                pageSize: 'A4'
            },
            {
                extend: 'print',
                text: '<i class="fas fa-print"></i> Imprimir',
                className: 'btn btn-verde-personalizado btn-sm'
            }
        ],
        responsive: true,
        destroy: true,
        autoWidth: false,
        language: {
            url: '//cdn.datatables.net/plug-ins/1.13.6/i18n/es-ES.json'
        },
        columns: [
            { data: 'NombreCompleto' },
            { data: 'Generacion' },
            { data: 'Especialidad' },
            { data: 'Correo' },
            { data: 'Telefono' }
        ]
    });
}

function cargarEgresados() {
    var idEspecialidad = $("#ddlEspecialidad").val() || 0;
    var anio = $("#ddlAnio").val() || 0;

    var tabla = $("#miTabla").DataTable();
    tabla.clear().draw();

    $.ajax({
        url: '/Egresados/ObtenerEgresados',
        type: 'GET',
        data: { idEspecialidad: idEspecialidad, anio: anio },
        beforeSend: function () {
            $("#miTabla tbody").html(
                '<tr><td colspan="5" class="text-center text-muted">Cargando datos...</td></tr>'
            );
        },
        success: function (data) {
            tabla.clear();
            if (data && data.length > 0) {
                tabla.rows.add(data);
            } else {
                $("#miTabla tbody").html(
                    '<tr><td colspan="5" class="text-center text-muted">No se encontraron resultados.</td></tr>'
                );
            }
            tabla.draw();
        },
        error: function () {
            Swal.fire('Error', 'No se pudieron cargar los datos de egresados.', 'error');
        }
    });
}

