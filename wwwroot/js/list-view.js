/*
 * list-view.js — bascule generique Cartes / Liste + recherche pour les listes-tableaux.
 * Opt-in : ajouter la classe "js-list-cards" sur une <table>.
 * - genere des cartes a partir des lignes (en-tetes = libelles, 1re colonne = titre)
 * - recherche client-side qui filtre lignes ET cartes
 * - compatible avec l'export existant (la table reste dans le DOM, hors-ecran en vue cartes)
 * - memorise la vue choisie par page (localStorage)
 */
(function () {
    function text(el) {
        return (el.innerText || el.textContent || '').replace(/\s+/g, ' ').trim();
    }

    function isActionCell(cell) {
        if (text(cell)) {
            return false;
        }
        return cell.querySelector('a, button, form, input, select') != null;
    }

    function buildCard(row, headers) {
        var card = document.createElement('div');
        card.className = 'lv-card';

        var cells = Array.prototype.slice.call(row.cells);
        var titleIdx = -1;
        for (var i = 0; i < cells.length; i++) {
            if (!isActionCell(cells[i]) && text(cells[i])) {
                titleIdx = i;
                break;
            }
        }

        var head = document.createElement('div');
        head.className = 'lv-card__head';
        var title = document.createElement('span');
        title.className = 'lv-card__title';
        title.innerHTML = titleIdx >= 0 ? cells[titleIdx].innerHTML : '-';
        head.appendChild(title);
        card.appendChild(head);

        var body = document.createElement('div');
        body.className = 'lv-card__body';
        var actions = document.createElement('div');
        actions.className = 'lv-card__actions';

        cells.forEach(function (cell, index) {
            if (index === titleIdx) {
                return;
            }

            if (isActionCell(cell)) {
                Array.prototype.slice.call(cell.childNodes).forEach(function (node) {
                    actions.appendChild(node.cloneNode(true));
                });
                return;
            }

            if (!text(cell)) {
                return;
            }

            var line = document.createElement('div');
            line.className = 'lv-card__line';
            if (headers[index]) {
                var label = document.createElement('span');
                label.className = 'lv-card__label';
                label.textContent = headers[index];
                line.appendChild(label);
            }
            var value = document.createElement('span');
            value.className = 'lv-card__value';
            value.innerHTML = cell.innerHTML;
            line.appendChild(value);
            body.appendChild(line);
        });

        card.appendChild(body);
        if (actions.childNodes.length) {
            card.appendChild(actions);
        }
        return card;
    }

    function enhance(table) {
        if (table.dataset.lvDone) {
            return;
        }
        table.dataset.lvDone = '1';

        var tbody = table.tBodies[0];
        if (!tbody) {
            return;
        }
        var rows = Array.prototype.slice.call(tbody.rows).filter(function (r) {
            return r.cells.length > 0;
        });
        if (rows.length < 1) {
            return;
        }

        var wrap = table.closest('.table-responsive') || table.parentElement;
        var headers = table.tHead && table.tHead.rows.length
            ? Array.prototype.slice.call(table.tHead.rows[0].cells).map(text)
            : [];

        var cards = rows.map(function (row) {
            return buildCard(row, headers);
        });

        var cardsWrap = document.createElement('div');
        cardsWrap.className = 'lv-cards';
        cards.forEach(function (c) { cardsWrap.appendChild(c); });

        // Structure : [toolbar] [container > cards + table-wrap(table)]
        var container = document.createElement('div');
        container.className = 'lv-container';

        wrap.parentNode.insertBefore(container, wrap);
        container.appendChild(cardsWrap);
        var tableWrap = document.createElement('div');
        tableWrap.className = 'lv-table-wrap';
        container.appendChild(tableWrap);
        tableWrap.appendChild(wrap);

        var toolbar = document.createElement('div');
        toolbar.className = 'lv-toolbar';
        toolbar.innerHTML =
            '<div class="lv-search"><i class="bi bi-search"></i>' +
            '<input type="search" class="form-control form-control-sm" placeholder="Rechercher..." autocomplete="off"></div>' +
            '<div class="btn-group lv-toggle" role="group" aria-label="Changer de vue">' +
            '<button type="button" class="btn btn-sm" data-lv="cards" title="Vue en cartes"><i class="bi bi-grid-3x3-gap"></i></button>' +
            '<button type="button" class="btn btn-sm" data-lv="list" title="Vue en liste"><i class="bi bi-list-ul"></i></button>' +
            '</div>';
        container.parentNode.insertBefore(toolbar, container);

        var searchInput = toolbar.querySelector('input');
        var toggleButtons = Array.prototype.slice.call(toolbar.querySelectorAll('[data-lv]'));
        var storageKey = 'lv-view:' + location.pathname;

        function setView(view) {
            container.setAttribute('data-view', view);
            toggleButtons.forEach(function (btn) {
                btn.classList.toggle('is-active', btn.getAttribute('data-lv') === view);
            });
            try { localStorage.setItem(storageKey, view); } catch (e) { /* ignore */ }
        }

        toggleButtons.forEach(function (btn) {
            btn.addEventListener('click', function () { setView(btn.getAttribute('data-lv')); });
        });

        searchInput.addEventListener('input', function () {
            var term = this.value.trim().toLowerCase();
            rows.forEach(function (row, i) {
                var match = !term || text(row).toLowerCase().indexOf(term) !== -1;
                row.style.display = match ? '' : 'none';
                if (cards[i]) { cards[i].style.display = match ? '' : 'none'; }
            });
        });

        var saved = 'list';
        try { saved = localStorage.getItem(storageKey) || 'list'; } catch (e) { /* ignore */ }
        setView(saved === 'cards' ? 'cards' : 'list');
    }

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('table.js-list-cards').forEach(enhance);
    });
})();
