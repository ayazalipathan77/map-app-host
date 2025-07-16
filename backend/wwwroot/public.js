// Define the base URL for your backend API
import { API_BASE_URL } from './config.js';

// Helper function to include Authorization header and handle global errors
async function fetchWithAuth(url, options = {}) {
    const token = localStorage.getItem('jwtToken');
    const headers = {
        ...options.headers,
        ...(token ? { 'Authorization': `Bearer ${token}` } : {})
    };

    try {
        const response = await fetch(url, { ...options, headers });

        // Handle unauthorized responses globally (public pages might not redirect, just show limited content)
        if (response.status === 401) {
            // For public pages, we might not want to redirect to login immediately.
            // Instead, just return the response and let the calling function handle it
            // (e.g., show a message that some content requires login).
            // For now, we'll just clear the token if it's invalid.
            localStorage.removeItem('jwtToken');
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

document.addEventListener('DOMContentLoaded', async () => {
    const map = L.map('mapid').setView([0, 0], 2); // Default view, will adjust after loading pins

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
    }).addTo(map);

    // Image Enlargement Dialog elements
    const imageDialog = document.getElementById('imageDialog');
    const enlargedImage = document.getElementById('enlargedImage');
    const closeImageDialogButton = imageDialog.querySelector('.close-image-dialog');

    // Function to open the image enlargement dialog
    function openImageDialog(src) {
        console.log('Opening image dialog for:', src);
        enlargedImage.src = src;
        imageDialog.style.display = 'flex';
        imageDialog.style.zIndex = '99999'; // Ensure it's on top
    }

    function closeImageDialog() {
        console.log('Closing image dialog');
        imageDialog.style.display = 'none';
        enlargedImage.src = '';
    }

    // Event listener for closing the image dialog
    closeImageDialogButton.addEventListener('click', closeImageDialog);
    imageDialog.addEventListener('click', (event) => {
        if (event.target === imageDialog) { // Close only if clicking on the overlay
            closeImageDialog();
        }
    });


    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/pins`); // Use fetchWithAuth
        const pins = await response.json();

        if (pins && pins.length > 0) {
            const latLngs = [];
            pins.forEach(pin => {
                const marker = L.marker([pin.lat, pin.lng]).addTo(map);
                latLngs.push([pin.lat, pin.lng]);

                const popupContent = document.createElement('div');
                popupContent.innerHTML = `<h3 class="font-semibold text-lg">${pin.description}</h3>`;

                if (pin.imageUrls && pin.imageUrls.length > 0) {
                    const imagesContainer = document.createElement('div');
                    imagesContainer.className = 'flex space-x-2 mt-2';
                    pin.imageUrls.forEach(imageUrl => {
                        const img = document.createElement('img');
                        img.src = imageUrl;
                        img.alt = 'Pin Image';
                        img.className = 'w-24 h-24 object-cover rounded-md cursor-pointer hover:opacity-75 transition-opacity';
                        img.addEventListener('click', () => openImageDialog(img.src));
                        imagesContainer.appendChild(img);
                    });
                    popupContent.appendChild(imagesContainer);
                }
                marker.bindPopup(popupContent);
            });

            // Adjust map view to fit all markers
            const bounds = L.latLngBounds(latLngs);
            map.fitBounds(bounds);
        } else {
            console.log('No pins found.');
        }
    } catch (error) {
        console.error('Error fetching pins:', error);
        // fetchWithAuth now handles 401, but for public pages, no redirect is needed here.
    }

    // Event delegation for images inside Leaflet popups
    map.on('popupopen', function(e) {
        const popupContent = e.popup.getElement();
        const images = popupContent.querySelectorAll('img');
        images.forEach(img => {
            img.addEventListener('click', (event) => {
                openImageDialog(event.target.src);
            });
        });
    });
});