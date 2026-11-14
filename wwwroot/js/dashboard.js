// Dashboard: selección de archivo, ventana "Ver JSON" y miniaturas rotas.
(function () {
    'use strict';

    // ---------- Importar: habilitar el botón al elegir un archivo ----------
    var archivo = document.getElementById('archivo');
    var nombre = document.getElementById('archivoNombre');
    var importBtn = document.getElementById('importBtn');
    var importForm = document.getElementById('importForm');
    var syncForm = document.getElementById('syncForm');
    var syncBtn = document.getElementById('syncBtn');

    if (archivo) {
        archivo.addEventListener('change', function () {
            var elegido = archivo.files && archivo.files[0];
            nombre.textContent = elegido ? elegido.name : 'Ningún archivo seleccionado';
            importBtn.disabled = !elegido;
        });
    }

    // Evita enviar dos veces el mismo formulario.
    if (importForm) {
        importForm.addEventListener('submit', function () {
            importBtn.disabled = true;
            importBtn.textContent = 'Importando…';
        });
    }

    if (syncForm) {
        syncForm.addEventListener('submit', function () {
            syncBtn.disabled = true;
            syncBtn.textContent = '⏳ Consultando las APIs…';
        });
    }

    // ---------- Ver JSON ----------
    var modalEl = document.getElementById('jsonModal');
    var cuerpo = document.getElementById('jsonModalBody');
    var titulo = document.getElementById('jsonModalTitle');
    var copiar = document.getElementById('jsonCopy');
    var modal = null;

    function abrirModal() {
        if (!modalEl || typeof bootstrap === 'undefined') { return; }
        modal = modal || new bootstrap.Modal(modalEl);
        modal.show();
    }

    document.addEventListener('click', function (e) {
        var boton = e.target.closest ? e.target.closest('.js-ver-json') : null;
        if (!boton) { return; }

        titulo.textContent = boton.getAttribute('data-title') || 'JSON del item';
        cuerpo.textContent = 'Cargando…';
        abrirModal();

        fetch(boton.getAttribute('data-url'), { headers: { 'Accept': 'application/json' } })
            .then(function (r) {
                if (!r.ok) { throw new Error('HTTP ' + r.status); }
                return r.json();
            })
            .then(function (json) {
                cuerpo.textContent = JSON.stringify(json, null, 2);
            })
            .catch(function () {
                cuerpo.textContent = 'No se pudo cargar el JSON de este item.';
            });
    });

    if (copiar) {
        copiar.addEventListener('click', function () {
            var texto = cuerpo.textContent || '';
            if (navigator.clipboard) {
                navigator.clipboard.writeText(texto).then(function () {
                    copiar.textContent = 'Copiado ✓';
                    setTimeout(function () { copiar.textContent = 'Copiar'; }, 1500);
                });
            }
        });
    }

    // ---------- Miniaturas que no cargan: queda el ícono de fondo ----------
    document.addEventListener('error', function (e) {
        var img = e.target;
        if (img && img.classList && img.classList.contains('db-thumb-img')) {
            img.classList.add('db-broken');
        }
    }, true);
})();
