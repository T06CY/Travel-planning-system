function initHotelMap() {
    try {
        const mapEl = document.getElementById('hotelMap');
        if (!mapEl || typeof google === 'undefined' || !google.maps) return;
        const center = { lat: 3.1390, lng: 101.6869 }; // Kuala Lumpur as default
        const map = new google.maps.Map(mapEl, {
            center,
            zoom: 6,
            disableDefaultUI: true
        });
        // Optionally add a marker for the selected destination if provided via data attributes
        const dest = mapEl.dataset.destination;
        if (dest) {
            const geocoder = new google.maps.Geocoder();
            geocoder.geocode({ address: dest }, (results, status) => {
                if (status === 'OK' && results[0]) {
                    map.setCenter(results[0].geometry.location);
                    map.setZoom(12);
                    new google.maps.Marker({ map, position: results[0].geometry.location });
                }
            });
        }
    } catch (e) {
        // Fail silently if maps not available
        console.error('Map init failed', e);
    }
}
