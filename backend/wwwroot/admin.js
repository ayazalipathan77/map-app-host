
// Helper function to include Authorization header and handle global errors
async function fetchWithAuth(url, options = {}) {
    const token = localStorage.getItem('jwtToken');
    const headers = {
        ...options.headers,
        ...(token ? { 'Authorization': `Bearer ${token}` } : {})
    };

    try {
        const response = await fetch(url, { ...options, headers });

        if (response.status === 401) {
            localStorage.removeItem('jwtToken');
            if (window.location.pathname !== '/login.html') {
                window.location.href = 'login.html';
            }
            throw new Error('Unauthorized');
        }

        if (!response.ok) {
            const errorData = await response.json().catch(() => ({ message: 'An unknown error occurred.' }));
            throw new Error(errorData.message || `HTTP error! status: ${response.status}`);
        }

        return response;
    } catch (error) {
        console.error('fetchWithAuth error:', error);
        throw error;
    }
}

// Define the base URL for your backend API
import { API_BASE_URL } from './config.js';

document.addEventListener('DOMContentLoaded', () => {
    const karachiCenter = [24.9, 67.1];
    const karachiBounds = [[24.75, 66.95], [25.05, 67.25]];

    let initialLat = karachiCenter[0];
    let initialLng = karachiCenter[1];
    let initialZoom = 12;

    const urlParams = new URLSearchParams(window.location.search);
    const paramLat = urlParams.get('lat');
    const paramLng = urlParams.get('lng');
    const paramZoom = urlParams.get('zoom');

    if (paramLat && paramLng) {
        initialLat = parseFloat(paramLat);
        initialLng = parseFloat(paramLng);
        if (paramZoom) {
            initialZoom = parseInt(paramZoom);
        }
    }

    const map = L.map('mapid').setView([initialLat, initialLng], initialZoom);
    map.setMaxBounds(karachiBounds);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
        minZoom: 10,
        maxBoundsViscosity: 1.0
    }).addTo(map);

    const geocoderControl = L.Control.geocoder({
        geocodingQueryParams: {
            'viewbox': `${karachiBounds[0][1]},${karachiBounds[0][0]},${karachiBounds[1][1]},${karachiBounds[1][0]}`,
            'bounded': 1
        }
    }).addTo(map);

    geocoderControl.on('markgeocode', function(e) {
        const bbox = e.geocode.bbox;
        const poly = L.polygon([
            bbox.getSouthEast(),
            bbox.getNorthEast(),
            bbox.getNorthWest(),
            bbox.getSouthWest()
        ]).addTo(map);
        map.fitBounds(poly.getBounds());
    });

    const pinFormContainer = document.querySelector('.pin-form-container');
    const pinForm = document.getElementById('pinForm');
    const latInput = document.getElementById('lat');
    const lngInput = document.getElementById('lng');
    const descriptionInput = document.getElementById('description');
    const imagesInput = document.getElementById('images');
    const cancelPinButton = document.getElementById('cancelPin');
    const closePinFormButton = document.getElementById('closePinForm');

    const imageDialog = document.getElementById('imageDialog');
    const enlargedImage = document.getElementById('enlargedImage');
    const closeImageDialogButton = imageDialog.querySelector('.close-image-dialog');

    function openImageDialog(src) {
        console.log('Opening image dialog for:', src);
        enlargedImage.src = src;
        imageDialog.style.display = 'flex';
    }

    function closeImageDialog() {
        console.log('Closing image dialog');
        imageDialog.style.display = 'none';
        enlargedImage.src = '';
    }

    closeImageDialogButton.addEventListener('click', closeImageDialog);
    imageDialog.addEventListener('click', (event) => {
        if (event.target === imageDialog) {
            closeImageDialog();
        }
    });

    let currentMarker = null;

    async function loadPins() {
        try {
            const response = await fetchWithAuth(`${API_BASE_URL}/api/pins`);
            const pins = await response.json();

            pins.forEach(pin => {
                const marker = L.marker([pin.lat, pin.lng]).addTo(map);
                const popupContent = document.createElement('div');
                popupContent.innerHTML = `<h3 class="font-semibold text-lg">${pin.description}</h3>`;

                if (pin.imageUrls && pin.imageUrls.length > 0) {
                    const imagesContainer = document.createElement('div');
                    imagesContainer.className = 'flex space-x-2 mt-2';
                    pin.imageUrls.forEach(imageUrl => {
                        const img = document.createElement('img');
                        img.src = `${API_BASE_URL}${imageUrl}`;
                        img.alt = 'Pin Image';
                        img.className = 'w-24 h-24 object-cover rounded-md cursor-pointer hover:opacity-75 transition-opacity';
                        img.addEventListener('click', () => openImageDialog(img.src));
                        imagesContainer.appendChild(img);
                    });
                    popupContent.appendChild(imagesContainer);
                }
                marker.bindPopup(popupContent);
            });
        } catch (error) {
            console.error('Error loading pins:', error);
        }
    }

    loadPins();

    map.on('popupopen', function (e) {
        const popupContent = e.popup.getElement();
        const images = popupContent.querySelectorAll('img');
        images.forEach(img => {
            img.addEventListener('click', (event) => {
                openImageDialog(event.target.src);
            });
        });
    });

    map.on('click', (e) => {
        if (currentMarker) {
            map.removeLayer(currentMarker);
        }
        currentMarker = L.marker(e.latlng).addTo(map);
        latInput.value = e.latlng.lat;
        lngInput.value = e.latlng.lng;
        pinFormContainer.classList.remove('hidden');
        pinFormContainer.classList.add('flex');
    });

    pinForm.addEventListener('submit', async (e) => {
        e.preventDefault();

        const formData = new FormData();
        formData.append('lat', latInput.value);
        formData.append('lng', lngInput.value);
        formData.append('description', descriptionInput.value);

        for (const file of imagesInput.files) {
            formData.append('images', file);
        }

        try {
            const response = await fetchWithAuth(`${API_BASE_URL}/api/pins`, {
                method: 'POST',
                body: formData
            });

            if (response.ok) {
                alert('Pin added successfully!');
                pinFormContainer.classList.add('hidden');
                pinFormContainer.classList.remove('flex');
                pinForm.reset();
                if (currentMarker) {
                    map.removeLayer(currentMarker);
                    currentMarker = null;
                }
                map.eachLayer(layer => {
                    if (layer instanceof L.Marker) {
                        map.removeLayer(layer);
                    }
                });
                loadPins();
            } else {
                alert('Failed to add pin.');
            }
        } catch (error) {
            console.error('Error adding pin:', error);
            alert('An error occurred while adding the pin.');
        }
    });

    cancelPinButton.addEventListener('click', () => {
        pinFormContainer.classList.add('hidden');
        pinFormContainer.classList.remove('flex');
        pinForm.reset();
        if (currentMarker) {
            map.removeLayer(currentMarker);
            currentMarker = null;
        }
    });

    closePinFormButton.addEventListener('click', () => {
        pinFormContainer.classList.add('hidden');
        pinFormContainer.classList.remove('flex');
        pinForm.reset();
        if (currentMarker) {
            map.removeLayer(currentMarker);
            currentMarker = null;
        }
    });
});
