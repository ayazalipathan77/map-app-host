
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

document.addEventListener('DOMContentLoaded', () => {
    const pinsContainer = document.getElementById('pinsContainer');
    const backToMapButton = document.getElementById('backToMapButton');
    const pinFilterInput = document.getElementById('pinFilterInput');

    // Pin Detail Modal elements
    const pinDetailModal = document.getElementById('pinDetailModal');
    const closeModalButton = pinDetailModal.querySelector('.close-button');
    const modalDescription = document.getElementById('modalDescription');
    const modalImages = document.getElementById('modalImages');
    const modalLat = document.getElementById('modalLat');
    const modalLng = document.getElementById('modalLng');

    // Image Enlargement Dialog elements
    const imageDialog = document.getElementById('imageDialog');
    const enlargedImage = document.getElementById('enlargedImage');
    const closeImageDialogButton = imageDialog.querySelector('.close-image-dialog');

    let allPins = [];

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

    async function loadPins() {
        try {
            const response = await fetchWithAuth(`${API_BASE_URL}/api/pins`);
            allPins = await response.json();
            renderPins(allPins);
        } catch (error) {
            console.error('Error loading pins:', error);
        }
    }

    function renderPins(pinsToRender) {
        pinsContainer.innerHTML = '';

        if (pinsToRender.length === 0) {
            pinsContainer.innerHTML = '<p class="text-gray-500 col-span-full">No pins found matching your filter.</p>';
            return;
        }

        pinsToRender.forEach(pin => {
            const pinCard = document.createElement('div');
            pinCard.className = 'bg-white rounded-lg shadow-lg overflow-hidden transform hover:-translate-y-2 transition-transform duration-300 ease-in-out';
            pinCard.innerHTML = `
                <div class="p-6">
                    <h3 class="text-lg font-normal text-gray-800 mb-2 truncate">${pin.description}</h3>
                    <p class="text-md text-gray-700 mb-4">Lat: ${pin.lat.toFixed(4)}, Lng: ${pin.lng.toFixed(4)}</p>
                    <div class="flex flex-wrap gap-3 justify-center">
                        <button class="view-details-btn text-sm bg-blue-600 hover:bg-blue-700 text-white font-bold py-2 px-4 rounded-lg transition-all duration-300" data-id="${pin.id}">Details</button>
                        <button class="edit-btn text-sm bg-yellow-500 hover:bg-yellow-600 text-white font-bold py-2 px-4 rounded-lg transition-all duration-300" data-id="${pin.id}">Edit</button>
                        <button class="delete-btn text-sm bg-red-600 hover:bg-red-700 text-white font-bold py-2 px-4 rounded-lg transition-all duration-300" data-id="${pin.id}">Delete</button>
                        <button class="go-to-pin-btn text-sm bg-green-600 hover:bg-green-700 text-white font-bold py-2 px-4 rounded-lg transition-all duration-300" data-lat="${pin.lat}" data-lng="${pin.lng}">Go to Pin</button>
                    </div>
                </div>
            `;
            pinsContainer.appendChild(pinCard);
        });

        pinsContainer.querySelectorAll('.view-details-btn').forEach(button => {
            button.addEventListener('click', (e) => showPinDetails(e.target.dataset.id));
        });
        pinsContainer.querySelectorAll('.edit-btn').forEach(button => {
            button.addEventListener('click', (e) => editPin(e.target.dataset.id));
        });
        pinsContainer.querySelectorAll('.delete-btn').forEach(button => {
            button.addEventListener('click', (e) => deletePin(e.target.dataset.id));
        });
        pinsContainer.querySelectorAll('.go-to-pin-btn').forEach(button => {
            button.addEventListener('click', (e) => goToPinLocation(e.target.dataset.lat, e.target.dataset.lng));
        });
    }

    function filterPins() {
        const filterText = pinFilterInput.value.toLowerCase();
        const filtered = allPins.filter(pin =>
            pin.description.toLowerCase().includes(filterText)
        );
        renderPins(filtered);
    }

    async function showPinDetails(pinId) {
        try {
            const response = await fetchWithAuth(`${API_BASE_URL}/api/pins/${pinId}`);
            const pin = await response.json();

            modalDescription.textContent = pin.description;
            modalLat.textContent = pin.lat;
            modalLng.textContent = pin.lng;
            modalImages.innerHTML = '';
            if (pin.imageUrls && pin.imageUrls.length > 0) {
                modalImages.innerHTML = ''; // Clear previous images
                pin.imageUrls.forEach(imageUrl => {
                    const img = document.createElement('img');
                    img.src = imageUrl;
                    img.alt = 'Pin Image';
                    img.className = 'w-full h-32 object-cover rounded-lg cursor-pointer hover:opacity-75 transition-opacity';
                    img.addEventListener('click', () => openImageDialog(img.src));
                    modalImages.appendChild(img);
                });
            } else {
                modalImages.innerHTML = '<p class="text-gray-500">No images available.</p>';
            }
            pinDetailModal.classList.remove('hidden');
            pinDetailModal.classList.add('flex');
        } catch (error) {
            console.error('Error fetching pin details:', error);
            alert('Failed to load pin details.');
        }
    }

    function editPin(pinId) {
        window.location.href = `edit-pin.html?id=${pinId}`;
    }

    async function deletePin(pinId) {
        if (confirm('Are you sure you want to delete this pin?')) {
            try {
                const response = await fetchWithAuth(`${API_BASE_URL}/api/pins/${pinId}`, {
                    method: 'DELETE'
                });

                if (response.ok) {
                    alert('Pin deleted successfully!');
                    loadPins();
                } else {
                    alert('Failed to delete pin.');
                }
            } catch (error) {
                console.error('Error deleting pin:', error);
                alert('An error occurred while deleting the pin.');
            }
        }
    }

    function goToPinLocation(lat, lng) {
        window.location.href = `admin.html?lat=${lat}&lng=${lng}&zoom=14`;
    }

    backToMapButton.addEventListener('click', () => {
        window.location.href = 'admin.html';
    });

    closeModalButton.addEventListener('click', () => {
        pinDetailModal.classList.add('hidden');
        pinDetailModal.classList.remove('flex');
    });

    window.addEventListener('click', (event) => {
        if (event.target === pinDetailModal) {
            pinDetailModal.classList.add('hidden');
            pinDetailModal.classList.remove('flex');
        }
    });

    pinFilterInput.addEventListener('input', filterPins);

    loadPins();
});
