(function () {
    const BASE = '/AdministracionGeneral';
    const tabs = ['usuarios', 'especialidades', 'secciones'];
    let dtUsuarios, dtEspecialidades, dtSecciones;

    const DT_ES = {
        decimal: ",", thousands: ".",
        emptyTable: "No hay datos disponibles en la tabla",
        info: "Mostrando _START_ a _END_ de _TOTAL_ registros",
        infoEmpty: "Mostrando 0 a 0 de 0 registros",
        infoFiltered: "(filtrado de _MAX_ registros en total)",
        lengthMenu: "Mostrar _MENU_ registros",
        loadingRecords: "Cargando...", processing: "Procesando...",
        search: "Buscar:",
        zeroRecords: "No se encontraron resultados",
        paginate: { first: "Primero", last: "Último", next: "Siguiente", previous: "Anterior" },
        aria: { sortAscending: ": activar para ordenar ascendente", sortDescending: ": activar para ordenar descendente" }
    };

    function show(tab) {
        tabs.forEach(t => {
            const el = document.getElementById(t);
            if (!el) return;
            if (t === tab) el.classList.remove('d-none');
            else el.classList.add('d-none');
        });

        if (tab === 'usuarios') loadUsuarios();
        if (tab === 'especialidades') loadEspecialidades();
        if (tab === 'secciones') loadSecciones();

        const url = new URL(location.href);
        url.searchParams.set('tab', tab);
        history.replaceState({}, '', url);
    }

    document.querySelectorAll('button[data-tab]').forEach(b => {
        b.addEventListener('click', () => show(b.dataset.tab));
    });

    $(document).ajaxError(function (e, xhr) {
        if (xhr && xhr.status === 401) {
            Swal.fire({ icon: 'warning', title: 'No autorizado', text: 'Debes iniciar sesión como Coordinador.' })
                .then(() => location.href = '/Account/Login');
            return;
        }
        Swal.fire({ icon: 'error', title: 'Error', text: 'Ocurrió un problema de comunicación.' });
    });

    // ===== USUARIOS =====
    function loadUsuarios() {
        const rol = $('#filtroRol').val() || '';
        if (dtUsuarios) dtUsuarios.destroy();
        dtUsuarios = $('#tablaUsuarios').DataTable({
            language: DT_ES,
            responsive: true,
            ajax: { url: `${BASE}/Usuarios?rol=${encodeURIComponent(rol)}`, dataSrc: 'data' },
            columns: [
                { data: 'Nombre' },
                { data: 'Cedula' },
                { data: 'Email' },
                { data: 'Rol' },
                { data: 'Estado' },
                {
                    data: null,
                    orderable: false,
                    render: (row) => {
                        if (Number(row.IdEstado) === 2) return 'Inactivo';
                        return `
              <a href="#" class="btn btn-sm btn-editar-rol-usuario"
                 data-id="${row.IdUsuario}" data-nombre="${row.Nombre}"
                 data-cedula="${row.Cedula}" data-email="${row.Email}"
                 title="Editar rol"><i class="bi bi-person-gear"></i></a>
              <a href="#" class="btn btn-sm btn-toggle-estado"
                 data-id="${row.IdUsuario}" data-estado="${row.Estado}" data-nombre="${row.Nombre}"
                 title="${row.Estado === 'Activo' ? 'Desactivar' : 'Activar'}">
                 <i class="bi ${row.IdEstado === 1 ? 'bi-person-slash' : 'bi-person-check'}"></i></a>
            `;
                    }
                },
                { data: 'IdEstado', visible: false, render: v => Number(v) === 1 ? 0 : 1 }
            ],
            order: [[6, 'asc'], [0, 'asc']]
        });
    }
    $('#filtroRol').on('change', () => loadUsuarios());

    $(document).on('click', '.btn-editar-rol-usuario', function (e) {
        e.preventDefault();
        const $t = $(this);
        $('#rolNombre').text($t.data('nombre'));
        $('#rolCedula').text($t.data('cedula'));
        $('#rolEmail').text($t.data('email'));
        $('#usuarioId').val($t.data('id'));
        $('#rol').val('');
        $('#modalEditarRolUsuario').modal('show');
    });

    $('#formEditarRolUsuario').on('submit', function (e) {
        e.preventDefault();
        $.post(`${BASE}/CambiarRolUsuario`, {
            idUsuario: $('#usuarioId').val(),
            rol: $('#rol').val()
        })
            .done(r => {
                Swal.fire({ icon: r.ok ? 'success' : 'error', title: r.ok ? 'Éxito' : 'Error', text: r.msg || '' })
                    .then(() => { if (r.ok) $('#modalEditarRolUsuario').modal('hide'); loadUsuarios(); });
            })
            .fail(() => Swal.fire({ icon: 'error', title: 'Error', text: 'No se pudo actualizar el rol.' }));
    });

    $(document).on('click', '.btn-toggle-estado', function (e) {
        e.preventDefault();
        const $t = $(this);
        const id = $t.data('id');
        const nombre = $t.data('nombre');
        const estadoActual = ($t.data('estado') || '').trim();
        const nuevo = estadoActual === 'Activo' ? 'Inactivo' : 'Activo';
        const verbo = nuevo === 'Inactivo' ? 'desactivar' : 'activar';

        Swal.fire({
            title: `¿Deseas ${verbo} a ${nombre}?`,
            icon: 'question', showCancelButton: true, confirmButtonText: 'Sí', cancelButtonText: 'Cancelar'
        }).then(res => {
            if (!res.isConfirmed) return;
            $.post(`${BASE}/CambiarEstadoUsuario`, { idUsuario: id, nuevoEstado: nuevo })
                .done(r => {
                    Swal.fire({ icon: r.ok ? 'success' : 'error', title: r.ok ? 'Éxito' : 'Error', text: r.msg || '' })
                        .then(() => loadUsuarios());
                })
                .fail(() => Swal.fire({ icon: 'error', title: 'Error', text: 'No se pudo cambiar el estado.' }));
        });
    });

    // ===== ESPECIALIDADES =====
    function loadEspecialidades() {
        if (dtEspecialidades) dtEspecialidades.destroy();
        dtEspecialidades = $('#tablaEspecialidades').DataTable({
            language: DT_ES,
            responsive: true,
            ajax: { url: `${BASE}/Especialidades`, dataSrc: 'data' },
            columns: [
                { data: 'Nombre' },
                {
                    data: null,
                    orderable: false,
                    render: (row) => {
                        if (Number(row.IdEstado) === 2) return 'Inactivo';
                        return `
              <a href="#" class="btn btn-sm btn-editar-especialidad"
                 data-id="${row.IdEspecialidad}" data-nombre="${row.Nombre}"
                 title="Editar"><i class="bi bi-pencil-square"></i></a>
              <a href="#" class="btn btn-sm btn-desactivar-especialidad"
                 data-id="${row.IdEspecialidad}"
                 title="Desactivar"><i class="bi bi-slash-circle"></i></a>
            `;
                    }
                },
                { data: 'IdEstado', visible: false, render: v => Number(v) === 1 ? 0 : 1 }
            ],
            order: [[2, 'asc'], [0, 'asc']]
        });
    }

    $('#formCrearEspecialidad').on('submit', function (e) {
        e.preventDefault();
        $.post(`${BASE}/CrearEspecialidad`, {
            nombre: $('#nombreEspecialidad').val(),
            descripcion: $('#descripcionEspecialidad').val()
        })
            .done(r => {
                Swal.fire({ icon: r.ok ? 'success' : 'error', title: r.ok ? 'Éxito' : 'Error', text: r.msg || '' })
                    .then(() => { if (r.ok) $('#modalCrearEspecialidad').modal('hide'); loadEspecialidades(); });
            })
            .fail(() => Swal.fire({ icon: 'error', title: 'Error', text: 'No se pudo crear la especialidad.' }));
    });

    $(document).on('click', '.btn-editar-especialidad', function (e) {
        e.preventDefault();
        $('#editarIdEspecialidad').val($(this).data('id'));
        $('#editarNombreEspecialidad').val($(this).data('nombre'));
        $('#editarDescripcionEspecialidad').val('');
        $('#modalEditarEspecialidad').modal('show');
    });

    $('#formEditarEspecialidad').on('submit', function (e) {
        e.preventDefault();
        $.post(`${BASE}/EditarEspecialidad`, {
            id: $('#editarIdEspecialidad').val(),
            nombre: $('#editarNombreEspecialidad').val(),
            descripcion: $('#editarDescripcionEspecialidad').val()
        })
            .done(r => {
                Swal.fire({ icon: r.ok ? 'success' : 'error', title: r.ok ? 'Éxito' : 'Error', text: r.msg || '' })
                    .then(() => { if (r.ok) $('#modalEditarEspecialidad').modal('hide'); loadEspecialidades(); });
            })
            .fail(() => Swal.fire({ icon: 'error', title: 'Error', text: 'No se pudo editar la especialidad.' }));
    });

    $(document).on('click', '.btn-desactivar-especialidad', function (e) {
        e.preventDefault();
        const id = $(this).data('id');
        Swal.fire({ title: 'Desactivar especialidad', text: '¿Deseas desactivarla?', icon: 'question', showCancelButton: true, confirmButtonText: 'Sí', cancelButtonText: 'Cancelar' })
            .then(res => {
                if (!res.isConfirmed) return;
                $.post(`${BASE}/DesactivarEspecialidad`, { id })
                    .done(r => {
                        Swal.fire({ icon: r.ok ? 'success' : 'error', title: r.ok ? 'Éxito' : 'Error', text: r.msg || '' })
                            .then(() => loadEspecialidades());
                    })
                    .fail(() => Swal.fire({ icon: 'error', title: 'Error', text: 'No se pudo desactivar la especialidad.' }));
            });
    });

    // ===== SECCIONES =====
    function loadSecciones() {
        if (dtSecciones) dtSecciones.destroy();
        dtSecciones = $('#tablaSecciones').DataTable({
            language: DT_ES,
            responsive: true,
            ajax: { url: `${BASE}/Secciones`, dataSrc: 'data' },
            columns: [
                { data: 'Seccion' },
                {
                    data: null,
                    orderable: false,
                    render: (row) => {
                        if (Number(row.IdEstado) === 2) return 'Inactivo';
                        return `
              <a href="#" class="btn btn-sm btn-editar-seccion"
                 data-id="${row.IdSeccion}" data-nombre="${row.Seccion}"
                 title="Editar"><i class="bi bi-pencil-square"></i></a>
              <a href="#" class="btn btn-sm btn-desactivar-seccion"
                 data-id="${row.IdSeccion}"
                 title="Desactivar"><i class="bi bi-slash-circle"></i></a>
            `;
                    }
                },
                { data: 'IdEstado', visible: false, render: v => Number(v) === 1 ? 0 : 1 }
            ],
            order: [[2, 'asc'], [0, 'asc']]
        });
    }

    $('#formCrearSeccion').on('submit', function (e) {
        e.preventDefault();
        $.post(`${BASE}/CrearSeccion`, {
            nombreSeccion: $('#nombreSeccion').val(),
            descripcionSeccion: $('#descripcionSeccion').val()
        })
            .done(r => {
                Swal.fire({ icon: r.ok ? 'success' : 'error', title: r.ok ? 'Éxito' : 'Error', text: r.msg || '' })
                    .then(() => { if (r.ok) $('#modalCrearSeccion').modal('hide'); loadSecciones(); });
            })
            .fail(() => Swal.fire({ icon: 'error', title: 'Error', text: 'No se pudo crear la sección.' }));
    });

    $(document).on('click', '.btn-editar-seccion', function (e) {
        e.preventDefault();
        $('#editarIdSeccion').val($(this).data('id'));
        $('#editarNombreSeccion').val($(this).data('nombre'));
        $('#editarDescripcionSeccion').val('');
        $('#modalEditarSeccion').modal('show');
    });

    $('#formEditarSeccion').on('submit', function (e) {
        e.preventDefault();
        $.post(`${BASE}/EditarSeccion`, {
            id: $('#editarIdSeccion').val(),
            nombreSeccion: $('#editarNombreSeccion').val(),
            descripcionSeccion: $('#editarDescripcionSeccion').val()
        })
            .done(r => {
                Swal.fire({ icon: r.ok ? 'success' : 'error', title: r.ok ? 'Éxito' : 'Error', text: r.msg || '' })
                    .then(() => { if (r.ok) $('#modalEditarSeccion').modal('hide'); loadSecciones(); });
            })
            .fail(() => Swal.fire({ icon: 'error', title: 'Error', text: 'No se pudo editar la sección.' }));
    });

    $(document).on('click', '.btn-desactivar-seccion', function (e) {
        e.preventDefault();
        const id = $(this).data('id');
        Swal.fire({ title: 'Desactivar sección', text: '¿Deseas desactivarla?', icon: 'question', showCancelButton: true, confirmButtonText: 'Sí', cancelButtonText: 'Cancelar' })
            .then(res => {
                if (!res.isConfirmed) return;
                $.post(`${BASE}/DesactivarSeccion`, { id })
                    .done(r => {
                        Swal.fire({ icon: r.ok ? 'success' : 'error', title: r.ok ? 'Éxito' : 'Error', text: r.msg || '' })
                            .then(() => loadSecciones());
                    })
                    .fail(() => Swal.fire({ icon: 'error', title: 'Error', text: 'No se pudo desactivar la sección.' }));
            });
    });

    const inicial = window.__TAB_INICIAL__ || (new URLSearchParams(location.search).get('tab') || 'usuarios');
    show(inicial);
})();
