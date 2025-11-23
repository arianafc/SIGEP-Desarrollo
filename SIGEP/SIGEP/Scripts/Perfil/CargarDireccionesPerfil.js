
function decodeHtml(html) {
    const txt = document.createElement("textarea");
    txt.innerHTML = html;
    return txt.value;
}


async function esperarDireccionesCargadas(maxIntentos = 20) {
    let intentos = 0;
    while (!window.direccionesData && intentos < maxIntentos) {
        await new Promise(r => setTimeout(r, 100));
        intentos++;
    }
 
}

async function inicializarDireccionDesdeModelo(provinciaModeloRaw, cantonModeloRaw, distritoModeloRaw) {

    await esperarDireccionesCargadas();

    const provinciaSelect = document.querySelector('.ddl-provincia');
    const cantonSelect = document.querySelector('.ddl-canton');
    const distritoSelect = document.querySelector('.ddl-distrito');

    const provinciaModelo = decodeHtml(provinciaModeloRaw || '');
    const cantonModelo = decodeHtml(cantonModeloRaw || '');
    const distritoModelo = decodeHtml(distritoModeloRaw || '');



    if (!provinciaModelo) {
       
        return;
    }

    
    provinciaSelect.value = provinciaModelo;
   
    cargarCantones(provinciaSelect); 

    
    if (cantonModelo) {
        const opcionCanton = Array.from(cantonSelect.options)
            .find(o => o.value === cantonModelo);

        if (opcionCanton) {
            cantonSelect.value = cantonModelo;
        

           
            cargarDistritos(cantonSelect); 

            if (distritoModelo) {
                const opcionDistrito = Array.from(distritoSelect.options)
                    .find(o => o.value === distritoModelo);

                if (opcionDistrito) {
                    distritoSelect.value = distritoModelo;
                
                } 
            }
        } 
    }
}
