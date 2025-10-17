(function ($) {
    $(function () {
        const cfg = window.VacantesEgresadoCfg || { urls: {} };

        function escapeHtml(text) {
            if (!text && text !== 0) return '';
            return $('<div>').text(text).html();
        }

        function formatFecha(val) {
            if (!val) return '';
            const d = new Date(val);
            return isNaN(d.getTime()) ? '' : d.toLocaleDateString('es-CR');
        }

        // === Cargar vacantes ===
        filtrarVacantes();
        $("#filtroArea,#filtroModalidad").on('change', filtrarVacantes);

        function filtrarVacantes() {
            var area = $("#filtroArea").val();
            var idModalidad = $("#filtroModalidad").val();

            $.getJSON(cfg.urls.getVacantes, { area, idModalidad })
                .done(function (resp) {
                    if (resp && resp.length > 0) renderVacantes(resp);
                    else $(".vacantes-lista").html(
                        '<div class="vacante-alerta"><strong>Información:</strong> No se encontraron vacantes.</div>'
                    );
                })
                .fail(function () {
                    $(".vacantes-lista").html(
                        '<div class="vacante-alerta"><strong>Error:</strong> No se pudo cargar la información.</div>'
                    );
                });
        }

        function renderVacantes(vacantes) {
            var $c = $(".vacantes-lista").empty();
            if (!vacantes || vacantes.length === 0) {
                $c.append('<div class="vacante-alerta"><strong>Información:</strong> No se encontraron vacantes.</div>');
                return;
            }

            vacantes.forEach(function (v) {
                var card = `
                    <article class="vacante-card" data-area="${escapeHtml(v.AreaAfin || '')}">
                        <header class="vacante-header">
                            <h3 class="vacante-titulo">${escapeHtml(v.NombrePuesto)}</h3>
                            <span class="vacante-empresa">${escapeHtml(v.Empresa || '')}</span>
                        </header>
                        <ul class="vacante-detalles">
                            <li><strong>Requisitos:</strong> ${escapeHtml(v.Requisitos || '')}</li>
                            <li><strong>Modalidad:</strong> ${escapeHtml(v.Modalidad || '')}</li>
                            <li><strong>Área profesional:</strong> ${escapeHtml(v.AreaAfin || '')}</li>
                            <li><strong>Fecha publicación:</strong> ${formatFecha(v.FechaPublicacion)}</li>
                            <li><strong>Fecha límite:</strong> ${formatFecha(v.FechaLimite)}</li>
                        </ul>
                        <div class="text-end">
                            <button class="btn btn-cta btn-detalle" 
                                data-id="${v.IdEmpleo}"
                                data-nombre="${escapeHtml(v.NombrePuesto)}"
                                data-empresa="${escapeHtml(v.Empresa)}"
                                data-descripcion="${escapeHtml(v.Descripcion)}"
                                data-requisitos="${escapeHtml(v.Requisitos)}"
                                data-modalidad="${escapeHtml(v.Modalidad)}"
                                data-area="${escapeHtml(v.AreaAfin)}"
                                data-fecha-publicacion="${v.FechaPublicacion}"
                                data-fecha-limite="${v.FechaLimite}">
                                Ver más
                            </button>
                        </div>
                    </article>`;
                $c.append(card);
            });
        }

        // === Modal Detalle ===
        $(document).on('click', '.btn-detalle', function () {
            var d = $(this).data();
            $('#vis-Nombre').text(d.nombre || '');
            $('#vis-Empresa').text(d.empresa || '');
            $('#vis-Descripcion').text(d.descripcion || '');
            $('#vis-Requisitos').text(d.requisitos || '');
            $('#vis-Modalidad').text(d.modalidad || '');
            $('#vis-Area').text(d.area || '');
            $('#vis-FechaPublicacion').text(formatFecha(d.fechaPublicacion));
            $('#vis-FechaLimite').text(formatFecha(d.fechaLimite));
            $('#modalVisualizar').modal('show');
        });

    });
})(jQuery);
