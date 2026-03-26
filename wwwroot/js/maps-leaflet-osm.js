/**
 * Cartes du site : Leaflet.js + OpenStreetMap (tuiles standard OSM).
 * @see https://leafletjs.com/
 * @see https://www.openstreetmap.org/copyright
 */
(function (w) {
    'use strict';

    var OSM_TILE_URL = 'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png';
    var OSM_ATTRIBUTION =
        '&copy; <a href="https://www.openstreetmap.org/copyright" rel="noopener noreferrer" target="_blank">OpenStreetMap</a> contributors';

    w.MangoLeafletOsm = {
        tileUrl: OSM_TILE_URL,
        attribution: OSM_ATTRIBUTION,

        /** Ajoute la couche raster OpenStreetMap sur une carte Leaflet existante. */
        addOpenStreetMapLayer: function (map, options) {
            if (!map || typeof L === 'undefined') return null;
            var opts = options || {};
            return L.tileLayer(OSM_TILE_URL, {
                attribution: OSM_ATTRIBUTION,
                maxZoom: opts.maxZoom != null ? opts.maxZoom : 19,
                minZoom: opts.minZoom != null ? opts.minZoom : 1
            }).addTo(map);
        },

        /** Recalcul de taille (sidebar, responsive, chargement différé). */
        bindMapResize: function (map, el) {
            if (!map || !el) return;
            function refresh() {
                map.invalidateSize();
            }
            map.whenReady(refresh);
            setTimeout(refresh, 50);
            setTimeout(refresh, 250);
            setTimeout(refresh, 400);
            w.addEventListener('resize', refresh);
            if (w.ResizeObserver) {
                new w.ResizeObserver(refresh).observe(el);
            }
        },

        /** Icône marqueur groupes (Bootstrap Icons dans le DOM). */
        groupeMarkerIcon: function () {
            return L.divIcon({
                html: '<i class="bi bi-geo-alt-fill" style="font-size:2rem;color:#8ab55a;text-shadow:0 2px 8px rgba(0,0,0,0.5);"></i>',
                className: '',
                iconSize: [30, 36],
                iconAnchor: [15, 36]
            });
        }
    };
})(window);
