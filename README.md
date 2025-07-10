# Map Application

This is a full-stack web application that allows an administrator to add pinpoints on a map with descriptions and multiple images, and a public interface to view these pins.

## Features

*   **Admin Interface:**
    *   Login protected (username: `ayaz`, password: `ayaz12344321`).
    *   Interactive map to select coordinates.
    *   Form to add description and upload multiple images for each pin.
*   **Public View Interface:**
    *   Displays all saved pins on a map.
    *   Clicking a pin shows its description and image thumbnails.
*   **Backend:** .NET Core Web API
    *   Handles authentication, pin storage, and image uploads.
    *   Uses local JSON file for data persistence and local file system for image storage.
*   **Frontend:** HTML, CSS, and Vanilla JavaScript
    *   Utilizes Leaflet.js for interactive maps.

## Tech Stack

*   **Backend:** .NET Core 8.0 Web API (C#)
*   **Frontend:** HTML, CSS, Vanilla JavaScript, Leaflet.js

## Setup and Run

Follow these steps to set up and run the application locally.

### Prerequisites

*   .NET SDK 8.0 or later
*   Node.js (for serving frontend, though a simple static file server or opening `index.html` directly works)

### 1. Clone the Repository (if applicable)

If you received this project as a Git repository, clone it:

```bash
git clone <repository_url>
cd map-app
```

If you received it as a compressed archive, extract it and navigate into the `map-app` directory.

### 2. Backend Setup

Navigate to the `backend` directory:

```bash
cd mapapp/backend
```

Restore .NET dependencies:

```bash
dotnet restore
```

Run the backend application:

```bash
dotnet run --project MapApp.csproj
```

The backend API will typically run on `http://localhost:5000` (or another port, which will be displayed in the console). Ensure this port matches the one configured in the frontend JavaScript files (`login.js`, `public.js`, `admin.js`). If not, you'll need to update the `http://localhost:5000` URLs in those files.

### 3. Frontend Setup

Navigate to the `frontend` directory:

```bash
cd ../frontend
```

You can serve the frontend files using a simple local HTTP server (e.g., `http-server` from npm, Python's `http.server`, or VS Code's Live Server extension).

**Using Python's Simple HTTP Server:**

```bash
python -m http.server 8000
```

Then, open your web browser and go to `http://localhost:8000/login.html` for the admin login or `http://localhost:8000/index.html` for the public view.

**Alternatively, you can just open the HTML files directly in your browser:**

*   `mapapp/frontend/login.html`
*   `mapapp/frontend/index.html`

However, using a local server is recommended to avoid potential CORS issues with file:// protocol.

## Usage

### Admin Login

Go to `login.html`.
*   **Username:** `ayaz`
*   **Password:** `ayaz12344321`

After successful login, you will be redirected to `admin.html`.

### Admin Map Interface

On the admin map:
1.  Click anywhere on the map to drop a temporary marker.
2.  A form will appear with the latitude and longitude pre-filled.
3.  Enter a description for the pin.
4.  Select one or more images to upload.
5.  Click "Add Pin" to save the pin.
6.  Click "Cancel" to discard the pin creation.
7.  Click "Logout" to return to the login page.

### Public Map View

Go to `index.html`.
All pins added by the admin will be displayed on the map. Click on any pin to see its description and associated images.

## Project Structure

```
map-app/
├── backend/
│   ├── Controllers/
│   │   ├── AuthController.cs
│   │   └── PinsController.cs
│   ├── Data/
│   │   ├── PinRepository.cs
│   │   └── pins.json
│   ├── Models/
│   │   ├── LoginRequest.cs
│   │   └── Pin.cs
│   ├── wwwroot/
│   │   └── images/  (uploaded images are stored here)
│   ├── Program.cs
│   ├── MapApp.csproj
│   └── ... (other .NET project files)
└── frontend/
    ├── admin.html
    ├── index.html
    ├── login.html
    ├── admin.js
    ├── login.js
    ├── public.js
    └── style.css
└── README.md
```