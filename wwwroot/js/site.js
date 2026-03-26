document.addEventListener('DOMContentLoaded', function () {
    // Navbar scroll effect
    var navbar = document.querySelector('.navbar-mango');
    if (navbar) {
        window.addEventListener('scroll', function () {
            if (window.scrollY > 50) {
                navbar.style.background = 'rgba(255, 255, 255, 0.97)';
                navbar.style.boxShadow = '0 4px 20px rgba(0, 0, 0, 0.06)';
            } else {
                navbar.style.background = 'rgba(255, 255, 255, 0.9)';
                navbar.style.boxShadow = 'none';
            }
        });
    }

    // Smooth reveal on scroll
    var observer = new IntersectionObserver(function (entries) {
        entries.forEach(function (entry) {
            if (entry.isIntersecting) {
                entry.target.style.opacity = '1';
                entry.target.style.transform = 'translateY(0)';
            }
        });
    }, { threshold: 0.1 });

    document.querySelectorAll('.card-3d, .stat-card-3d, .commissaire-card, .livre-dor-card').forEach(function (el) {
        /* Pas d’animation d’entrée sur les blocs liste (tableau) : évite effet « zoom » / saut */
        if (el.querySelector && el.querySelector('.table-modern')) return;
        el.style.opacity = '0';
        el.style.transform = 'translateY(30px)';
        el.style.transition = 'opacity 0.6s ease, transform 0.6s ease';
        observer.observe(el);
    });
});

document.addEventListener('DOMContentLoaded', function () {
    var trigger = document.getElementById('notificationTrigger');
    var panel = document.getElementById('notificationPanel');
    var list = document.getElementById('notificationList');
    var empty = document.getElementById('notificationEmpty');
    var badge = document.getElementById('notificationBadge');
    var clear = document.getElementById('notificationClear');
    var toastStack = document.getElementById('notificationToastStack');

    if (!trigger || !panel || !list || !empty || !badge || !clear || !toastStack) return;

    var storageKey = 'mango.notifications.fallback.' + (window.location.host || 'local');
    var requestToken = document.querySelector('meta[name="request-verification-token"]');
    var notifications = [];
    var unreadCount = 0;

    function escapeHtml(value) {
        return String(value || '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    function formatDateLabel(value) {
        var date = value ? new Date(value) : new Date();
        return date.toLocaleString('fr-FR', {
            day: '2-digit',
            month: '2-digit',
            hour: '2-digit',
            minute: '2-digit'
        });
    }

    function saveFallback() {
        try {
            window.localStorage.setItem(storageKey, JSON.stringify(notifications.filter(function (item) { return item.source === 'local'; }).slice(0, 25)));
        } catch (e) {
        }
    }

    function sortNotifications() {
        notifications.sort(function (a, b) {
            return new Date(b.dateCreation).getTime() - new Date(a.dateCreation).getTime();
        });
        notifications = notifications.slice(0, 25);
    }

    function syncBadge() {
        unreadCount = notifications.filter(function (item) { return !item.read; }).length;
        badge.textContent = String(unreadCount);
        badge.classList.toggle('d-none', unreadCount === 0);
    }

    function render() {
        list.querySelectorAll('.notification-item').forEach(function (node) { node.remove(); });

        if (!notifications.length) {
            empty.hidden = false;
            syncBadge();
            return;
        }

        empty.hidden = true;
        notifications.forEach(function (item) {
            var article = document.createElement('article');
            article.className = 'notification-item' + (item.read ? '' : ' is-unread');
            var bodyTag = item.link ? 'a' : 'div';
            var hrefAttr = item.link ? ' href="' + escapeHtml(item.link) + '"' : '';
            article.innerHTML =
                '<div class="notification-item__icon"><i class="bi bi-bell-fill"></i></div>' +
                '<' + bodyTag + ' class="notification-item__body' + (item.link ? ' notification-item__body-link' : '') + '"' + hrefAttr + '>' +
                (item.title ? '<div class="notification-item__title">' + escapeHtml(item.title) + '</div>' : '') +
                '<div class="notification-item__message">' + escapeHtml(item.message) + '</div>' +
                '<div class="notification-item__meta">' + escapeHtml(formatDateLabel(item.dateCreation)) + '</div>' +
                '</' + bodyTag + '>';
            list.appendChild(article);
        });

        syncBadge();
    }

    function markAllAsRead() {
        notifications = notifications.map(function (item) {
            item.read = true;
            return item;
        });
        render();

        if (requestToken && requestToken.content) {
            fetch('/Notifications/MarkAllRead', {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': requestToken.content,
                    'X-Requested-With': 'XMLHttpRequest'
                }
            }).catch(function () {
            });
        }

        saveFallback();
    }

    function addToast(message) {
        var toast = document.createElement('div');
        toast.className = 'notification-toast';
        toast.innerHTML = '<i class="bi bi-bell-fill"></i><span>' + escapeHtml(message) + '</span>';
        toastStack.appendChild(toast);

        window.setTimeout(function () {
            toast.classList.add('is-visible');
        }, 10);

        window.setTimeout(function () {
            toast.classList.remove('is-visible');
            window.setTimeout(function () {
                toast.remove();
            }, 260);
        }, 4200);
    }

    function addLocalNotification(message, isRead) {
        var now = new Date().toISOString();
        notifications.unshift({
            id: 'local-' + Date.now(),
            title: 'Notification',
            message: message,
            link: null,
            source: 'local',
            read: !!isRead,
            dateCreation: now
        });
        sortNotifications();
        saveFallback();
        render();
    }

    function syncServerNotifications(items) {
        var locals = notifications.filter(function (item) { return item.source === 'local'; });
        var servers = (items || []).map(function (item) {
            return {
                id: item.id,
                title: item.titre,
                message: item.message,
                link: item.lien,
                source: 'server',
                read: item.estLue,
                dateCreation: item.dateCreation
            };
        });

        notifications = servers.concat(locals.filter(function (localItem) {
            return !servers.some(function (serverItem) {
                return serverItem.message === localItem.message;
            });
        }));
        sortNotifications();
        saveFallback();
        render();
    }

    function loadServerNotifications() {
        return fetch('/Notifications/Mine?take=20', {
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
            .then(function (response) {
                if (!response.ok) throw new Error('notifications');
                return response.json();
            })
            .then(function (items) {
                syncServerNotifications(items);
                return true;
            })
            .catch(function () {
                return false;
            });
    }

    function togglePanel(forceOpen) {
        var shouldOpen = typeof forceOpen === 'boolean' ? forceOpen : panel.hidden;
        panel.hidden = !shouldOpen;
        trigger.setAttribute('aria-expanded', shouldOpen ? 'true' : 'false');
        if (shouldOpen) {
            markAllAsRead();
            loadServerNotifications();
        }
    }

    try {
        var saved = window.localStorage.getItem(storageKey);
        if (saved) {
            notifications = JSON.parse(saved) || [];
        }
    } catch (e) {
        notifications = [];
    }

    render();
    loadServerNotifications();

    trigger.addEventListener('click', function (e) {
        e.preventDefault();
        e.stopPropagation();
        togglePanel();
    });

    clear.addEventListener('click', function () {
        markAllAsRead();
    });

    document.addEventListener('click', function (e) {
        if (!panel.hidden && !panel.contains(e.target) && !trigger.contains(e.target)) {
            togglePanel(false);
        }
    });

    if (window.signalR) {
        var connection = new window.signalR.HubConnectionBuilder()
            .withUrl('/hubs/notifications')
            .withAutomaticReconnect()
            .build();

        connection.on('RecevoirNotification', function (message) {
            addToast(message);
            window.setTimeout(function () {
                loadServerNotifications().then(function (loaded) {
                    if (!loaded) {
                        addLocalNotification(message, false);
                    }
                });
            }, 250);
        });

        connection.start().catch(function () {
        });
    }
});

// Sidebar dropdown toggle
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.sidebar-toggle').forEach(function (toggle) {
        toggle.addEventListener('click', function (e) {
            e.preventDefault();
            var group = toggle.closest('.sidebar-group');
            // Fermer les autres groupes
            document.querySelectorAll('.sidebar-group.open').forEach(function (g) {
                if (g !== group) g.classList.remove('open');
            });
            group.classList.toggle('open');
        });
    });

    // Ouvrir automatiquement le groupe contenant la page active
    var currentPath = window.location.pathname.toLowerCase();
    document.querySelectorAll('.sidebar-submenu .nav-link').forEach(function (link) {
        var href = link.getAttribute('href');
        if (href && currentPath.startsWith(href.toLowerCase())) {
            link.classList.add('active');
            var group = link.closest('.sidebar-group');
            if (group) group.classList.add('open');
        }
    });

    // Fermer la sidebar mobile au clic sur un lien
    document.querySelectorAll('.sidebar .nav-link:not(.sidebar-toggle)').forEach(function (link) {
        link.addEventListener('click', function () {
            var nav = document.getElementById('sidebarNav');
            var overlay = document.getElementById('sidebarOverlay');
            if (nav) nav.classList.remove('open');
            if (overlay) overlay.classList.remove('open');
        });
    });
});
