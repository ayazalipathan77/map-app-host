// Define the base URL for your backend API
import { API_BASE_URL } from './config.js';

document.addEventListener('DOMContentLoaded', () => {
    const loginForm = document.getElementById('loginForm');
    const messageElement = document.getElementById('message');

    loginForm.addEventListener('submit', async (e) => {
        e.preventDefault();

        const username = loginForm.username.value;
        const password = loginForm.password.value;

        try {
            const response = await fetch(`${API_BASE_URL}/api/auth/login`, { // Use API_BASE_URL
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ username, password })
            });

            const data = await response.json();

            if (response.ok) {
                // Store the JWT token instead of a boolean flag
                localStorage.setItem('jwtToken', data.token);
                window.location.href = 'admin.html';
            } else {
                messageElement.textContent = data.message || 'Login failed.';
            }
        } catch (error) {
            console.error('Error:', error);
            messageElement.textContent = 'An error occurred. Please try again.';
        }
    });
});