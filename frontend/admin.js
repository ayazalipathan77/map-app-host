// Helper function to include Authorization header and handle global errors
async function fetchWithAuth(url, options = {}) {
    const token = localStorage.getItem('jwtToken');
    const headers = {
        ...options.headers,
        ...(token ? { 'Authorization': `Bearer ${token}` } : {})
    };

    try {
        const response = await fetch(url, { ...options, headers });

        // Handle unauthorized responses globally
        if (response.status === 401) {
            localStorage.removeItem('jwtToken'); // Clear invalid token
            // Only redirect if not already on login page to prevent infinite loops
            if (window.location.pathname !== '/login.html') {
                window.location.href = 'login.html'; // Redirect to login
            }
            throw new Error('Unauthorized'); // Propagate error
        }

        // Handle other HTTP errors (e.g., 400, 500)
        if (!response.ok) {
            const errorData = await response.json().catch(() => ({ message: 'An unknown error occurred.' }));
            throw new Error(errorData.message || `HTTP error! status: ${response.status}`);
        }

        return response;
    } catch (error) {
        console.error('fetchWithAuth error:', error);
        // Re-throw the error so calling functions can still catch it if needed
        throw error;
    }
}

// Define the base URL for your backend API
import { API_BASE_URL } from './config.js';

document.addEventListener('DOMContentLoaded', () => {
    

    // Karachi coordinates and bounds
    const karachiCenter = [24.9, 67.1];
    const karachiBounds = [[24.75, 66.95], [25.05, 67.25]];

    let initialLat = karachiCenter[0];
    let initialLng = karachiCenter[1];
    let initialZoom = 12; // Default zoom

    // Check for URL parameters for pin location
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

    const map = L.map('mapid').setView([initialLat, initialLng], initialZoom); // Set initial view
    map.setMaxBounds(karachiBounds); // Restrict map to Karachi bounds

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
        minZoom: 10, // Optional: Set a minimum zoom level to keep Karachi visible
        maxBoundsViscosity: 1.0 // Prevents panning outside bounds
    }).addTo(map);

    // Initialize Leaflet Control Geocoder and add to map
    const geocoderControl = L.Control.geocoder({
        geocodingQueryParams: {
            'viewbox': `${karachiBounds[0][1]},${karachiBounds[0][0]},${karachiBounds[1][1]},${karachiBounds[1][0]}`,
            'bounded': 1 // Restrict search to viewbox
        }
    }).addTo(map);

    // Handle geocoder result selection using the captured instance
    geocoderControl.on('markgeocode', function(e) {
        const bbox = e.geocode.bbox;
        const poly = L.polygon([
            bbox.getSouthEast(),
            bbox.getNorthEast(),
            bbox.getNorthWest(),
            bbox.getSouthWest()
        ]).addTo(map);
        map.fitBounds(poly.getBounds());
        // Optionally, you can add a marker at the result location
        // L.marker(e.geocode.center).addTo(map).bindPopup(e.geocode.name).openPopup();
    });


    const pinFormContainer = document.querySelector('.pin-form-container');
    const pinForm = document.getElementById('pinForm');
    const latInput = document.getElementById('lat');
    const lngInput = document.getElementById('lng');
    const descriptionInput = document.getElementById('description');
    const imagesInput = document.getElementById('images');
    const cancelPinButton = document.getElementById('cancelPin');

    // Image Enlargement Dialog elements
    const imageDialog = document.getElementById('imageDialog');
    const enlargedImage = document.getElementById('enlargedImage');
    const closeImageDialogButton = imageDialog.querySelector('.close-image-dialog');

    // Function to open the image enlargement dialog
    function openImageDialog(src) {
        enlargedImage.src = src;
        imageDialog.style.display = 'flex'; // Use flex to center content
    }

    // Function to close the image enlargement dialog
    function closeImageDialog() {
        imageDialog.style.display = 'none';
        enlargedImage.src = ''; // Clear image source
    }

    // Event listener for closing the image dialog
    closeImageDialogButton.addEventListener('click', closeImageDialog);
    imageDialog.addEventListener('click', (event) => {
        if (event.target === imageDialog) { // Close only if clicking on the overlay
            closeImageDialog();
        }
    });


    let currentMarker = null;

    // Load existing pins
    async function loadPins() {
        try {
            const response = await fetchWithAuth(`${API_BASE_URL}/api/pins`); // Use API_BASE_URL
            const pins = await response.json();

            pins.forEach(pin => {
                const marker = L.marker([pin.lat, pin.lng]).addTo(map);
                let popupContent = `<h3>${pin.description}</h3>`;
                if (pin.imageUrls && pin.imageUrls.length > 0) {
                    pin.imageUrls.forEach(imageUrl => {
                        popupContent += `<img src="${API_BASE_URL}${imageUrl}" alt="Pin Image" style="max-width:100px; max-height:100px; margin:5px;" class="clickable-image">`; // Use API_BASE_URL
                    });
                }
                marker.bindPopup(popupContent);
            });
        } catch (error) {
            console.error('Error loading pins:', error);
            // fetchWithAuth now handles 401 redirection
        }
    }

    loadPins();

    // Event delegation for images inside Leaflet popups
    map.on('popupopen', function(e) {
        const popupContent = e.popup.getElement();
        const images = popupContent.querySelectorAll('.clickable-image');
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
        pinFormContainer.style.display = 'block';
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
                pinFormContainer.style.display = 'none';
                pinForm.reset();
                if (currentMarker) {
                    map.removeLayer(currentMarker);
                    currentMarker = null;
                }
                // Reload pins to show the new one
                map.eachLayer(layer => {
                    if (layer instanceof L.Marker) {
                        map.removeLayer(layer);
                    }
                });
                loadPins();
            } else {
                alert('Failed to add pin.');
                // fetchWithAuth now handles 401 redirection
            }
        } catch (error) {
            console.error('Error adding pin:', error);
            alert('An error occurred while adding the pin.');
        }
    });

    cancelPinButton.addEventListener('click', () => {
        pinFormContainer.style.display = 'none';
        pinForm.reset();
        if (currentMarker) {
            map.removeLayer(currentMarker);
            currentMarker = null;
        }
    });

    // Removed: logoutButton.addEventListener('click', ...);
    // This is now handled by the side menu in menu.js

    // Removed: Navigate to View All Pins page
    // viewPinsButton.addEventListener('click', () => {
    //     window.location.href = 'view-pins.html';
    // });
});