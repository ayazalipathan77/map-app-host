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

document.addEventListener('DOMContentLoaded', async () => {
    const urlParams = new URLSearchParams(window.location.search);
    const pinId = urlParams.get('id');

    if (!pinId) {
        alert('No pin ID provided for editing.');
        window.location.href = 'view-pins.html';
        return;
    }

    const editPinForm = document.getElementById('editPinForm');
    const pinIdInput = document.getElementById('pinId');
    const latInput = document.getElementById('lat');
    const lngInput = document.getElementById('lng');
    const descriptionInput = document.getElementById('description');
    const imagesInput = document.getElementById('images');
    const currentImagesDiv = document.getElementById('currentImages');
    const backToPinsButton = document.getElementById('backToPinsButton');
    const cancelEditButton = document.getElementById('cancelEdit');

    let existingPinImageUrls = []; // To store the original image URLs

    // Fetch pin data and populate the form
    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/pins/${pinId}`);
        if (!response.ok) {
            throw new Error('Pin not found');
        }
        const pin = await response.json();

        pinIdInput.value = pin.id;
        latInput.value = pin.lat;
        lngInput.value = pin.lng;
        descriptionInput.value = pin.description;
        existingPinImageUrls = pin.imageUrls || []; // Store original image URLs

        // Display current images with delete buttons
        renderCurrentImages(existingPinImageUrls);

    } catch (error) {
        console.error('Error fetching pin for editing:', error);
        alert('Failed to load pin for editing.');
        window.location.href = 'view-pins.html';
        return;
    }

    function renderCurrentImages(imageUrls) {
        currentImagesDiv.innerHTML = '';
        if (imageUrls.length > 0) {
            imageUrls.forEach(imageUrl => {
                const imgContainer = document.createElement('div');
                imgContainer.classList.add('current-image-item');
                imgContainer.innerHTML = `
                    <img src="${API_BASE_URL}${imageUrl}" alt="Current Pin Image">
                    <button type="button" class="delete-image-btn" data-url="${imageUrl}">&times;</button>
                `;
                currentImagesDiv.appendChild(imgContainer);
            });

            // Add event listeners to delete buttons
            currentImagesDiv.querySelectorAll('.delete-image-btn').forEach(button => {
                button.addEventListener('click', (e) => {
                    const urlToDelete = e.target.dataset.url;
                    existingPinImageUrls = existingPinImageUrls.filter(url => url !== urlToDelete);
                    renderCurrentImages(existingPinImageUrls); // Re-render images
                });
            });
        } else {
            currentImagesDiv.innerHTML = '<p>No current images.</p>';
        }
    }


    // Handle form submission for updating pin
    editPinForm.addEventListener('submit', async (e) => {
        e.preventDefault();

        const formData = new FormData();
        formData.append('lat', latInput.value);
        formData.append('lng', lngInput.value);
        formData.append('description', descriptionInput.value);

        // Append existing image URLs to keep
        existingPinImageUrls.forEach(url => {
            formData.append('existingImageUrlsToKeep', url);
        });

        // Append new images if selected
        for (const file of imagesInput.files) {
            formData.append('newImages', file);
        }

        try {
            const response = await fetchWithAuth(`${API_BASE_URL}/api/pins/${pinId}`, {
                method: 'PUT',
                body: formData
            });

            if (response.ok) {
                alert('Pin updated successfully!');
                window.location.href = 'view-pins.html'; // Go back to all pins page
            } else {
                alert('Failed to update pin.');
            }
        } catch (error) {
            console.error('Error updating pin:', error);
            alert('An error occurred while updating the pin.');
        }
    });

    // Navigation buttons
    backToPinsButton.addEventListener('click', () => {
        window.location.href = 'view-pins.html';
    });

    cancelEditButton.addEventListener('click', () => {
        window.location.href = 'view-pins.html';
    });
});