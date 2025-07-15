let apiBaseUrl;

const hostname = window.location.hostname;

if (hostname === 'localhost' || hostname === '127.0.0.1') {
    apiBaseUrl = 'http://localhost:5111';
} else {
    apiBaseUrl = 'https://map-app-host-backend.onrender.com';
}

export const API_BASE_URL = apiBaseUrl;