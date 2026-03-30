(function () {
    function normalizeText(value) {
        return (value || '').replace(/\s+/g, ' ').trim();
    }

    function slugify(value) {
        return normalizeText(value)
            .normalize('NFD')
            .replace(/[\u0300-\u036f]/g, '')
            .replace(/[^a-zA-Z0-9]+/g, '-')
            .replace(/^-+|-+$/g, '')
            .toLowerCase() || 'liste';
    }

    function escapeHtml(value) {
        return String(value || '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    function isVisible(element) {
        if (!element) {
            return false;
        }

        var style = window.getComputedStyle(element);
        return style.display !== 'none'
            && style.visibility !== 'hidden'
            && !element.hidden
            && (element.offsetWidth > 0 || element.offsetHeight > 0 || element.getClientRects().length > 0);
    }

    function uniqueElements(elements) {
        var seen = new Set();
        return elements.filter(function (element) {
            if (!element || seen.has(element)) {
                return false;
            }

            seen.add(element);
            return true;
        });
    }

    function cloneForExport(element) {
        var clone = element.cloneNode(true);
        clone.querySelectorAll('script, style, form, button, input, select, textarea, canvas, video, img, svg, .btn, .no-print, .pagination-clean, .js-list-export-toolbar').forEach(function (node) {
            node.remove();
        });
        clone.querySelectorAll('i.bi').forEach(function (node) {
            node.remove();
        });
        return clone;
    }

    function extractTableData(root) {
        var table = root.matches('table') ? root : root.querySelector('table');
        if (!table || !isVisible(table)) {
            return null;
        }

        var headerCells = Array.from(table.querySelectorAll('thead th')).filter(isVisible);
        var headers = headerCells.map(function (cell) { return normalizeText(cell.innerText); });

        var bodyRows = Array.from(table.querySelectorAll('tbody tr')).filter(isVisible);
        if (!bodyRows.length) {
            bodyRows = Array.from(table.querySelectorAll('tr')).filter(function (row, index) {
                return isVisible(row) && (index > 0 || !headers.length);
            });
        }

        var rows = bodyRows.map(function (row) {
            return Array.from(row.children)
                .filter(isVisible)
                .map(function (cell) { return normalizeText(cell.innerText); });
        }).filter(function (row) {
            return row.some(function (value) { return value; });
        });

        if (!rows.length) {
            return null;
        }

        if (!headers.length) {
            headers = rows[0].map(function (_, index) { return 'Colonne ' + (index + 1); });
        }

        return {
            headers: headers,
            rows: rows
        };
    }

    function extractCardData(root) {
        var selectors = ['.support-ticket-card', '.support-resource-card', '.campus-discussion-card', '.card-3d'];
        var cards = [];

        selectors.forEach(function (selector) {
            Array.from(root.querySelectorAll(selector)).forEach(function (card) {
                if (!isVisible(card)) {
                    return;
                }

                var parentCard = card.parentElement && card.parentElement.closest(selector);
                if (parentCard && root.contains(parentCard)) {
                    return;
                }

                cards.push(card);
            });
        });

        cards = uniqueElements(cards);
        if (!cards.length && selectors.some(function (selector) { return root.matches(selector); }) && isVisible(root)) {
            cards = [root];
        }

        if (!cards.length) {
            return null;
        }

        var rows = cards.map(function (card, index) {
            var clone = cloneForExport(card);
            var titleElement = clone.querySelector('h1, h2, h3, h4, h5, h6, .support-resource-card__title, .campus-discussion-card__title, .support-ticket-card__title, .fw-semibold, strong');
            var title = normalizeText(titleElement ? titleElement.innerText : '');
            var details = normalizeText(clone.innerText || clone.textContent || '');

            if (title && details.indexOf(title) === 0) {
                details = normalizeText(details.slice(title.length));
            }

            return [title || ('Element ' + (index + 1)), details || '-'];
        }).filter(function (row) {
            return row[0] || row[1];
        });

        if (!rows.length) {
            return null;
        }

        return {
            headers: ['Element', 'Details'],
            rows: rows
        };
    }

    function extractDataFromRoot(root, mode) {
        if (!root || !isVisible(root)) {
            return null;
        }

        if (mode === 'cards') {
            return extractCardData(root) || extractTableData(root);
        }

        if (mode === 'table') {
            return extractTableData(root) || extractCardData(root);
        }

        return extractTableData(root) || extractCardData(root);
    }

    function findExportRoot(toolbar) {
        var mode = toolbar.getAttribute('data-export-mode') || '';
        var explicitTarget = toolbar.getAttribute('data-export-target');
        if (explicitTarget) {
            var targetNode = document.querySelector(explicitTarget);
            var explicitData = extractDataFromRoot(targetNode, mode);
            return explicitData ? { root: targetNode, dataset: explicitData } : null;
        }

        var current = toolbar.previousElementSibling;
        while (current) {
            var dataset = extractDataFromRoot(current, mode);
            if (dataset) {
                return { root: current, dataset: dataset };
            }

            current = current.previousElementSibling;
        }

        var fallbackSelectors = ['.support-ticket-grid', '.campus-discussion-list', '.table-responsive', 'table', '.row'];
        for (var i = 0; i < fallbackSelectors.length; i++) {
            var nodes = Array.from(document.querySelectorAll(fallbackSelectors[i]));
            for (var j = nodes.length - 1; j >= 0; j--) {
                var node = nodes[j];
                var fallbackData = extractDataFromRoot(node, mode);
                if (fallbackData) {
                    return { root: node, dataset: fallbackData };
                }
            }
        }

        return null;
    }

    function buildFilename(title) {
        var date = new Date();
        var stamp = [
            date.getFullYear(),
            String(date.getMonth() + 1).padStart(2, '0'),
            String(date.getDate()).padStart(2, '0')
        ].join('');
        return slugify(title) + '-' + stamp;
    }

    function downloadBlob(filename, blob) {
        var url = URL.createObjectURL(blob);
        var link = document.createElement('a');
        link.href = url;
        link.download = filename;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        setTimeout(function () { URL.revokeObjectURL(url); }, 1000);
    }

    function exportExcel(title, dataset) {
        var tableHtml = '<table><thead><tr>'
            + dataset.headers.map(function (header) { return '<th>' + escapeHtml(header) + '</th>'; }).join('')
            + '</tr></thead><tbody>'
            + dataset.rows.map(function (row) {
                return '<tr>' + row.map(function (cell) { return '<td>' + escapeHtml(cell) + '</td>'; }).join('') + '</tr>';
            }).join('')
            + '</tbody></table>';

        var html = '<html><head><meta charset="utf-8"></head><body>' + tableHtml + '</body></html>';
        var blob = new Blob(['\ufeff', html], { type: 'application/vnd.ms-excel;charset=utf-8;' });
        downloadBlob(buildFilename(title) + '.xls', blob);
    }

    function exportPdfWithPrint(title, dataset) {
        var printWindow = window.open('', '_blank', 'noopener,noreferrer,width=1100,height=800');
        if (!printWindow) {
            window.alert('Autorisez les fenetres popup pour exporter en PDF.');
            return;
        }

        var tableRows = dataset.rows.map(function (row) {
            return '<tr>' + row.map(function (cell) { return '<td>' + escapeHtml(cell) + '</td>'; }).join('') + '</tr>';
        }).join('');

        printWindow.document.write('<!doctype html><html><head><meta charset="utf-8"><title>' + escapeHtml(title) + '</title><style>body{font-family:Arial,sans-serif;padding:24px;color:#1f2937}h1{font-size:22px;margin-bottom:8px}p{color:#6b7280;margin-bottom:18px}table{width:100%;border-collapse:collapse;font-size:12px}th,td{border:1px solid #d1d5db;padding:8px;vertical-align:top;text-align:left}th{background:#eef4e0}@media print{body{padding:0}}</style></head><body><h1>' + escapeHtml(title) + '</h1><p>Extrait le ' + new Date().toLocaleString() + '</p><table><thead><tr>' + dataset.headers.map(function (header) { return '<th>' + escapeHtml(header) + '</th>'; }).join('') + '</tr></thead><tbody>' + tableRows + '</tbody></table><script>window.onload=function(){window.print();}<\/script></body></html>');
        printWindow.document.close();
    }

    function exportPdf(title, dataset) {
        if (window.jspdf && window.jspdf.jsPDF) {
            var orientation = dataset.headers.length > 4 ? 'landscape' : 'portrait';
            var doc = new window.jspdf.jsPDF({ orientation: orientation, unit: 'pt', format: 'a4' });
            doc.setFontSize(16);
            doc.text(title, 32, 36);
            doc.setFontSize(10);
            doc.text('Extrait le ' + new Date().toLocaleString(), 32, 54);
            doc.autoTable({
                head: [dataset.headers],
                body: dataset.rows,
                startY: 68,
                margin: { left: 24, right: 24 },
                styles: { fontSize: 8, cellPadding: 5, overflow: 'linebreak' },
                headStyles: { fillColor: [89, 117, 55], textColor: 255 },
                alternateRowStyles: { fillColor: [248, 250, 252] }
            });
            doc.save(buildFilename(title) + '.pdf');
            return;
        }

        exportPdfWithPrint(title, dataset);
    }

    function bindToolbar(toolbar) {
        var initialResolution = findExportRoot(toolbar);
        if (!initialResolution || !initialResolution.dataset || !initialResolution.dataset.rows.length) {
            toolbar.style.display = 'none';
            return;
        }

        toolbar.querySelectorAll('[data-export-format]').forEach(function (button) {
            button.addEventListener('click', function () {
                var resolved = findExportRoot(toolbar);
                if (!resolved || !resolved.dataset || !resolved.dataset.rows.length) {
                    window.alert('Aucune ligne visible a exporter pour cette liste.');
                    return;
                }

                var title = toolbar.getAttribute('data-export-title') || document.title || 'Liste';
                if (button.getAttribute('data-export-format') === 'excel') {
                    exportExcel(title, resolved.dataset);
                    return;
                }

                exportPdf(title, resolved.dataset);
            });
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('.js-list-export-toolbar').forEach(bindToolbar);
    });
})();


