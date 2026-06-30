(function () {
    var widget = document.getElementById('chatWidget');
    if (!widget) return;

    var toggleBtn = document.getElementById('chatWidgetToggle');
    var closeBtn = document.getElementById('chatWidgetClose');
    var panel = document.getElementById('chatWidgetPanel');
    var tokenMeta = document.querySelector('meta[name="request-verification-token"]');
    var token = tokenMeta ? tokenMeta.content : '';
    var estAgent = widget.getAttribute('data-est-agent') === 'true';
    var estConnecte = widget.getAttribute('data-est-connecte') === 'true';

    var thread = document.getElementById('chatThread');
    var suggestions = document.getElementById('chatSuggestions');
    var form = document.getElementById('chatThreadForm');
    var input = document.getElementById('chatThreadInput');

    var modeLive = false;
    var conversationId = null;
    var connection = null;

    function openPanel() {
        panel.hidden = false;
        toggleBtn.setAttribute('aria-expanded', 'true');
        input.focus();
    }

    function closePanel() {
        panel.hidden = true;
        toggleBtn.setAttribute('aria-expanded', 'false');
    }

    toggleBtn.addEventListener('click', function () {
        panel.hidden ? openPanel() : closePanel();
    });
    closeBtn.addEventListener('click', closePanel);

    function addBubble(texte, mine) {
        var div = document.createElement('div');
        div.className = 'chat-widget__message ' + (mine ? 'chat-widget__message--mine' : 'chat-widget__message--theirs');
        div.textContent = texte;
        thread.appendChild(div);
        thread.scrollTop = thread.scrollHeight;
        return div;
    }

    function addCards(items) {
        var box = document.createElement('div');
        box.className = 'chat-widget__results chat-widget__results--inline';
        box.innerHTML = items.map(function (it) {
            return '<div class="chat-widget__result-item"><div><strong>' + escapeHtml(it.titre) + '</strong>' +
                (it.sousTitre ? '<br><small class="text-muted">' + escapeHtml(it.sousTitre) + '</small>' : '') +
                '</div>' + (it.badge ? '<span class="badge bg-secondary">' + escapeHtml(it.badge) + '</span>' : '') + '</div>';
        }).join('');
        thread.appendChild(box);
        thread.scrollTop = thread.scrollHeight;
    }

    function addActionChips(actions) {
        var box = document.createElement('div');
        box.className = 'chat-widget__suggestions chat-widget__suggestions--inline';
        actions.forEach(function (a) {
            var btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'chat-widget__chip';
            btn.textContent = a.label;
            btn.addEventListener('click', function () {
                box.remove();
                a.onClick();
            });
            box.appendChild(btn);
        });
        thread.appendChild(box);
        thread.scrollTop = thread.scrollHeight;
    }

    function creerTicketEtEscalader(probleme) {
        addBubble("Création du ticket et mise en relation avec un agent...", false);
        var body = new URLSearchParams();
        body.set('probleme', probleme);
        fetch('/Chat/CreerTicketDepuisChat', {
            method: 'POST',
            headers: { 'RequestVerificationToken': token, 'Content-Type': 'application/x-www-form-urlencoded' },
            body: body.toString()
        })
            .then(function (r) { return r.json(); })
            .then(function (data) {
                modeLive = true;
                suggestions.hidden = true;
                conversationId = data.conversationId;
                addBubble('Ticket ' + data.numeroTicket + ' créé. Un agent va prendre le relais dans cette discussion.', false);
                startLiveConnection();
            })
            .catch(function () {
                addBubble("Impossible de créer le ticket pour le moment, réessayez plus tard.", false);
            });
    }

    function setTyping(on) {
        var existing = document.getElementById('chatTypingIndicator');
        if (existing) existing.remove();
        if (on) {
            var div = document.createElement('div');
            div.id = 'chatTypingIndicator';
            div.className = 'chat-widget__message chat-widget__message--theirs chat-widget__typing';
            div.textContent = '...';
            thread.appendChild(div);
            thread.scrollTop = thread.scrollHeight;
        }
    }

    // --- Suggestions rapides ---
    suggestions.querySelectorAll('[data-suggestion]').forEach(function (chip) {
        chip.addEventListener('click', function () {
            var texte = chip.getAttribute('data-suggestion');
            input.value = texte;
            form.requestSubmit();
        });
    });

    // --- Bascule vers le live chat (agent humain) ---
    function basculerVersAgent() {
        modeLive = true;
        suggestions.hidden = true;

        fetch('/Chat/Start', { method: 'POST', headers: { 'RequestVerificationToken': token } })
            .then(function (r) { return r.json(); })
            .then(function (data) {
                conversationId = data.conversationId;
                startLiveConnection();
            });
    }

    function startLiveConnection() {
        connection = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/chat')
            .withAutomaticReconnect()
            .build();

        connection.on('NouveauMessage', function (msg) {
            if (msg.conversationId !== conversationId) return;
            if (msg.expediteurId && msg.expediteurNom && modeLive) {
                addBubble(msg.contenu, false);
            }
        });

        connection.start().then(function () {
            connection.invoke('JoinConversation', conversationId);
            if (estAgent) connection.invoke('JoinAgentQueue');
        });
    }

    // --- Envoi d'un message ---
    form.addEventListener('submit', function (e) {
        e.preventDefault();
        var texte = input.value.trim();
        if (!texte) return;
        addBubble(texte, true);
        input.value = '';

        if (modeLive && conversationId) {
            var body = new URLSearchParams();
            body.set('contenu', texte);
            fetch('/Chat/Send/' + conversationId, {
                method: 'POST',
                headers: { 'RequestVerificationToken': token, 'Content-Type': 'application/x-www-form-urlencoded' },
                body: body.toString()
            });
            return;
        }

        setTyping(true);
        var body = new URLSearchParams();
        body.set('message', texte);
        fetch('/Chat/Ask', {
            method: 'POST',
            headers: { 'RequestVerificationToken': token, 'Content-Type': 'application/x-www-form-urlencoded' },
            body: body.toString()
        })
            .then(function (r) { return r.json(); })
            .then(function (data) {
                setTyping(false);
                addBubble(data.texte, false);
                if (data.items && data.items.length) addCards(data.items);

                if (data.intention === 'agent') {
                    basculerVersAgent();
                } else if (data.intention === 'incident-aide' || data.intention === 'incident-generique') {
                    var probleme = data.problemeOriginal || texte;
                    addActionChips([
                        { label: 'C’est résolu, merci', onClick: function () { addBubble('C’est résolu, merci', true); addBubble('Parfait, ravi d’avoir pu aider !', false); } },
                        { label: 'Toujours bloqué, créer un ticket', onClick: function () { creerTicketEtEscalader(probleme); } }
                    ]);
                } else if (data.intention === 'aucun-resultat') {
                    addActionChips([
                        { label: 'Parler à un agent', onClick: function () { input.value = 'Parler à un agent'; form.requestSubmit(); } }
                    ]);
                }
            })
            .catch(function () {
                setTyping(false);
                addBubble("Une erreur est survenue, réessayez.", false);
            });
    });

    function escapeHtml(str) {
        var div = document.createElement('div');
        div.textContent = str == null ? '' : String(str);
        return div.innerHTML;
    }
})();
