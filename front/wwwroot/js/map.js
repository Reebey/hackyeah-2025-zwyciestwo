window.initMap = function (dotNetHelper) {
    // 1. Inicjalizacja mapy
    var map = L.map('map').setView([52.2297, 21.0122], 6);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; OpenStreetMap contributors'
    }).addTo(map);

    // 2. Grupa rysunkowa (warstwa, na której rysujesz)
    var drawnItems = new L.FeatureGroup();
    map.addLayer(drawnItems);

    // 3. Toolbar do rysowania markerów
    var drawControl = new L.Control.Draw({
        edit: {
            featureGroup: drawnItems
        },
        draw: {
            polygon: false,
            circle: false,
            rectangle: false,
            polyline: false,
            marker: true
        }
    });
    map.addControl(drawControl);

    // 4. Gdy użytkownik doda punkt
    map.on(L.Draw.Event.CREATED, function (e) {
        var marker = e.layer;
        drawnItems.addLayer(marker);

        var coords = marker.getLatLng();
        dotNetHelper.invokeMethodAsync('OnMarkerAdded', coords.lat, coords.lng);
    });

    // 5. Gdy użytkownik przesunie/edytuje marker
    map.on('draw:edited', function (e) {
        e.layers.eachLayer(function (layer) {
            var coords = layer.getLatLng();
            dotNetHelper.invokeMethodAsync('OnMarkerMoved', coords.lat, coords.lng);
        });
    });

    // 6. Trasy – dokładne, płynne, z OSRM Routing API
    // === Przykładowe realistyczne trasy ===
    const routes = [
        {
            name: "Warszawa – Kraków",
            color: "red",
            details: "Ekspres InterCity: ok. 2h 30min",
            waypoints: [
                [21.0122, 52.2297], // Warszawa
                [19.9450, 50.0647]  // Kraków
            ]
        },
        {
            name: "Gdańsk – Poznań",
            color: "blue",
            details: "IC Bałtyk: ok. 3h 40min",
            waypoints: [
                [18.6466, 54.3520], // Gdańsk
                [16.9252, 52.4064]  // Poznań
            ]
        },
        {
            name: "Wrocław – Katowice",
            color: "green",
            details: "TLK Ślązak: ok. 2h",
            waypoints: [
                [17.0385, 51.1079], // Wrocław
                [19.0238, 50.2649]  // Katowice
            ]
        },
        {
            name: "Szczecin – Warszawa",
            color: "orange",
            details: "EIC Odra: ok. 5h 30min",
            waypoints: [
                [14.5528, 53.4285], // Szczecin
                [21.0122, 52.2297]  // Warszawa
            ]
        }
    ];

    // === Funkcja do pobierania realistycznych tras z OSRM ===
    async function fetchRoute(route) {
        const [startLng, startLat] = route.waypoints[0];
        const [endLng, endLat] = route.waypoints[1];

        // API OSRM — tryb car (można też 'train' lub 'bike', ale 'car' ma najlepsze pokrycie)
        const url = `https://router.project-osrm.org/route/v1/driving/${startLng},${startLat};${endLng},${endLat}?overview=full&geometries=geojson`;

        try {
            const response = await fetch(url);
            const data = await response.json();

            const coords = data.routes[0].geometry.coordinates.map(c => [c[1], c[0]]);

            const polyline = L.polyline(coords, {
                color: route.color,
                weight: 5,
                opacity: 0.3
            }).addTo(map);

            polyline.bindPopup(`<b>${route.name}</b><br>${route.details}`);

            // Efekt podświetlenia po najechaniu
            polyline.on("mouseover", function () {
                this.setStyle({ opacity: 1, weight: 6 });
                this.bringToFront();
            });

            polyline.on("mouseout", function () {
                this.setStyle({ opacity: 0.3, weight: 5 });
            });

            return coords;
        } catch (err) {
            console.error("Błąd pobierania trasy:", err);
        }
    }

    // === Pobierz i narysuj wszystkie trasy ===
    (async function drawAllRoutes() {
        const allCoords = [];
        for (const route of routes) {
            const coords = await fetchRoute(route);
            if (coords) allCoords.push(...coords);
        }
        if (allCoords.length > 0) {
            const bounds = L.latLngBounds(allCoords);
            map.fitBounds(bounds, { padding: [30, 30] });
        }
    })();
}
