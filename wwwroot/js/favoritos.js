(function () {

    function getToken() {
        var input = document.querySelector('input[name="__RequestVerificationToken"]');
        return input ? input.value : "";
    }

    function post(action, mediaId) {
        var body = new URLSearchParams();
        body.append("mediaId", mediaId);

        return fetch("/Favoritos/" + action, {
            method: "POST",
            headers: {
                "Content-Type": "application/x-www-form-urlencoded",
                "RequestVerificationToken": getToken()
            },
            body: body.toString()
        }).then(function (r) {
            if (!r.ok) throw new Error("HTTP " + r.status);
            return r.json();
        });
    }


    function setButtonState(btn, isFavorite) {
        btn.classList.toggle("is-favorite", isFavorite);

        if (btn.classList.contains("btn-favorite")) {
            btn.textContent = isFavorite ? "♥ En favoritos" : "♡ Favorito";
        }
        btn.title = isFavorite ? "Quitar de favoritos" : "Agregar a favoritos";
    }

    function applyState(mediaId, isFavorite) {
        document
            .querySelectorAll('.btn-favorite[data-media-id="' + mediaId + '"]')
            .forEach(function (b) { setButtonState(b, isFavorite); });
    }


    window.toggleFavorite = function (btn) {
        var mediaId = btn.dataset.mediaId;
        if (!mediaId || btn.dataset.busy === "1") return;

        btn.dataset.busy = "1";

        post("Toggle", mediaId)
            .then(function (res) { applyState(mediaId, res.isFavorite); })
            .catch(function () { alert("No se pudo actualizar el favorito. Intenta de nuevo."); })
            .finally(function () { btn.dataset.busy = "0"; });
    };


    window.removeFavoriteCard = function (mediaId) {
        var card = document.querySelector('.fav-card[data-media-id="' + mediaId + '"]');
        if (!card || card.dataset.busy === "1") return;

        card.dataset.busy = "1";

        post("Quitar", mediaId)
            .then(function (res) {
                card.classList.add("removing");

                setTimeout(function () {
                    card.remove();
                    updateCountAndEmpty(res.count);
                }, 300);
            })
            .catch(function () {
                card.dataset.busy = "0";
                alert("No se pudo quitar el favorito. Intenta de nuevo.");
            });
    };

    function updateCountAndEmpty(serverTotal) {
        var visible = document.querySelectorAll(".fav-card").length;

        var count = document.getElementById("favCount");
        if (count) {
            count.textContent = visible + (visible === 1 ? " elemento guardado" : " elementos guardados");
        }

        var empty = document.getElementById("favEmpty");
        if (empty) empty.style.display = visible === 0 ? "" : "none";
    }


    document.addEventListener("DOMContentLoaded", function () {
        if (!document.querySelector('.btn-favorite[data-media-id]')) return;

        fetch("/Favoritos/Ids")
            .then(function (r) { return r.ok ? r.json() : []; })
            .then(function (ids) {
                ids.forEach(function (id) { applyState(id, true); });
            })
            .catch(function () { /* sin estado visual si falla */ });
    });

})();
