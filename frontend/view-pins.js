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

document.addEventListener('DOMContentLoaded', () => {
    const pinsContainer = document.getElementById('pinsContainer');
    const backToMapButton = document.getElementById('backToMapButton');
    const pinFilterInput = document.getElementById('pinFilterInput'); // New

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

    let allPins = []; // To store all fetched pins

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


    async function loadPins() {
        try {
            const response = await fetchWithAuth(`${API_BASE_URL}/api/pins`);
            allPins = await response.json(); // Store all pins
            renderPins(allPins); // Render all pins initially
        } catch (error) {
            console.error('Error loading pins:', error);
            // fetchWithAuth now handles 401 redirection
        }
    }

    function renderPins(pinsToRender) {
        pinsContainer.innerHTML = ''; // Clear previous pins

        if (pinsToRender.length === 0) {
            pinsContainer.innerHTML = '<p>No pins found matching your filter.</p>';
            return;
        }

        pinsToRender.forEach(pin => {
            const pinCard = document.createElement('div');
            pinCard.classList.add('pin-card');
            pinCard.innerHTML = `
                    <h3>${pin.description}</h3>
                    <p>Lat: ${pin.lat}, Lng: ${pin.lng}</p>
                    <div class="pin-actions">
                        <button class="view-details-btn" data-id="${pin.id}">View Details</button>
                        <button class="edit-btn" data-id="${pin.id}">Edit</button>
                        <button class="delete-btn" data-id="${pin.id}">Delete</button>
                        <button class="go-to-pin-btn" data-lat="${pin.lat}" data-lng="${pin.lng}">Go to Pin Location</button>
                    </div>
                `;
            pinsContainer.appendChild(pinCard);
        });

        // Add event listeners for buttons
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
                pin.imageUrls.forEach(imageUrl => {
                    const img = document.createElement('img');
                    img.src = `${API_BASE_URL}${imageUrl}`;
                    img.alt = 'Pin Image';
                    img.style.maxWidth = '150px';
                    img.style.maxHeight = '150px';
                    img.style.margin = '5px';
                    img.classList.add('clickable-image'); // Add class for event listener
                    img.addEventListener('click', () => openImageDialog(img.src)); // Add click listener
                    modalImages.appendChild(img);
                });
            } else {
                modalImages.innerHTML = '<p>No images available.</p>';
            }
            pinDetailModal.style.display = 'block';
        } catch (error) {
            console.error('Error fetching pin details:', error);
            alert('Failed to load pin details.');
            // fetchWithAuth now handles 401 redirection
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
                    loadPins(); // Reload pins after deletion
                } else {
                    alert('Failed to delete pin.');
                    // fetchWithAuth now handles 401 redirection
                }
            } catch (error) {
                console.error('Error deleting pin:', error);
                alert('An error occurred while deleting the pin.');
            }
        }
    }

    function goToPinLocation(lat, lng) {
        window.location.href = `admin.html?lat=${lat}&lng=${lng}&zoom=14`; // Pass lat, lng, and zoom
    }

    // Event listeners for navigation and modal
    backToMapButton.addEventListener('click', () => {
        window.location.href = 'admin.html';
    });

    closeModalButton.addEventListener('click', () => {
        pinDetailModal.style.display = 'none';
    });

    window.addEventListener('click', (event) => {
        if (event.target === pinDetailModal) {
            pinDetailModal.style.display = 'none';
        }
    });

    // Event listener for filter input
    pinFilterInput.addEventListener('input', filterPins);

    loadPins(); // Initial load of pins
});