$(function () {
    var Contrasenna = $('#ContrasennaNueva');
    var ConfirmarContrasenna = $('#ConfirmarContrasenna');
    var BtnActualizar = $('#BtnActualizarContrasenna');
    var Form = $('#CambiarContrasennaForm');

    function validarContrasenna() {
        let pass = Contrasenna.val().trim();
        let confirm = ConfirmarContrasenna.val().trim();

        if (pass.length < 8) {
            Swal.fire({
                icon: 'warning',
                title: 'Contraseña muy corta',
                text: 'La contraseña debe tener al menos 8 caracteres.',
                confirmButtonColor: '#8CA653'
            });
            return false;
        }

        if (pass !== confirm) {
            Swal.fire({
                icon: 'warning',
                title: 'Las contraseñas no coinciden',
                text: 'Por favor, verifique que ambas contraseñas sean iguales.',
                confirmButtonColor: '#8CA653'
            });
            return false;
        }

        return true;
    }

    Form.on("submit", function (e) {
        if (!validarContrasenna()) {
            e.preventDefault(); // evita que se mande si no pasa validación
            return false;
        }
    });
});


// Validación de info personal
document.getElementById("btnActualizarInfo")?.addEventListener("click", function () {
    const campos = document.querySelectorAll('#info-personal input');
    let incompletos = false;

    campos.forEach(input => {
        if (input.value.trim() === "") {
            incompletos = true;
        }
    });

    if (incompletos) {
        Swal.fire({
            icon: 'warning',
            title: 'Campos incompletos',
            text: 'Por favor complete todos los campos obligatorios',
            confirmButtonColor: '#8CA653'
        });
    } else {
        Swal.fire({
            icon: 'success',
            title: 'Información actualizada correctamente',
            showConfirmButton: false,
            timer: 2000
        });
    }
});

// Reutilizable éxito
function mostrarSuccessMensaje() {
    Swal.fire({
        icon: 'success',
        title: 'Información guardada correctamente',
        showConfirmButton: false,
        timer: 2000
    });
}

["btnActualizarAcademica", "btnActualizarEncargado", "btnActualizarLaboral", "btnActualizarMedica", "btnSubirDoc"].forEach(id => {
    document.getElementById(id)?.addEventListener("click", mostrarSuccessMensaje);
});

// Reutilizable warning
function mostrarWarningMensaje() {
    Swal.fire({
        icon: 'warning',
        title: 'Ha ocurrido un error',
        showConfirmButton: false,
        timer: 2000
    });
}

["btnEliminarEncargado", "btnEliminarFile", "btnDescargarFile", "btnVerFile", "btnGuardarEncargado"].forEach(id => {
    document.getElementById(id)?.addEventListener("click", mostrarWarningMensaje);
});

function editarEncargado(idEncargado) {
   
    console.log("Editar encargado:", idEncargado);
}

function eliminarEncargado(idEncargado) {
    if (confirm("¿Seguro que desea eliminar este encargado?")) {
        
        console.log("Encargado eliminado:", idEncargado);
    }
}