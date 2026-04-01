$(document).ready(function () {
    inicializarTabla();
    cargarEgresados();

    $("#ddlEspecialidad, #ddlAnio").on('change', function () {
        cargarEgresados();
    });

    // Click en ícono de expand
    $('#miTabla tbody').on('click', 'td.dt-control', function () {
        const tabla = $('#miTabla').DataTable();
        const tr = $(this).closest('tr');
        const row = tabla.row(tr);

        if (row.child.isShown()) {
            row.child.hide();
            tr.removeClass('shown');
        } else {
            row.child(formatDetalle(row.data())).show();
            tr.addClass('shown');
        }
    });
});

function formatDetalle(d) {
    const formacion = (d.Formacion && d.Formacion.length > 0)
        ? d.Formacion.map(f => `
            <tr>
                <td>${f.Carrera || '—'}</td>
                <td>${f.Titulo || '—'}</td>
                <td>${f.AnnoGraduacion || '—'}</td>
            </tr>`).join('')
        : '<tr><td colspan="3" class="text-muted">Sin registros</td></tr>';

    const laboral = (d.Laboral && d.Laboral.length > 0)
        ? d.Laboral.map(l => `
            <tr>
                <td>${l.EmpresaActual || '—'}</td>
                <td>${l.PuestoActual || '—'}</td>
            </tr>`).join('')
        : '<tr><td colspan="2" class="text-muted">Sin registros</td></tr>';

    return `
        <div class="detalle-egresado p-3">
            <div class="row">
                <div class="col-md-6">
                    <h6 class="text-verde mb-2"><i class="fas fa-graduation-cap me-2"></i>Formación Académica</h6>
                    <table class="table table-sm table-bordered">
                        <thead class="table-verde">
                            <tr>
                                <th>Carrera</th>
                                <th>Título</th>
                                <th>Año Graduación</th>
                            </tr>
                        </thead>
                        <tbody>${formacion}</tbody>
                    </table>
                </div>
                <div class="col-md-6">
                    <h6 class="text-verde mb-2"><i class="fas fa-briefcase me-2"></i>Información Laboral</h6>
                    <table class="table table-sm table-bordered">
                        <thead class="table-verde">
                            <tr>
                                <th>Empresa Actual</th>
                                <th>Puesto Actual</th>
                            </tr>
                        </thead>
                        <tbody>${laboral}</tbody>
                    </table>
                </div>
            </div>
        </div>`;
}

function inicializarTabla() {
    $("#miTabla").DataTable({
        dom: '<"d-flex justify-content-between align-items-center mb-3"Bf>rt<"d-flex justify-content-between align-items-center mt-3"lip>',
        buttons: [
            {
                extend: 'excelHtml5',
                text: '<i class="fas fa-file-excel"></i> Exportar a Excel',
                className: 'btn btn-verde-personalizado btn-sm'
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
            {
                className: 'dt-control',
                orderable: false,
                data: null,
                defaultContent: '<i class="fas fa-plus-circle text-verde" style="cursor:pointer;"></i>'
            },
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
                '<tr><td colspan="6" class="text-center text-muted">Cargando datos...</td></tr>'
            );
        },
        success: function (data) {
            tabla.clear();
            if (data && data.length > 0) {
                tabla.rows.add(data);
            } else {
                $("#miTabla tbody").html(
                    '<tr><td colspan="6" class="text-center text-muted">No se encontraron resultados.</td></tr>'
                );
            }
            tabla.draw();
        },
        error: function () {
            Swal.fire('Error', 'No se pudieron cargar los datos de egresados.', 'error');
        }
    });
}