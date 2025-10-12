$(document).ready(function () {
    inicializarTabla();
    cargarEgresados();

    $("#btnBuscar").click(function () {
        cargarEgresados();
    });
});

function inicializarTabla() {
    $("#tblEgresados").DataTable({
        dom: 'Bfrtip',
        buttons: [
            {
                extend: 'excelHtml5',
                text: '<i class="fa fa-file-excel"></i> Exportar Excel',
                className: 'btn btn-success btn-sm'
            },
            {
                extend: 'pdfHtml5',
                text: '<i class="fa fa-file-pdf"></i> Exportar PDF',
                className: 'btn btn-danger btn-sm',
                orientation: 'landscape',
                pageSize: 'A4'
            },
            {
                extend: 'print',
                text: '<i class="fa fa-print"></i> Imprimir',
                className: 'btn btn-primary btn-sm'
            }
        ],
        responsive: true,
        destroy: true,
        language: {
            url: '//cdn.datatables.net/plug-ins/1.13.4/i18n/es-ES.json'
        },
        columns: [
            { data: 'NombreCompleto' },
            { data: 'Generacion' },
            { data: 'Especialidad' },
            { data: 'Correo' },
            { data: 'Telefono' },
            {
                data: 'IdUsuario',
                className: 'text-center',
                render: function (data) {
                    return `
                        <button class='btn btn-info btn-sm' onclick='verDetalle(${data})'>
                            <i class='fa fa-eye'></i> Ver Detalle
                        </button>`;
                }
            }
        ]
    });
}

function cargarEgresados() {
    var especialidad = $("#ddlEspecialidad").val();
    var anio = $("#ddlAnio").val();

    $.ajax({
        url: '/Egresado/ObtenerEgresados',
        type: 'GET',
        data: { especialidadId: especialidad, anio: anio },
        success: function (data) {
            var table = $("#tblEgresados").DataTable();
            table.clear();
            table.rows.add(data);
            table.draw();
        },
        error: function () {
            alert("Error al cargar los datos de los egresados.");
        }
    });
}

function verDetalle(id) {
    $.ajax({
        url: '/Egresado/Detalle',
        type: 'GET',
        data: { id: id },
        success: function (html) {
            $("#detalleContenido").html(html);
            $("#modalDetalleEgresado").modal("show");
        },
        error: function () {
            alert("No se pudo cargar el detalle del egresado.");
        }
    });
}