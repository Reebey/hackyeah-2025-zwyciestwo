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
}
