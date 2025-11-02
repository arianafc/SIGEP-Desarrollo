$(function () {

    // Inicializar datepicker
    $("#FechaNacimiento").datepicker({
        dateFormat: "yy-mm-dd",
        changeMonth: true,
        changeYear: true,
        yearRange: "1900:" + new Date().getFullYear(), // Desde 1900 hasta el año actual
        defaultDate: "-17Y" // Fecha por defecto: hace 17 años
    });

    // Toggle de contraseña
    document.querySelectorAll('.toggle-password').forEach(function (toggle) {
        toggle.addEventListener('click', function () {
            const input = this.previousElementSibling;
            if (input && input.classList.contains('password-input')) {
                const type = input.type === 'password' ? 'text' : 'password';
                input.type = type;
                this.classList.toggle('fa-eye');
                this.classList.toggle('fa-eye-slash');
            }
        });
    });

    // AJAX para registro
    $("#RegistroForm").on("submit", function (e) {
        e.preventDefault();


        var fecha = new Date($("#FechaNacimiento").val());
        var hoy = new Date();

        var edad = hoy.getFullYear() - fecha.getFullYear();
        var m = hoy.getMonth() - fecha.getMonth();
        if (m < 0 || (m === 0 && hoy.getDate() < fecha.getDate())) {
            edad--;
        }

        if (edad < 17) {
            e.preventDefault();
            Swal.fire({
                icon: 'warning',
                title: 'Edad insuficiente',
                text: 'Debes ser mayor de 17 años para registrarte.',
                confirmButtonColor: '#2D594D',
                confirmButtonText: 'Aceptar'
            });
            return false;
        }


        const formData = {
            Nombre: $("#NombreRegistro").val().trim(),
            Apellido1: $("#Apellido1").val().trim(),
            Apellido2: $("#Apellido2").val().trim(),
            CorreoPersonal: $("#CorreoRegistro").val().trim(),
            IdEspecialidad: $("#IdEspecialidad").val(),
            IdSeccion: $("#IdSeccion").val(),
            FechaNacimiento: $("#FechaNacimiento").val().trim(),
            Cedula: $("#CedulaRegistro").val().trim(),
            Contrasenna: $("#ContrasennaRegistro").val().trim()
        };

        // Validación básica
        for (const key in formData) {
            if (!formData[key]) {
                Swal.fire({
                    icon: 'warning',
                    title: 'Error',
                    text: 'Debes completar todos los campos.',
                    confirmButtonColor: '#2D594D',
                    confirmButtonText: 'Aceptar'
                });
                return;
            }
        }

        if (formData.Contrasenna.length < 8) {

            Swal.fire({
                icon: 'warning',
                title: 'Error',
                text: 'La contraseña debe tener al menos 8 carácteres',
                confirmButtonColor: '#2D594D',
                confirmButtonText: 'Aceptar'
            });
            return;
        }

        if (formData.Contrasenna !== $("#ContrasennaConfirmar").val().trim()) {
            Swal.fire({
                icon: 'warning',
                title: 'Error',
                text: 'Las contraseñas no coinciden.',
                confirmButtonColor: '#2D594D',
                confirmButtonText: 'Aceptar'
            });
            return;
        }

        $.ajax({
            type: "POST",
            url: registroUrl,
            data: formData,
            dataType: "json",
            success: function (resp) {
                if (resp.success) {
                    Swal.fire({
                        icon: 'success',
                        title: 'Cuenta creada correctamente',
                        text: resp.message,
                        confirmButtonColor: '#2D594D',
                        confirmButtonText: 'Aceptar'
                    }).then(() => {
                        window.location.href = loginUrl;
                    });
                } else {
                    Swal.fire({
                        icon: 'error',
                        title: 'Error',
                        text: resp.message,
                        confirmButtonColor: '#2D594D',
                        confirmButtonText: 'Aceptar'
                    });
                }
            },
            error: function () {
                Swal.fire({
                    icon: 'warning',
                    title: 'Error',
                    text: 'Error de comunicación con el servidor.',
                    confirmButtonColor: '#2D594D',
                    confirmButtonText: 'Aceptar'
                });
            }
        });



    });
});